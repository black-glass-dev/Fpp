"""Derivative only: crop author-scanned debris/base; NEVER decimate retained road or touch images."""
import copy,hashlib,json,struct
from pathlib import Path
import numpy as np
from QA.analysis.profile_road_geometry import accessor,matrix

# Chosen from the original world-space height diagnostic, not arbitrary triangle-count reduction.
CROP=((.24,.74),(.16,.43),(.05,.95))

def clean(source,target):
    source,target=Path(source),Path(target);raw=source.read_bytes();jl=struct.unpack_from('<I',raw,12)[0]
    doc=json.loads(raw[20:20+jl]);offset=20+jl;binary=raw[offset+8:offset+8+struct.unpack_from('<I',raw,offset)[0]]
    assert len(doc['meshes'])==1 and len(doc['meshes'][0]['primitives'])==1, 'Reinspect changed source topology'
    primitive=doc['meshes'][0]['primitives'][0];assert set(primitive['attributes'])=={'POSITION','NORMAL','TEXCOORD_0'}
    assert primitive.get('mode',4)==4
    transforms=[]
    def visit(i,parent):
        n=doc['nodes'][i];world=parent@matrix(n)
        if 'mesh' in n:transforms.append(world)
        for child in n.get('children',[]):visit(child,world)
    for root in doc['scenes'][doc.get('scene',0)].get('nodes',[]):visit(root,np.eye(4))
    assert len(transforms)==1
    attrs={k:accessor(doc,binary,a) for k,a in primitive['attributes'].items()}
    assert all(a.dtype==np.dtype('<f4') for a in attrs.values()), 'Do not silently requantize source attributes'
    original=np.concatenate([attrs['POSITION'],attrs['NORMAL'],attrs['TEXCOORD_0']],axis=1)
    world=attrs['POSITION']@transforms[0][:3,:3].T+transforms[0][:3,3];low=world.min(0);span=world.max(0)-low;normalized=(world-low)/span
    tris=accessor(doc,binary,primitive['indices']).reshape(-1,3).astype(np.int64);coords=normalized[tris]
    inside=np.ones(len(tris),bool);candidate=np.ones(len(tris),bool)
    for axis,(lo,hi) in enumerate(CROP):
        inside&=((coords[:,:,axis]>=lo)&(coords[:,:,axis]<=hi)).all(1)
        candidate&=~((coords[:,:,axis]<lo).all(1)|(coords[:,:,axis]>hi).all(1))
    kept=tris[inside];used,remap=np.unique(kept,return_inverse=True);output=[original[used]];indices=remap.tolist();extras=[]
    def clip(poly,axis,bound,lower):
        if not poly:return []
        result=[];prev=poly[-1];pv=prev[8+axis];pin=pv>=bound if lower else pv<=bound
        for curr in poly:
            cv=curr[8+axis];cin=cv>=bound if lower else cv<=bound
            if cin!=pin:result.append(prev+(curr-prev)*((bound-pv)/(cv-pv)))
            if cin:result.append(curr)
            prev,pv,pin=curr,cv,cin
        return result
    for tri in tris[candidate&~inside]:
        poly=[np.concatenate([original[v].astype('f8'),normalized[v]]) for v in tri]
        for axis,(lo,hi) in enumerate(CROP):poly=clip(clip(poly,axis,lo,True),axis,hi,False)
        if len(poly)<3:continue
        for i in range(1,len(poly)-1):
            triangle=[poly[0],poly[i],poly[i+1]]
            if np.linalg.norm(np.cross(triangle[1][:3]-triangle[0][:3],triangle[2][:3]-triangle[0][:3]))<1e-12:continue
            for vertex in triangle:indices.append(len(used)+len(extras));extras.append(vertex[:8])
    if extras:output.append(np.array(extras,dtype='<f4'))
    vertices=np.concatenate(output).astype('<f4');assert len(indices)>0
    assert np.array_equal(vertices[:len(used)],original[used]),'Retained source attributes must stay exact'
    newdoc=copy.deepcopy(doc);payload=bytearray(binary)
    def append(array,kind):
        while len(payload)%4:payload.append(0)
        start=len(payload);data=array.tobytes();payload.extend(data)
        vi=len(newdoc['bufferViews']);newdoc['bufferViews'].append({'buffer':0,'byteOffset':start,'byteLength':len(data)})
        ai=len(newdoc['accessors']);a={'bufferView':vi,'componentType':5125 if kind=='SCALAR' else 5126,'count':len(array),'type':kind}
        if kind=='VEC3':a.update(min=array.min(0).tolist(),max=array.max(0).tolist())
        newdoc['accessors'].append(a);return ai
    p=newdoc['meshes'][0]['primitives'][0]
    p['attributes']={name:append(np.ascontiguousarray(vertices[:,start:end]),kind) for name,start,end,kind in [('POSITION',0,3,'VEC3'),('NORMAL',3,6,'VEC3'),('TEXCOORD_0',6,8,'VEC2')]}
    p['indices']=append(np.array(indices,dtype='<u4'),'SCALAR');newdoc['buffers'][0]['byteLength']=len(payload)
    report={'source':source.name,'sourceSha256':hashlib.sha256(raw).hexdigest(),'status':'Derived road-only crop, not lossless full-scene optimization. Removed debris and scan base are explicitly authorized content deletion. Retained interior road positions/normals/UVs exact; only cut-boundary vertices interpolated. No decimation, texture edit or shader change.','normalizedWorldCrop':CROP,'originalTriangles':len(tris),'untouchedInteriorTriangles':int(inside.sum()),'derivedTriangles':len(indices)//3,'boundaryVertexCount':len(extras),'retainedAttributesExact':True,'originalImageBinaryPrefixExact':bytes(payload[:len(binary)])==binary}
    newdoc.setdefault('extras',{})['FppRoadDerivative']=report
    encoded=json.dumps(newdoc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);payload+=b'\0'*((-len(payload))%4)
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(struct.pack('<III',0x46546c67,2,12+8+len(encoded)+8+len(payload))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(payload),0x004e4942)+payload)
    return report

if __name__=='__main__':
    source=next(Path('.asset-staging/village/Road').rglob('Road(ready to use).glb'))
    report=clean(source,'.asset-staging/road-cleanup/Road-clean-core.glb')
    Path('QA/RoadGeometry/derived-road-report.json').write_text(json.dumps(report,indent=2)+'\n')
