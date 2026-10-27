import tempfile
from pathlib import Path
import unittest

import generate_layout


class LayoutTests(unittest.TestCase):
    def test_nested_declaration(self):
        source = "prefix; typedef struct A { union { int x; float y; }; int z; } A; suffix;"
        self.assertEqual(generate_layout.block(source, "typedef struct A"), source[8:-8])

    def test_truncated_declaration_rejects(self):
        with self.assertRaises(ValueError):
            generate_layout.block("struct A { union { int x; };", "struct A")

    def test_changed_pin_rejects_before_parsing(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in generate_layout.SOURCES:
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("untrusted native declarations")
            with self.assertRaisesRegex(ValueError, "differs from pinned"):
                generate_layout.generate(root)


if __name__ == "__main__":
    unittest.main()
