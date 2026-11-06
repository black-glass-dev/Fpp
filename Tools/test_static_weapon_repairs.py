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

    def test_other_weapons_reload_does_not_override_original_clip(self):
        code=Path('Assets/Scripts/WeaponController.cs').read_text()
        self.assertNotIn('Mathf.Sin((reloadEnd-Time.time)',code)
        self.assertIn('if(equipped==null)PlayClip("reload",1.6f)',code)
        self.assertIn('WeaponPresentation.SupportTarget',code)
        self.assertIn('CompleteReload(Time.time)',code)
        self.assertIn('SecondaryWeaponAudit.Validate',Path('Assets/Editor/BuildPreview.cs').read_text())

if __name__=='__main__':unittest.main()
