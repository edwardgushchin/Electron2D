"""Verify generated allocator overrides and reject both changed pinned inputs."""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

recipe = Path(__file__).resolve().with_name('memory.cmake')
upstream = Path(sys.argv[1]).resolve()
names = ['src/utils/utils.c', 'sharpyuv/sharpyuv.c']
with tempfile.TemporaryDirectory(prefix='e2d-webp-source-check-') as directory:
    project = Path(directory)
    source = project / 'source'
    for name in names:
        file = source / name
        file.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(upstream / name, file)
    (project / 'CMakeLists.txt').write_text(f'''cmake_minimum_required(VERSION 3.24)
project(SourceCheck C)
set(electron2dwebpcodec_SOURCE_DIR "{source.as_posix()}")
foreach(target webputils webputilsdecode)
  add_library(${{target}} OBJECT "${{electron2dwebpcodec_SOURCE_DIR}}/src/utils/utils.c")
endforeach()
add_library(sharpyuv OBJECT "${{electron2dwebpcodec_SOURCE_DIR}}/sharpyuv/sharpyuv.c")
include("{recipe.as_posix()}")
''', encoding='utf-8')
    def configure():
        return subprocess.run(['cmake', '-S', str(project), '-B', str(project / 'build')], capture_output=True, text=True)
    result = configure()
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    for name in names:
        if (source / name).read_bytes() != (upstream / name).read_bytes():
            raise RuntimeError('Preparation changed the pinned source')
        prepared = project / 'build/memory-sources' / Path(name).name
        if 'e2d_webp_allocate(' not in prepared.read_text(encoding='utf-8'):
            raise RuntimeError('Prepared source omits allocation ownership')
        file = source / name
        file.write_bytes(file.read_bytes() + b'\n/* changed */\n')
        result = configure()
        if not result.returncode or 'Pinned WebP allocator input changed' not in result.stderr:
            raise RuntimeError('Changed source was accepted: ' + name)
        shutil.copy2(upstream / name, file)
print('Two generated allocator sources preserve pinned input and reject corruption.')
