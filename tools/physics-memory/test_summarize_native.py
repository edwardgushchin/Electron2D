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
    def test_carry_and_exclusion(self):
        report = summarize(trace())
        self.assertEqual((report["grossPeakBytes"], report["workloadPeakBytes"], report["endBytes"]), (119, 100, 0))
        self.assertEqual((report["excludedMarkerBytes"], report["markerAllocations"], report["markerFrees"]), (140, 7, 7))
        rows = report["phases"]
        self.assertEqual([r["peakBytes"] for r in rows], [100, 100, 100, 100, 0, 0, 0, 0])
        self.assertEqual(rows[3]["freedBytes"], 100)
        self.assertEqual(rows[3]["entryBytes"] - rows[3]["exitBytes"], 100)
        self.assertEqual(report["endLeafModules"], {})

    def test_bad_streams(self):
        original = trace()
        cases = [original[:-1], original[:11], [s.replace("10500 3", "10600 3") for s in original],
                 [s.replace("a 11 2", "a 12 2") for s in original],
                 [*original, "- 0"], [*original, "c 2", "c 1"],
                 [*original, "+ -1"], [*original, "t -1 0"], [*original, "i 3 -1 0"],
                 [*original, "i 3"],
                 [*original[:11], "+ ff", *original[11:]]]
        for lines in cases:
            with self.subTest(tail=lines[-2:]), self.assertRaises(ValueError):
                summarize(lines)


if __name__ == "__main__":
    unittest.main()
