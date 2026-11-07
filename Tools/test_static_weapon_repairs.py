"""Regression for the actual torn floor/held pistol geometry in phone screenshots."""
import json
from pathlib import Path
import unittest
import numpy as np
import trimesh

class StaticWeaponRepairTests(unittest.TestCase):
    def test_pistols_have_closed_consistently_wound_surfaces(self):
        models=json.loads(Path('Assets/PackModels/catalog.json').read_text())['models']
        for name in ('Glock 17','Gun pistol m'):
            model=next(m for m in models if m['name']==name)
            self.assertIn('Texture-aware QEM',model['conversion'])
            self.assertLessEqual(sum(p['triangles'] for p in model['parts']),100000)
            for part in model['parts']:
                with self.subTest(weapon=name,part=part['model']):
                    mesh=trimesh.load(part['model'],force='mesh',process=False)
                    self.assertTrue(np.isfinite(mesh.vertices).all())
                    self.assertTrue(np.isfinite(mesh.visual.uv).all())
                    # UV/normal seams duplicate vertices in OBJ. Weld only this test copy
                    # to inspect geometric adjacency; never alter the exported UVs.
                    mesh.merge_vertices(merge_tex=True,merge_norm=True)
                    self.assertTrue(mesh.is_watertight,'Open triangle boundaries must not regress')
                    self.assertTrue(mesh.is_winding_consistent,'Inverted faces must not regress')

    def test_mosque_atlas_chunks_still_join_without_torn_edges(self):
        model=json.loads(Path('Assets/MosquePack/catalog.json').read_text())['models'][0]
        self.assertIn('original chart boundary',model['conversion'])
        mesh=trimesh.util.concatenate([trimesh.load(p['model'],force='mesh',process=False) for p in model['parts']])
        mesh.merge_vertices(digits_vertex=5,merge_tex=True,merge_norm=True)
        boundary_counts=np.bincount(mesh.edges_unique_inverse)
        self.assertFalse(np.any(boundary_counts==1),'Separately reduced atlas chunks must not tear apart')
        self.assertTrue(mesh.is_winding_consistent)

    def test_scan_ground_is_clipped_without_stretching_wall_vertices(self):
        shader=Path('Assets/Shaders/MobileScan.shader').read_text()
        vertex=shader[shader.index('v2f vert'):shader.index('fixed4 frag')]
        self.assertNotIn('world.y=',vertex)
        self.assertNotIn('abs(normalize(i.normal).y)>.6)clip(-1)',shader)
        self.assertIn('_InteriorRect',shader)
        self.assertIn('_Entry',shader)
        importer=Path('Assets/Editor/PackModelLibrary.cs').read_text()
        self.assertIn('assetPath.StartsWith("Assets/MosquePack/")?2048:1024',importer)
        self.assertIn('maxTextureSize = size',importer)

    def test_other_weapons_reload_does_not_override_original_clip(self):
        code=Path('Assets/Scripts/WeaponController.cs').read_text()
        self.assertNotIn('Mathf.Sin((reloadEnd-Time.time)',code)
        self.assertIn('if(equipped==null)PlayClip("reload",1.6f)',code)
        self.assertIn('WeaponPresentation.SupportTarget',code)
        self.assertIn('CompleteReload(Time.time)',code)
        self.assertIn('SecondaryWeaponAudit.Validate',Path('Assets/Editor/BuildPreview.cs').read_text())

if __name__=='__main__':unittest.main()
