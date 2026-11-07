"""Original deterministic tileable ground art, not photographic scans or MWII assets.
2048px albedo + 1024px height-derived tangent normals, palette PNG storage.
Only grass/dirt are replaced; supplied model textures remain untouched.
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
N=2048
root=Path('Assets/Art/Ground')
rng=np.random.default_rng(44138)
def noise(cells):
    grid=rng.random((cells,cells),dtype=np.float32)
    t=np.arange(N,dtype=np.float32)*cells/N;i=t.astype(int);f=t-i;f=f*f*(3-2*f)
    a=grid[i[:,None]%cells,i[None,:]%cells];b=grid[i[:,None]%cells,(i[None,:]+1)%cells]
    c=grid[(i[:,None]+1)%cells,i[None,:]%cells];d=grid[(i[:,None]+1)%cells,(i[None,:]+1)%cells]
    return (a*(1-f[None,:])+b*f[None,:])*(1-f[:,None])+(c*(1-f[None,:])+d*f[None,:])*f[:,None]
macro=noise(4);clods=noise(32);grain=noise(256);fine=rng.random((N,N),dtype=np.float32)
soil_height=macro*.18+clods*.32+grain*.13+fine*.025
soil=(np.array([103,80,54])[None,None,:]+(macro-.5)[...,None]*38+(clods-.5)[...,None]*40+(grain-.5)[...,None]*32+(fine-.5)[...,None]*13)
soil=Image.fromarray(np.uint8(np.clip(soil,0,255)));height=Image.fromarray(np.uint8(np.clip(soil_height,0,1)*255));draw=ImageDraw.Draw(soil);hd=ImageDraw.Draw(height)
for _ in range(8500):
    x,y=rng.integers(0,N,2);r=int(rng.integers(2,10));tone=int(rng.integers(65,150));box=(int(x-r),int(y-r*.65),int(x+r),int(y+r*.65))
    draw.ellipse(box,fill=(tone+12,tone+5,tone-8));draw.arc(box,15,165,fill=(max(0,tone-18),max(0,tone-23),max(0,tone-30)),width=1);hd.ellipse(box,fill=int(rng.integers(120,175)))
soil.filter(ImageFilter.GaussianBlur(.45)).quantize(colors=128,dither=Image.Dither.NONE).save(root/'Dirt.png',optimize=True)
def normal(height,path):
    h=np.asarray(height.resize((1024,1024),Image.Resampling.LANCZOS),dtype=np.float32)/255
    dx=(np.roll(h,-1,1)-np.roll(h,1,1))*4.2;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*4.2
    n=np.stack((-dx,-dy,np.ones_like(h)),2);n/=np.linalg.norm(n,axis=2,keepdims=True)
    image=Image.fromarray(np.uint8(np.clip(n*.5+.5,0,1)*255));image.quantize(colors=128,dither=Image.Dither.NONE).save(path,optimize=True)
normal(height,root/'DirtNormal.png')
green=(np.array([66,77,39])[None,None,:]+(macro-.5)[...,None]*42+(grain-.5)[...,None]*18+(fine-.5)[...,None]*10)
grass=Image.fromarray(np.uint8(np.clip(green,0,255)));gh=Image.fromarray(np.uint8(np.clip(.3+clods*.2+grain*.08,0,1)*255));gd=ImageDraw.Draw(grass);gheight=ImageDraw.Draw(gh)
for _ in range(39000):
    x,y=rng.integers(0,N,2);length=rng.uniform(8,34);angle=rng.uniform(0,np.pi*2);tone=int(rng.integers(-18,38));width=int(rng.integers(1,5));dx=np.cos(angle)*length;dy=np.sin(angle)*length
    for ox,oy in [(0,0),(-N,0),(N,0),(0,-N),(0,N)]:
        a=(int(x+ox),int(y+oy));b=(int(x+dx+ox),int(y+dy+oy));gd.line([a,b],fill=(65+tone,81+tone,34+tone//2),width=width);gheight.line([a,b],fill=170+tone,width=width)
grass.filter(ImageFilter.GaussianBlur(.45)).quantize(colors=128,dither=Image.Dither.NONE).save(root/'Grass.png',optimize=True);normal(gh,root/'GrassNormal.png')
for name in ['Grass','Dirt','GrassNormal','DirtNormal']:
    p=root/(name+'.png');print(p,p.stat().st_size)
