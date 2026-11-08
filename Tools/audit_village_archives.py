"""Fetch supplied archives on CI, inventory EVERY entry; never alter source art."""
import hashlib
import json
from pathlib import Path, PurePosixPath
import stat
import struct
import zipfile

PACKS = [
    ('Road', '1UwMOC-ouTG1iee02v3ZH76mdwOWqkuA_', 73435128),
    ('Tree', '1l8xdlzdBF5Ngz16wZxKZa4zq8fKkA5y6', 14175387),
    ('House', '1rnZjNBUcW_3OGDsxxoMqSB-_oJfrfUCq', 94866448),
]
MODELS = {'.glb', '.gltf', '.fbx', '.obj', '.blend', '.dae', '.3ds', '.stl'}
TEXTURES = {'.png', '.jpg', '.jpeg', '.tga', '.webp', '.bmp', '.exr', '.ktx2', '.dds'}


def safe_entries(archive):
    entries = archive.infolist()
    if len(entries) > 50000 or sum(e.file_size for e in entries) > 4 * 1024**3:
        raise ValueError('Archive exceeds bounded extraction budget')
    seen = set()
    for entry in entries:
        name = entry.filename.replace('\\', '/')
        path = PurePosixPath(name)
        if path.is_absolute() or '..' in path.parts or ':' in name or '\x00' in name:
            raise ValueError('Unsafe archive member')
        if stat.S_ISLNK(entry.external_attr >> 16):
            raise ValueError('Archive symlinks are not allowed')
        if name in seen:
            raise ValueError('Duplicate archive member; refusing silent overwrite')
        seen.add(name)
    return entries


def glb_summary(path):
    with path.open('rb') as stream:
        header = stream.read(20)
        if len(header) != 20 or header[:4] != b'glTF' or header[16:20] != b'JSON':
            raise ValueError('Invalid GLB header')
        length = struct.unpack_from('<I', header, 12)[0]
        if length > 32 * 1024**2:
            raise ValueError('GLB metadata exceeds budget')
        document = json.loads(stream.read(length))
    return {
        'asset': document.get('asset', {}),
        'meshCount': len(document.get('meshes', [])),
        'nodeNames': [n.get('name', '') for n in document.get('nodes', [])],
        'materialCount': len(document.get('materials', [])),
        'imageCount': len(document.get('images', [])),
        'materialsWithoutBaseColorImage': [m.get('name', str(i)) for i, m in enumerate(document.get('materials', [])) if 'baseColorTexture' not in m.get('pbrMetallicRoughness', {})],
        'animations': len(document.get('animations', [])),
        'skins': len(document.get('skins', [])),
    }


def main():
    import gdown
    root = Path('.asset-staging/village'); root.mkdir(parents=True, exist_ok=True)
    result = {'status': 'downloaded/inventoried; NOT scene integration or visual acceptance', 'textureEdits': False, 'packs': []}
    for label, file_id, expected in PACKS:
        archive_path = root / (label + '.zip')
        gdown.download(id=file_id, output=str(archive_path), quiet=True)
        if not archive_path.exists() or archive_path.stat().st_size != expected:
            raise ValueError(label + ' download size differs from Drive metadata')
        pack = {'name': label, 'driveId': file_id, 'bytes': expected, 'sha256': hashlib.sha256(archive_path.read_bytes()).hexdigest(), 'entries': []}
        with zipfile.ZipFile(archive_path) as archive:
            entries = safe_entries(archive)
            if archive.testzip() is not None:
                raise ValueError('Archive CRC failure')
            destination = root / label
            for entry in entries:
                name = entry.filename.replace('\\', '/')
                path = destination / name
                if entry.is_dir():
                    path.mkdir(parents=True, exist_ok=True); continue
                path.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(entry) as source, path.open('wb') as target:
                    import shutil
                    shutil.copyfileobj(source, target)
                suffix = path.suffix.lower()
                record = {'path': name, 'bytes': entry.file_size, 'kind': 'model' if suffix in MODELS else 'texture' if suffix in TEXTURES else 'dependency-or-document', 'integration': 'pending inspection; not silently excluded'}
                if suffix == '.glb':
                    record['gltf'] = glb_summary(path)
                pack['entries'].append(record)
        result['packs'].append(pack)
        print(label, 'downloaded, CRC verified;', len(pack['entries']), 'files retained')
    output = Path('QA/village-source-inventory.json')
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')


if __name__ == '__main__':
    main()
