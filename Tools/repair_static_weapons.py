"""Rebuild only the two severely reduced static pistols from the supplied archives."""
import json,zipfile
from pathlib import Path
import gdown
from prepare_models import convert
stage=Path('.asset-staging/pistol-repair');stage.mkdir(parents=True,exist_ok=True)
for id,name in [('1vU_MdH2D1P8hEqXs8HY0fjoAZONIR2BX','first'),('1Sykikr17jBuzRYziYSC5I8_YFNvkE1ot','second')]:
    target=stage/(name+'.zip');gdown.download(id=id,output=str(target),quiet=True)
    with zipfile.ZipFile(target) as archive:
        for entry in archive.infolist():
            if entry.filename.lower().endswith('.glb') and ('glock' in entry.filename.lower() or 'gun pistol' in entry.filename.lower()):
                path=stage/Path(entry.filename).name;path.write_bytes(archive.read(entry));print('Selected',path.name,flush=True)
catalog_path=Path('Assets/PackModels/catalog.json');catalog=json.loads(catalog_path.read_text())
for source in stage.glob('*.glb'):
    model=convert(source,Path('Assets/PackModels'),budget_total=32000,preserve_uv=True)
    model['conversion']='Texture-aware QEM preserving source atlas; rendered two-sided for thin scanned surfaces'
    count=sum(p['triangles'] for p in model['parts']);assert count<100000, (source.name,count)
    matches=[i for i,m in enumerate(catalog['models']) if m['name']==model['name']];assert len(matches)==1
    catalog['models'][matches[0]]=model
    print('::notice::Repaired '+model['name']+': '+str(count)+' triangles',flush=True)
assert len(list(stage.glob('*.glb')))==2,'Both original static pistols must be found'
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n')
