"""Select the installed software Vulkan driver for headless Linux CI."""

import json
import os
from pathlib import Path


def environment(directory):
    candidates = list(directory.glob('lvp_icd*.json'))
    if len(candidates) != 1:
        raise ValueError('Expected one installed Lavapipe ICD manifest.')
    manifest = candidates[0].resolve(strict=True)
    if '\n' in str(manifest) or '\r' in str(manifest):
        raise ValueError('The ICD path must fit one environment line.')
    configuration = json.loads(manifest.read_text())
    driver = configuration.get('ICD') if isinstance(configuration, dict) else None
    library = driver.get('library_path') if isinstance(driver, dict) else None
    if not isinstance(library, str) or Path(library).name != 'libvulkan_lvp.so':
        raise ValueError('The selected manifest must name libvulkan_lvp.so.')
    return {'VK_DRIVER_FILES': str(manifest), 'VK_ICD_FILENAMES': str(manifest)}


if __name__ == '__main__':
    selected = environment(Path('/usr/share/vulkan/icd.d'))
    with open(os.environ['GITHUB_ENV'], 'a') as output:
        for name, value in selected.items():
            output.write(f'{name}={value}\n')
    print(json.dumps(selected))
