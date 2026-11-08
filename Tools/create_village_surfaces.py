"""Original 2048px neutral detail maps for untextured opaque authored surfaces.
Existing image textures are never resized or overwritten. Colour factors remain material tints.
"""
from pathlib import Path
import numpy as np
from PIL import Image

def create(output, size=2048):
    output=Path(output);output.mkdir(parents=True,exist_ok=True)
    y,x=np.mgrid[0:size,0:size];rng=np.random.default_rng(713)
    grain=rng.normal(0,1,(size,size))
    fields={
        'Plaster': .88+grain*.022+.025*np.sin(x*.019)*np.sin(y*.013),
        'Concrete': .76+grain*.035+.035*np.sin(x*.041+y*.027),
        'Wood': .72+.075*np.sin(x*.12+np.sin(y*.009)*2)+.035*np.sin(x*.41)+grain*.015,
    }
    for name,value in fields.items():
        rgb=np.repeat(np.clip(value[...,None]*255,0,255).astype('uint8'),3,axis=2)
        Image.fromarray(rgb).save(output/(name+'_BaseColor.png'),optimize=True)
        dy,dx=np.gradient(value);normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1)
        normal/=np.linalg.norm(normal,axis=-1)[...,None]
        Image.fromarray(np.clip((normal*.5+.5)*255,0,255).astype('uint8')).save(output/(name+'_Normal.png'),optimize=True)
if __name__=='__main__':create('Assets/Generated/Village/SurfaceTextures')
