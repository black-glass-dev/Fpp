import json
from pathlib import Path
import tempfile
import unittest
import numpy as np
from PIL import Image
from create_village_surfaces import create

class CompactPara(unittest.TestCase):
    def test_every_original_source_has_an_explicit_small_parcel(self):
        inventory=json.loads(Path('QA/village-source-inventory.json').read_text())
        layout=json.loads(Path('Assets/Editor/VillageLayout.json').read_text())
        expected={Path(e['path']).stem for p in inventory['packs'] for e in p['entries']}
        self.assertEqual(expected,{p['name'] for p in layout['models']})
        self.assertEqual(18,len(layout['models']))
        self.assertEqual((60,60),(layout['halfWidth'],layout['halfDepth']))
        self.assertLess(120*120,600*440*.06)
        for parcel in layout['models']:
            self.assertLessEqual(abs(parcel['x'])+7,60)
            self.assertLessEqual(abs(parcel['z'])+7,60)
            if parcel['width']:
                self.assertEqual(4.2,parcel['width'])
            else:self.assertTrue(3<=parcel['height']<=8)
        self.assertEqual(24,len(layout['treeAnchors']))
        self.assertEqual(21,sum(-60<t['x']<0 for t in layout['treeAnchors']))
        # Conservative maximum13.5m city parcels leave a2.5m gap for the2.4m alley.
        city=[m for m in layout['models'] if m['x'] in (34,50)]
        self.assertEqual(7,len(city))
        self.assertGreaterEqual(50-34-13.5,2.4)
        self.assertGreater(24-4.2/2,16+10/2)
        self.assertGreaterEqual(34-13.5/2,24+4.2/2)

    def test_full_quality_tree_mix_is_explicit_and_preserves_density(self):
        layout=json.loads(Path('Assets/Editor/VillageLayout.json').read_text())
        indices=[m['treeIndices'] for m in layout['models'] if 'treeIndices' in m]
        self.assertEqual([1,4,19],sorted(map(len,indices)))
        self.assertEqual(list(range(24)),sorted(i for group in indices for i in group))
        heavy=next(m for m in layout['models'] if m['name']=='Mango tree')
        self.assertEqual([23],heavy['treeIndices'])
        workload=json.loads(Path('QA/village-source-workload.json').read_text())
        counts={Path(m['source']).stem:m['fullDetailNodeTriangleInstances'] for m in workload['models']}
        before=sum(counts[m['name']]*8 for m in layout['models'] if 'treeIndices' in m)
        after=sum(counts[m['name']]*len(m['treeIndices']) for m in layout['models'] if 'treeIndices' in m)
        self.assertLess(after,before*.2) # layout allocation, not per-model decimation or FPS certificate

    def test_all_paths_form_one_connected_walk_network(self):
        lanes=json.loads(Path('Assets/Editor/VillageLayout.json').read_text())['lanes']
        def touches(a,b):
            return abs(a['x']-b['x'])<=(a['width']+b['width'])/2+.001 and abs(a['z']-b['z'])<=(a['length']+b['length'])/2+.001
        reached={0}
        for _ in lanes:
            reached|={i for i,a in enumerate(lanes) if any(touches(a,lanes[j]) for j in reached)}
        self.assertEqual(len(lanes),len(reached),'No disconnected cosmetic road strips')
        for lane in lanes:
            self.assertLessEqual(abs(lane['x'])+lane['width']/2,60)
            self.assertLessEqual(abs(lane['z'])+lane['length']/2,60)
        shader=Path('Assets/Shaders/FppUrpWorld.hlsl').read_text()
        self.assertIn('road=saturate(_Road)',shader)
        self.assertNotIn('_Road*max(lane,yard)',shader)

    def test_full_detail_kept_instead_of_simultaneous_lods_or_proxy_rendering(self):
        source=Path('Assets/Editor/VillageExpansion.cs').read_text()
        for token in ['FullDetailOnly(go)','group.enabled=false','UCX_','_LOD1','_LOD2','!Helper(filter.transform)','door_','2.1f/doors','plan.width/bounds.size.x']:
            self.assertIn(token,source)
        self.assertNotIn('entry.pack=="House"?18',source)
        self.assertNotIn('new Vector3(600,2,1)',source)

    def test_metric_bricks_are_centimetres_not_uv_stretched_metre_slabs(self):
        source=Path('Assets/Editor/VillageExpansion.cs').read_text()
        self.assertIn('1/1.92f',source)
        self.assertAlmostEqual(.24,1.92/8)
        self.assertAlmostEqual(.12,1.92/16)
        shader=Path('Assets/Shaders/FppUrpWorld.hlsl').read_text()
        self.assertIn('FPP_METRIC_DETAIL',shader)
        self.assertIn('i.world.xz',shader)

    def test_original_detail_maps_are_deterministic_nonblank_and_shared(self):
        with tempfile.TemporaryDirectory() as tmp:
            create(tmp,size=64)
            files={p.name:p.read_bytes() for p in Path(tmp).glob('*.png')}
            self.assertEqual(6,len(files));create(tmp,size=64)
            for p in Path(tmp).glob('*.png'):
                self.assertEqual(files[p.name],p.read_bytes())
                if 'BaseColor' in p.name:self.assertGreater(np.asarray(Image.open(p)).std(),1)
        repair=Path('Assets/Editor/VillageSurfaceRepair.cs').read_text()
        self.assertIn('source.GetTexture("baseColorTexture")!=null)continue',repair)
        self.assertIn('new Material(source)',repair)
        self.assertIn('kind==null)continue',repair)

    def test_lossless_optimizations_do_not_replace_quality_settings(self):
        optimizer=Path('Assets/Editor/LosslessWorldOptimizer.cs').read_text()
        for token in ['imageContentsHash','graphicsFormat','mipmapCount','wrapModeU','mipMapBias','hash.isValid','StaticOcclusionCulling.Compute()','smallestHole=.25f','~StaticEditorFlags.BatchingStatic']:
            self.assertIn(token,optimizer)
        for forbidden in ['SetResolution','shadowDistance=','renderScale=','SetTriangles','SetVertices','Resize(','SetLODs','fast_simplification']:
            self.assertNotIn(forbidden,optimizer)
        self.assertIn('SHA256.Create()',optimizer)
        self.assertIn('GetRawTextureData<byte>().ToArray()',optimizer)
        self.assertIn('streamingMipmapsPriority',optimizer)
        bootstrap=Path('Assets/Scripts/MobileBootstrap.cs').read_text()
        self.assertIn('currentRenderPipeline==null&&c.dynamicResolution&&PlayerPrefs.GetInt("fpp.visual.drs",0)!=0)ApplyResolution(c)',bootstrap)

    def test_wall_clock_evidence_does_not_hide_slow_frames_or_write_each_frame(self):
        code=Path('Assets/Scripts/FrameTimeEvidence.cs').read_text()
        loop=code.split('private void LateUpdate()')[1].split('public string CopyReport()')[0]
        for forbidden in ['new float','WriteAllText','Array.Sort','ms>1000','Time.deltaTime']:
            self.assertNotIn(forbidden,loop)
        self.assertIn('Stopwatch.GetTimestamp()',loop)
        self.assertIn('maxMs=',code)
        self.assertIn('COPY PERF',Path('Assets/Scripts/MobileHud.cs').read_text())

if __name__=='__main__':unittest.main()
