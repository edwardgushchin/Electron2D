"""Report an aggregate CI check without treating skipped or cancelled work as success."""

import json
import os
import sys


def check(results, group):
    if not isinstance(results, dict):
        raise ValueError("Missing CI dependencies")
    if group == 'development':
        if set(results) != {'plan', 'fast', 'build_status', 'tests_status', 'qualify'}:
            raise ValueError('Missing or unexpected development dependencies')
        if any(not isinstance(item, dict) for item in results.values()):
            raise ValueError('Malformed development dependency')
        full = results['plan'].get('outputs', {}).get('full')
        if full not in {'true', 'false'}:
            raise ValueError('Missing qualification plan')
        expected = {'plan': 'success', 'fast': 'success',
                    'build_status': 'success' if full == 'true' else 'skipped',
                    'tests_status': 'success' if full == 'true' else 'skipped',
                    'qualify': 'success' if full == 'true' else 'skipped'}
        if any(not isinstance(results[name], dict) or results[name].get('result') != state
               for name, state in expected.items()):
            raise RuntimeError('Development checks did not pass their planned scope')
        return
    expected = {'matrix', 'native', group}
    if group not in {'build', 'tests'} or set(results) not in (expected, expected | {'plan'}):
        raise ValueError("Missing or unexpected CI dependencies")
    failed = {name: item.get("result") if isinstance(item, dict) else None
              for name, item in results.items() if not isinstance(item, dict) or item.get("result") != "success"}
    if failed:
        raise RuntimeError(f"CI dependencies did not pass: {failed}")


if __name__ == "__main__":
    check(json.loads(os.environ["CI_RESULTS"]), sys.argv[1])
    print("All required CI dependencies passed")
