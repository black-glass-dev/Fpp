import json
import os
from pathlib import Path
import tempfile
import unittest
import numpy as np
from PIL import Image
import trimesh
from prepare_models import convert

class ConversionTests(unittest.TestCase):
    def test_textured_static_scene_and_transform(self):
        with tempfile.TemporaryDirectory() as temp:
            old = os.getcwd()
            try:
                os.chdir(temp)
                mesh = trimesh.creation.box()
                material = trimesh.visual.material.PBRMaterial(
                    baseColorTexture=Image.new('RGBA', (4,4), (20,80,40,255)),
                    baseColorFactor=[1.,1.,1.,1.])
                mesh.visual = trimesh.visual.TextureVisuals(
                    uv=np.zeros((len(mesh.vertices),2)),material=material)
                scene = trimesh.Scene()
                transform = np.eye(4); transform[:3,3] = [3,2,1]
                scene.add_geometry(mesh,transform=transform)
                source=Path('AK-47.glb');source.write_bytes(scene.export(file_type='glb'))
                model=convert(source,Path('Assets/TestModels'))
                self.assertTrue(model['weapon'])
                self.assertEqual(len(model['parts']),1)
                part=model['parts'][0]
                self.assertTrue(Path(part['texture']).exists())
                obj=Path(part['model']).read_text()
                self.assertNotIn('mtllib',obj)
                self.assertNotIn('usemtl',obj)
                restored=trimesh.load(part['model'],force='mesh')
                self.assertEqual(len(restored.faces),12)
                np.testing.assert_allclose(restored.bounds,mesh.bounds+[3,2,1],atol=1e-5)
            finally:
                os.chdir(old)

    def test_every_model_has_source_hash_and_finite_color(self):
        catalog=json.loads(Path('Assets/PackModels/catalog.json').read_text())
        self.assertEqual(len(catalog['models']),6)
        self.assertFalse(any(m['name'] in ('kriss_vector_animated_free','mark_23__animated_free') for m in catalog['models']))
        self.assertTrue(Path('Assets/Viewmodels/Mark23/Mark23.fbx').is_file())
        self.assertEqual(sum(m['weapon'] for m in catalog['models']),3)
        for model in catalog['models']:
            self.assertEqual(len(model['sourceSha256']),64)
            for part in model['parts']:
                self.assertTrue(np.all(np.isfinite(part['color'])))
                self.assertTrue(all(0<=v<=1 for v in part['color']))

if __name__ == '__main__': unittest.main()
