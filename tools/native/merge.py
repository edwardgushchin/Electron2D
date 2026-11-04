"""Merge the selected target's archive closure without extracting colliding basenames."""
from pathlib import Path
import subprocess
import sys
import tempfile


def merge(output, archiver, inputs):
    if not inputs or output in inputs or any(not path.is_file() for path in inputs):
        raise ValueError("A complete distinct target archive closure is required")
    with tempfile.TemporaryDirectory(dir=output.parent) as temporary:
        candidate = Path(temporary) / output.name
        if sys.platform == "darwin" and Path(archiver).name in ("ar", "libtool"):
            subprocess.run(["xcrun", "libtool", "-static", "-o", str(candidate), *map(str, inputs)], check=True)
        else:
            # MRI scripts do not consistently quote paths; use bounded local names.
            aliases = []
            for index, path in enumerate(inputs):
                alias = Path(temporary) / ("input-" + str(index) + ".a")
                alias.symlink_to(path)
                aliases.append(alias.name)
            script = "CREATE " + candidate.name + "\n"
            script += "".join("ADDLIB " + name + "\n" for name in aliases)
            subprocess.run([archiver, "-M"], input=script + "SAVE\nEND\n",
                           cwd=temporary, text=True, check=True)
        candidate.replace(output)


if __name__ == "__main__":
    merge(Path(sys.argv[1]).resolve(), sys.argv[2], [Path(arg).resolve() for arg in sys.argv[3:]])
