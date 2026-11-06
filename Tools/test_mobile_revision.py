from pathlib import Path
import json,unittest
from PIL import Image

class MobileRevisionTests(unittest.TestCase):
    def test_original_skin_and_clips_preserved(self):
        rig=json.loads(Path('Assets/Viewmodels/Mark23/rig.json').read_text())
        self.assertGreater(len(rig['bones']),40)
        for word in ('draw','shoot','reload'):
            self.assertTrue(any(word in name.lower() for name in rig['actions']))
        self.assertTrue(Path('Assets/Viewmodels/Mark23/Mark23.fbx').read_bytes().startswith(b'Kaydara FBX Binary'))
        self.assertEqual({m['name'] for m in rig['materials']},{'Hand_D','Glove_D','Mark23_D'})
        for material in rig['materials']:
            with Image.open(material['texture']) as image:self.assertGreater(image.width,256)
    def test_no_projectile_ball_or_imgui_controls(self):
        controller=Path('Assets/Scripts/WeaponController.cs').read_text()
        self.assertNotIn('PrimitiveType.Sphere',controller)
        self.assertNotIn('OnGUI',controller)
        self.assertIn('Physics.Raycast',controller)
        self.assertIn('PlayClip("shoot"',controller)
        self.assertIn('PlayClip("reload"',controller)
    def test_mobile_layout_persistence(self):
        hud=Path('Assets/Scripts/MobileHud.cs').read_text()
        for term in ('PlayerPrefs.SetString','SaveLayout','ResetLayout','Screen.safeArea','ScaleWithScreenSize'):
            self.assertIn(term,hud)
        for control in ('move','fire','reload','pickup','aim','sidearm'):
            self.assertIn('"'+control+'"',hud)
    def test_ground_and_icon(self):
        for name in ('Grass','Dirt','Stone'):
            with Image.open('Assets/Art/Ground/'+name+'.png') as image:
                self.assertEqual(image.size,(512,512));self.assertIsNotNone(image.getbbox())
        with Image.open('Assets/Art/Icon/FppIcon.png') as image:self.assertEqual(image.size,(512,512))
if __name__=='__main__':unittest.main()
