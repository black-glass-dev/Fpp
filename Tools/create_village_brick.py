"""Original deterministic fired-brick basket weave maps; no supplied pixels overwritten."""
from pathlib import Path
import numpy as np
from PIL import Image

def create(output=Path('Assets/Art/VillageBrick'),size=2048):
    output.mkdir(parents=True,exist_ok=True)
    y,x=np.mgrid[0:size,0:size];cell=size//8
    bx=x//cell;by=y//cell;horizontal=(bx+by)%2==0
    u=np.where(horizontal,x%cell,y%cell);v=np.where(horizontal,y%cell,x%cell)
    half=cell//2;edge=np.minimum.reduce([u,cell-1-u,v%half,half-1-v%half]).astype(float)
    mortar=edge<4;bevel=np.clip((edge-3)/5,0,1)
    rng=np.random.default_rng(4206);fine=rng.normal(0,1,(size,size))
    variation=np.sin((bx*13+by*31+v//half*7)*2.31)*12
    grain=fine*3+np.sin(x*.073+y*.11)*2
    rgb=np.stack([154+variation+grain,69+variation*.55+grain,43+variation*.3+grain],axis=-1)
    rgb*=np.where(mortar,.56,.85+.15*bevel)[...,None]
    rgb=np.clip(rgb,0,255).astype('uint8')
    Image.fromarray(rgb).save(output/'FiredBrick_BaseColor.png',optimize=True)
    height=bevel*.9+fine*.015;dy,dx=np.gradient(height)
    normal=np.stack([-dx*2,-dy*2,np.ones_like(dx)],axis=-1);normal/=np.linalg.norm(normal,axis=-1)[...,None]
    Image.fromarray(np.clip((normal*.5+.5)*255,0,255).astype('uint8')).save(output/'FiredBrick_Normal.png',optimize=True)

if __name__=='__main__':create()
