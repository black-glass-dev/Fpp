from pathlib import Path
import json, sys
for arg in sys.argv[1:]:
    catalog = json.loads(Path(arg).read_text())
    assert catalog['models'], 'Empty catalog'
    names = set()
    total = 0
    for model in catalog['models']:
        assert model['name'] not in names, 'Duplicate model name'
        names.add(model['name'])
        assert model['parts'], 'Empty model'
        for part in model['parts']:
            path = Path(part['model'])
            assert path.is_relative_to('Assets') and '..' not in path.parts
            assert path.is_file(), path
            text = path.read_text()
            assert '\nv ' in text and '\nf ' in text, path
            faces = sum(1 for line in text.splitlines() if line.startswith('f '))
            assert faces == part['triangles'], (path, faces, part['triangles'])
            total += faces
            if part['texture']:
                assert Path(part['texture']).is_file(), part['texture']
            assert len(part['color']) == 4
    print(arg, ':', len(names), 'models,', total, 'triangles; references and counts valid')
