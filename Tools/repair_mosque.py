"""Restore original atlas UVs rather than nearest-vertex UVs on a heavily reduced scan."""
import json,runpy
from pathlib import Path
from prepare_models import convert
runpy.run_path('Tools/fetch_mosque.py',run_name='__main__')
models=[]
for source in sorted(Path('.asset-staging/mosque/extracted').rglob('*.glb')):
    model=convert(source,Path('Assets/MosquePack'),budget_total=128000,preserve_uv=True,preserve_boundary=True)
    model['conversion']='UV-preserving textured QEM; original chart boundary, normal and UV preservation'
    models.append(model)
    print('::notice::UV-aware triangle count: '+str(sum(p['triangles'] for p in model['parts'])),flush=True)
Path('Assets/MosquePack/catalog.json').write_text(json.dumps(dict(models=models),indent=2)+'\n')
size=sum(p.stat().st_size for p in Path('Assets/MosquePack').rglob('*') if p.is_file())
assert sum(p['triangles'] for m in models for p in m['parts'])<350000
assert size<64*1024**2, f'Converted scan over mobile asset budget: {size}'
print('::notice::UV-preserving mosque conversion: '+str(sum(p['triangles'] for m in models for p in m['parts']))+' triangles; '+str(size)+' bytes')

# Fill unused near-black atlas voids, not valid brick/stone image detail.
from PIL import Image
import numpy as np
from scipy.ndimage import distance_transform_edt
for path in Path('Assets/MosquePack').rglob('Texture_*.png'):
    a=np.array(Image.open(path).convert('RGBA'));mask=np.max(a[:,:,:3],2)<22
    if mask.any():
        indices=distance_transform_edt(mask,return_distances=False,return_indices=True)
        a[mask,:3]=a[indices[0][mask],indices[1][mask],:3]
        Image.fromarray(a).save(path,optimize=True)
notice=Path('Assets/Resources/MOSQUE_NOTICE.txt')
notice.write_text(notice.read_text()+'\nAdditional changes: scan chart boundaries preserved during reduction; near-black atlas voids filled; procedural foundation and internal backing added.\n')
