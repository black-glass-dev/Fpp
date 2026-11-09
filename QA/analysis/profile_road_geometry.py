"""Read-only original road geometry diagnostic; not a rendered gameplay image."""
import json,struct
from pathlib import Path
import numpy as np
from PIL import Image

def accessor(doc,binary,index):
    a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
    dtype={5121:'u1',5123:'<u2',5125:'<u4',5126:'<f4'}[a['componentType']]
    n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']];dt=np.dtype(dtype)
    return np.ndarray((a['count'],n),dt,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',dt.itemsize*n),dt.itemsize)).copy()

def matrix(node):
    if 'matrix' in node:return np.array(node['matrix']).reshape(4,4).T
    x,y,z,w=node.get('rotation',[0,0,0,1]);m=np.eye(4)
    m[:3,:3]=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])@np.diag(node.get('scale',[1,1,1]))
    m[:3,3]=node.get('translation',[0,0,0]);return m

def main(root='.asset-staging/village/Road',folder='QA/RoadGeometry'):
    out=Path(folder);out.mkdir(exist_ok=True);rows=[]
    for path in sorted(Path(root).rglob('*.glb')):
        data=path.read_bytes();length=struct.unpack_from('<I',data,12)[0];doc=json.loads(data[20:20+length]);start=20+length;binary=data[start+8:start+8+struct.unpack_from('<I',data,start)[0]]
        points=[];parts=[]
        def walk(index,parent):
            node=doc['nodes'][index];world=parent@matrix(node)
            if 'mesh' in node:
                for pi,p in enumerate(doc['meshes'][node['mesh']]['primitives']):
                    xyz=accessor(doc,binary,p['attributes']['POSITION']);xyz=xyz@world[:3,:3].T+world[:3,3];points.append(xyz)
                    parts.append({'node':node.get('name',''),'primitive':pi,'vertices':len(xyz),'indices':doc['accessors'][p['indices']]['count'] if 'indices' in p else len(xyz),'material':p.get('material'),'attributes':list(p['attributes'])})
            for child in node.get('children',[]):walk(child,world)
        for root in doc['scenes'][doc.get('scene',0)].get('nodes',[]):walk(root,np.eye(4))
        xyz=np.concatenate(points);low=xyz.min(0);span=xyz.max(0)-low;metres=(xyz-low)*(4.2/span[0]);normalized=(xyz-low)/np.maximum(span,1e-12)
        center=metres[(abs(normalized[:,0]-.5)<.2)&(abs(normalized[:,2]-.5)<.3)]
        image=np.full((768,512),-np.inf);x=np.minimum((normalized[:,0]*511).astype(int),511);z=np.minimum((normalized[:,2]*767).astype(int),767);np.maximum.at(image,(767-z,x),metres[:,1]);valid=np.isfinite(image)
        rgb=np.zeros((*image.shape,3),dtype=np.uint8);h=np.nan_to_num(np.where(valid,image,0))/max(float(metres[:,1].max()),1e-8)
        rgb[:,:,0]=(h*255).astype('u1');rgb[:,:,1]=((1-h)*190*valid).astype('u1');rgb[:,:,2]=(valid*70).astype('u1')
        key='ready-road' if (path.name.startswith('Road(') or 'clean-core' in path.name) else 'brick-road';Image.fromarray(rgb).save(out/(key+'-height.png'))
        rows.append({'source':path.name,'status':'READ-ONLY geometry diagnostic. Red=high, green=low; black=unsampled. Not gameplay rendering. No cleanup/crop applied.','parts':parts,'sizeAt4_2mWidth':(span*4.2/span[0]).tolist(),'allVertexHeightPercentiles':np.percentile(metres[:,1],[0,5,25,50,75,95,100]).tolist(),'centralVertexHeightPercentiles':np.percentile(center[:,1],[0,5,25,50,75,95,100]).tolist() if len(center) else [],'heightMap':key+'-height.png'})
    (out/'original-road-geometry.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
