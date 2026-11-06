"""Restore original atlas UVs rather than nearest-vertex UVs on a heavily reduced scan."""
import json,runpy
from pathlib import Path
from prepare_models import convert
runpy.run_path('Tools/fetch_mosque.py',run_name='__main__')
models=[]
for source in sorted(Path('.asset-staging/mosque/extracted').rglob('*.glb')):
    model=convert(source,Path('Assets/MosquePack'),budget_total=128000,preserve_uv=True)
    model['conversion']='UV-preserving textured QEM; boundary and normal preservation'
    models.append(model)
    print('::notice::UV-aware triangle count: '+str(sum(p['triangles'] for p in model['parts'])),flush=True)
Path('Assets/MosquePack/catalog.json').write_text(json.dumps(dict(models=models),indent=2)+'\n')
size=sum(p.stat().st_size for p in Path('Assets/MosquePack').rglob('*') if p.is_file())
assert size<32*1024**2, f'Converted scan over mobile asset budget: {size}'
print('::notice::UV-preserving mosque conversion: '+str(sum(p['triangles'] for m in models for p in m['parts']))+' triangles; '+str(size)+' bytes')
