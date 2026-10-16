#!/usr/bin/env python3
"""Summarize interpreted Heaptrack 1.5/v3 events delimited by the network phase markers."""
import argparse
from collections import Counter
import gzip
import json


PHASES = ("bootstrap", "startup", "initial-simulation", "warmed-session",
          "extra-checkpoints", "disposal", "release", "shutdown")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def summarize(lines, *, phase_markers=True):
    strings = ["[unknown]"]
    ips = [(0, "", "0")]
    traces = [(0, 0)]
    infos, counts, markers, phases = [], [], [], []
    groups = Counter()
    live = gross = gross_peak = workload_peak = marker_live = marker_bytes = marker_frees = 0
    time_ms = 0
    version = False

    def interval(index):
        return {"name": PHASES[index] if phase_markers else "lifetime", "entryBytes": live, "peakBytes": live,
                "peakLeafModules": {k: v for k, v in groups.items() if v},
                "allocatedBytes": 0, "freedBytes": 0, "allocationCalls": 0, "freeCalls": 0}

    phase = interval(0)

    def stack(trace):
        frames = []
        while trace:
            ip, trace = traces[trace]
            module, function, address = ips[ip]
            frames.append({"module": strings[module], "function": function, "address": address})
        return frames

    def inventory():
        routes, sites = {}, []
        for index, (size, module, marker, trace) in enumerate(infos):
            if marker is not None or not counts[index]:
                continue
            frames = stack(trace)
            caller = next((f for f in frames if f["module"] != module), None)
            key = (module, caller["module"] if caller else "[no external frame]",
                   caller["function"] if caller else "")
            row = routes.setdefault(key, {"leafModule": key[0], "callerModule": key[1],
                                          "callerFunction": key[2], "bytes": 0, "allocations": 0})
            total = size * counts[index]
            row["bytes"] += total
            row["allocations"] += counts[index]
            sites.append({"allocationInfo": index, "size": size, "allocations": counts[index],
                          "bytes": total, "trace": trace})
        sites.sort(key=lambda row: (-row["bytes"], row["allocationInfo"]))
        for row in sites[:12]:
            row["stack"] = stack(row.pop("trace"))
        require(sum(row["bytes"] for row in sites) == live, "Live allocation inventory mismatch")
        require(sum(row["bytes"] for row in routes.values()) == live, "Live call-path partition mismatch")
        return {"bytes": live, "routes": sorted(routes.values(), key=lambda row: -row["bytes"]),
                "topSites": sites[:12], "otherSiteBytes": sum(row["bytes"] for row in sites[12:]),
                "otherSites": len(sites[12:])}

    for ordinal, line in enumerate(lines, 1):
        op, _, data = line.rstrip("\n").partition(" ")
        if op == "v":
            require(not version and data == "10500 3", "Expected one interpreted Heaptrack 1.5/v3 header")
            version = True
        elif op == "s":
            size, _, value = data.partition(" ")
            require(len(value.encode()) == int(size, 16), "Invalid string length")
            strings.append(value)
        elif op == "i":
            fields = data.split()
            require(len(fields) >= 2, "Incomplete instruction record")
            module = int(fields[1], 16)
            function = int(fields[2], 16) if len(fields) > 2 else 0
            require(0 <= module < len(strings) and 0 <= function < len(strings), "Unknown instruction string")
            address = int(fields[0], 16)
            require(0 <= address <= 2**64 - 1, "Invalid instruction address")
            ips.append((module, strings[function], hex(address)))
        elif op == "t":
            ip, parent = (int(s, 16) for s in data.split())
            require(0 <= ip < len(ips) and 0 <= parent < len(traces), "Unknown instruction or trace parent")
            traces.append((ip, parent))
        elif op == "a":
            size, trace = (int(s, 16) for s in data.split())
            require(0 <= size <= 2**64 - 1 and 0 <= trace < len(traces), "Invalid allocation size or trace")
            module, function, _ = ips[traces[trace][0]]
            marker = size - 17 if function == "e2d_memory_phase" else None
            if marker is not None:
                require(phase_markers, "Phase markers are forbidden in an unmarked lifetime trace")
                require(0 <= marker <= 6, "Invalid phase-marker size")
            infos.append((size, strings[module], marker, trace))
            counts.append(0)
        elif op == "c":
            stamp = int(data, 16)
            require(stamp >= time_ms, "Trace timestamps moved backwards")
            time_ms = stamp
        elif op in ("+", "-"):
            require(version, "Allocation before trace version")
            index = int(data, 16)
            require(0 <= index < len(infos), "Unknown allocation; interpret the raw trace first")
            size, module, marker, _ = infos[index]
            sign = 1 if op == "+" else -1
            counts[index] += sign
            require(counts[index] >= 0, "Free exceeds recorded allocations")
            gross += sign * size
            gross_peak = max(gross_peak, gross)
            if marker is not None:
                marker_live += sign * size
                if sign > 0:
                    require(marker == len(markers), "Missing, duplicate or out-of-order phase marker")
                    phase["exitBytes"] = live
                    phases.append(phase)
                    markers.append({"phase": marker, "eventLine": ordinal, "lastTimestampMS": time_ms,
                                    "workloadBytes": live, "markerBytes": size, "liveAllocations": inventory()})
                    marker_bytes += size
                    phase = interval(len(markers))
                else:
                    marker_frees += 1
                continue
            live += sign * size
            groups[module] += sign * size
            require(groups[module] >= 0, "Negative module allocation balance")
            if sign > 0:
                phase["allocatedBytes"] += size
                phase["allocationCalls"] += 1
            else:
                phase["freedBytes"] += size
                phase["freeCalls"] += 1
            workload_peak = max(workload_peak, live)
            if live > phase["peakBytes"]:
                phase["peakBytes"] = live
                phase["peakLeafModules"] = {k: v for k, v in groups.items() if v}
    require(version and len(markers) == marker_frees == (7 if phase_markers else 0) and marker_live == 0, "Incomplete phase-marker trace")
    require(gross == live == sum(groups.values()), "Allocation balance mismatch")
    phase["exitBytes"] = live
    phases.append(phase)
    for row in phases:
        require(row["entryBytes"] + row["allocatedBytes"] - row["freedBytes"] == row["exitBytes"], "Interval balance mismatch")
        require(sum(row["peakLeafModules"].values()) == row["peakBytes"], "Peak partition mismatch")
    return {"format": "heaptrack-1.5-v3", "grossPeakBytes": gross_peak, "workloadPeakBytes": workload_peak,
            "endBytes": live, "endLeafModules": {k: v for k, v in groups.items() if v},
            "excludedMarkerBytes": marker_bytes, "markerAllocations": len(markers),
            "markerFrees": marker_frees, "markers": markers, "phases": phases, "endAllocations": inventory()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("trace", help="Interpreted .gz trace, produced with the matching Heaptrack tools")
    parser.add_argument("--lifetime", action="store_true", help="Accept an unmarked whole-process trace; reject phase markers")
    args = parser.parse_args()
    with gzip.open(args.trace, "rt", encoding="utf-8") as stream:
        result = summarize(stream, phase_markers=not args.lifetime)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
