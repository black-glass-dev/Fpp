"""Download the user-supplied public Drive archive on a network-enabled CI runner."""
from pathlib import Path
import json, struct, subprocess, zipfile
import gdown
root = Path('.asset-staging/mosque')
root.mkdir(parents=True, exist_ok=True)
p = root / 'source.zip'
gdown.download(id='16rIZD0UNZ-NS_C6mxbNA4J1xqBFQdDmn', output=str(p), quiet=True)
if not p.exists() or p.stat().st_size != 112897326:
    raise SystemExit('Mosque archive missing or size differs from supplied Drive metadata')
with zipfile.ZipFile(p) as archive:
    entries = archive.infolist()
    if sum(e.file_size for e in entries) > 2 * 1024**3:
        raise SystemExit('Archive exceeds extraction budget')
    for entry in entries:
        path = (root / 'extracted' / entry.filename).resolve()
        if not path.is_relative_to((root / 'extracted').resolve()):
            raise SystemExit('Unsafe archive path')
    archive.extractall(root / 'extracted')
    print('::notice::Mosque archive entries: ' + ', '.join(e.filename for e in entries)[:1500])
# Preserve any embedded author/license/source attribution before static conversion.
metadata=[]
for model in (root / 'extracted').rglob('*.glb'):
    with model.open('rb') as stream:
        header=stream.read(20)
        length=struct.unpack_from('<I',header,12)[0]
        document=json.loads(stream.read(length))
    metadata.append(dict(file=model.name, asset=document.get('asset', {})))
folder=Path('Assets/MosquePack');folder.mkdir(parents=True,exist_ok=True)
(folder / 'source-metadata.json').write_text(json.dumps(metadata,indent=2)+'\n')
notices=['Chunakhola mosque — user-supplied model', 'Changes: static conversion, polygon reduction, texture resizing.']
for entry in metadata:
    extra=entry['asset'].get('extras',{})
    if extra:
        notices.extend(str(key)+': '+str(value) for key,value in extra.items())
    else:
        notices.append('No embedded license metadata. Project owner must verify redistribution rights.')
resources=Path('Assets/Resources');resources.mkdir(exist_ok=True)
(resources / 'MOSQUE_NOTICE.txt').write_text('\n'.join(notices)+'\n')
if not Path('Assets/MosquePack/catalog.json').exists():
    subprocess.run(['python', 'Tools/prepare_models.py', str(root / 'extracted'),
    '--output', 'Assets/MosquePack', '--catalog', 'Assets/MosquePack/catalog.json'], check=True)
if sum(p.stat().st_size for p in Path('Assets/MosquePack').rglob('*') if p.is_file()) > 64 * 1024**2:
    raise SystemExit('Converted mosque exceeds 64 MiB budget; optimize before committing')
catalog = json.loads(Path('Assets/MosquePack/catalog.json').read_text())
print('::notice::Mosque models converted: ' + ', '.join(m['name'] for m in catalog['models'])[:1500])
