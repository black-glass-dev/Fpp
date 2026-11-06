from pathlib import Path
import uuid
for p in sorted(Path('Assets').rglob('*')):
    meta = Path(str(p)+'.meta')
    if p.suffix == '.meta' or meta.exists(): continue
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'fpp:'+p.as_posix()).hex
    if p.is_dir():
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
    elif p.suffix == '.cs':
        body = 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
    else:
        # Let Unity choose the correct specialized importer on first import.
        body = ''
    meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+body)
