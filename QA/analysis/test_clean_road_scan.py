import json,struct,tempfile,unittest
from pathlib import Path
import numpy as np
from QA.analysis.clean_road_scan import clean,CROP
from QA.analysis.profile_road_geometry import accessor

class RoadCrop(unittest.TestCase):
    def test_fused_debris_removed_boundary_clipped_and_interior_image_bytes_unchanged(self):
        xyz=np.array([[0,0,0],[1,1,1],[.3,.25,.2],[.4,.25,.2],[.3,.25,.3],[.1,.3,.4],[.8,.3,.4],[.4,.3,.9],[.8,.9,.4],[.9,.9,.4],[.9,.9,.5]],dtype='<f4')
        normal=np.tile(np.array([0,1,0],dtype='<f4'),(len(xyz),1));uv=xyz[:,[0,2]].copy();idx=np.array([2,3,4,5,6,7,8,9,10],dtype='<u4');payload=bytearray();views=[];access=[]
        for a,t,c in [(xyz,'VEC3',5126),(normal,'VEC3',5126),(uv,'VEC2',5126),(idx,'SCALAR',5125)]:
            views.append({'buffer':0,'byteOffset':len(payload),'byteLength':a.nbytes});payload.extend(a.tobytes());access.append({'bufferView':len(views)-1,'componentType':c,'count':len(a),'type':t})
        payload.extend(b'UNCHANGED ORIGINAL IMAGE PAYLOAD!')
        doc={'asset':{'version':'2.0'},'buffers':[{'byteLength':len(payload)}],'bufferViews':views,'accessors':access,'meshes':[{'primitives':[{'attributes':{'POSITION':0,'NORMAL':1,'TEXCOORD_0':2},'indices':3}]}],'nodes':[{'mesh':0}],'scenes':[{'nodes':[0]}],'scene':0}
        j=json.dumps(doc).encode();j+=b' '*((-len(j))%4);payload+=b'\0'*((-len(payload))%4)
        raw=struct.pack('<III',0x46546c67,2,28+len(j)+len(payload))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(payload),0x004e4942)+payload
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'original.glb';target=Path(folder)/'clean.glb';source.write_bytes(raw);report=clean(source,target)
            self.assertEqual(raw,source.read_bytes());self.assertTrue(report['originalImageBinaryPrefixExact']);self.assertTrue(report['retainedAttributesExact']);self.assertEqual(1,report['untouchedInteriorTriangles']);self.assertGreater(report['boundaryVertexCount'],0)
            result=target.read_bytes();n=struct.unpack_from('<I',result,12)[0];d=json.loads(result[20:20+n]);b=result[28+n:];p=d['meshes'][0]['primitives'][0];pos=accessor(d,b,p['attributes']['POSITION'])
            np.testing.assert_array_equal(pos[:3],xyz[2:5])
            for axis,(lo,hi) in enumerate(CROP):self.assertGreaterEqual(pos[:,axis].min()+1e-6,lo);self.assertLessEqual(pos[:,axis].max()-1e-6,hi)
            self.assertFalse((pos[:,1]>.43+1e-6).any());self.assertEqual(0,accessor(d,b,p['indices']).size%3)
if __name__=='__main__':unittest.main()
