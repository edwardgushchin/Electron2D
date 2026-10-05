"""Report an aggregate CI check without treating skipped or cancelled work as success."""

import json
import os
import sys


def check(results, group):
    if group not in {"build", "tests"} or not isinstance(results, dict) or set(results) != {"matrix", "native", group}:
        raise ValueError("Missing or unexpected CI dependencies")
    failed = {name: item.get("result") if isinstance(item, dict) else None
              for name, item in results.items() if not isinstance(item, dict) or item.get("result") != "success"}
    if failed:
        raise RuntimeError(f"CI dependencies did not pass: {failed}")


if __name__ == "__main__":
    check(json.loads(os.environ["CI_RESULTS"]), sys.argv[1])
    print("All required CI dependencies passed")
