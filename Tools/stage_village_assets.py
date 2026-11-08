"""Stage every unchanged supplied GLB for Unity's pinned glTF importer. CI only."""
import json
import shutil
from pathlib import Path
from audit_village_archives import main as download_and_audit


def main():
    download_and_audit()
    inventory = json.loads(Path('QA/village-source-inventory.json').read_text())
    output = Path('Assets/Generated/Village'); output.mkdir(parents=True, exist_ok=True)
    models = []
    for pack in inventory['packs']:
        for index, entry in enumerate(pack['entries']):
            if entry['kind'] != 'model':
                raise RuntimeError('New dependency requires explicit staging; do not silently omit it')
            if Path(entry['path']).suffix.lower() != '.glb':
                raise RuntimeError('Unsupported source needs a lossless importer')
            source = Path('.asset-staging/village') / pack['name'] / entry['path']
            target = output / (pack['name'] + '_' + str(index).zfill(2) + '.glb')
            shutil.copyfile(source, target)
            models.append({'pack': pack['name'], 'name': Path(entry['path']).stem, 'assetPath': target.as_posix(), 'sourcePath': entry['path']})
    if len(models) != 18:
        raise RuntimeError('Expected all18 supplied models; inventory changed')
    from create_village_brick import create
    create(output / 'BrickTextures')
    (output / 'catalog.json').write_text(json.dumps({'models': models}, ensure_ascii=False, indent=2) + '\n')
    print('Staged all18 source GLBs unchanged; no decimation or texture resizing')


if __name__ == '__main__':
    main()
