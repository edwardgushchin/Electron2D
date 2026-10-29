from pathlib import Path
import subprocess,tempfile
root=Path(__file__).parent
with tempfile.TemporaryDirectory(prefix='e2d-animation-oracle-') as tmp:
 exe=str(Path(tmp)/'oracle')
 subprocess.run(['c++','-std=c++17','-O2',str(root/'oracle.cpp'),'-o',exe],check=True)
 actual=subprocess.check_output([exe])
 assert actual==(root/'reference.tsv').read_bytes(), 'Reference endpoint vectors changed.'
 print('Animation endpoint independent oracle:',len(actual.splitlines()),'vectors reproduced')
