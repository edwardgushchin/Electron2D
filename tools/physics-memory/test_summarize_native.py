"""Check phase carry-over, marker exclusion and rejected incomplete/corrupt traces."""
import unittest
from summarize_native import summarize


def trace():
    lines = ["v 10500 3", "s 8 work.so!", "s 4 work", "s b markers.so!", "s 10 e2d_memory_phase",
             "i 1000 1 2", "t 1 0", "i 2000 3 4", "t 2 0", "a 64 1", "+ 0"]
    for marker in range(7):
        lines += [f"a {17 + marker:x} 2", f"+ {marker + 1:x}", f"- {marker + 1:x}"]
        if marker == 2:
            lines += ["- 0"]
    return lines


class PhaseTests(unittest.TestCase):
    def test_unmarked_lifetime(self):
        lines = [*trace()[:11], "- 0"]
        report = summarize(lines, phase_markers=False)
        self.assertEqual((report["grossPeakBytes"], report["workloadPeakBytes"], report["endBytes"]), (100, 100, 0))
        self.assertEqual([r["name"] for r in report["phases"]], ["lifetime"])
        self.assertEqual((report["markerAllocations"], report["markerFrees"], report["excludedMarkerBytes"]), (0, 0, 0))
        with self.assertRaises(ValueError):
            summarize(lines)
        with self.assertRaises(ValueError):
            summarize(trace(), phase_markers=False)

    def test_carry_and_exclusion(self):
        report = summarize(trace())
        self.assertEqual((report["grossPeakBytes"], report["workloadPeakBytes"], report["endBytes"]), (119, 100, 0))
        self.assertEqual((report["excludedMarkerBytes"], report["markerAllocations"], report["markerFrees"]), (140, 7, 7))
        rows = report["phases"]
        self.assertEqual([r["peakBytes"] for r in rows], [100, 100, 100, 100, 0, 0, 0, 0])
        self.assertEqual(rows[3]["freedBytes"], 100)
        self.assertEqual(rows[3]["entryBytes"] - rows[3]["exitBytes"], 100)
        self.assertEqual(report["endLeafModules"], {})
        boundary = report["markers"][2]["liveAllocations"]
        self.assertEqual(boundary["bytes"], 100)
        self.assertEqual(boundary["routes"], [{"leafModule": "work.so!", "callerModule": "[no external frame]",
                                               "callerFunction": "", "bytes": 100, "allocations": 1}])
        self.assertEqual(boundary["topSites"][0]["stack"],
                         [{"module": "work.so!", "function": "work", "address": "0x1000"}])
        self.assertEqual(report["endAllocations"]["bytes"], 0)

    def test_live_call_paths_and_omitted_sites(self):
        lines = trace()[:11]
        lines += ["s 9 caller.so", "s 5 entry", "i 3000 5 6", "t 3 0", "i 4000 1 2", "t 4 3",
                  "i 5000 1 2", "t 5 4"]
        for index in range(1, 16):
            lines += [f"a {index:x} 5", f"+ {index:x}", f"+ {index:x}"]
        for marker in range(7):
            lines += [f"a {17 + marker:x} 2", f"+ {16 + marker:x}", f"- {16 + marker:x}"]
        # The same allocation-info may be reused; no individual pointer lifetime is inferred.
        lines += ["- 1", "+ 1"]
        report = summarize(lines)
        boundary = report["markers"][3]["liveAllocations"]
        self.assertEqual(boundary["bytes"], 340)
        self.assertEqual(boundary["routes"][0], {"leafModule": "work.so!", "callerModule": "caller.so",
                                                "callerFunction": "entry", "bytes": 240, "allocations": 30})
        self.assertEqual(len(boundary["topSites"]), 12)
        self.assertEqual((boundary["otherSiteBytes"], boundary["otherSites"]), (20, 4))
        self.assertEqual(sum(r["bytes"] for r in boundary["topSites"]) + boundary["otherSiteBytes"], 340)
        self.assertEqual([f["module"] for f in boundary["topSites"][1]["stack"]],
                         ["work.so!", "work.so!", "caller.so"])
        self.assertEqual(report["endAllocations"], boundary)

    def test_bad_streams(self):
        original = trace()
        cases = [original[:-1], original[:11], [s.replace("10500 3", "10600 3") for s in original],
                 [s.replace("a 11 2", "a 12 2") for s in original],
                 [*original, "- 0"], [*original, "c 2", "c 1"],
                 [*original, "+ -1"], [*original, "t -1 0"], [*original, "i 3 -1 0"],
                 [*original, "i 3"],
                 [*original, "i -1 0"], [*original, "i 10000000000000000 0"],
                 [*original[:11], "+ ff", *original[11:]]]
        for lines in cases:
            with self.subTest(tail=lines[-2:]), self.assertRaises(ValueError):
                summarize(lines)


if __name__ == "__main__":
    unittest.main()
