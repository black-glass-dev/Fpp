"""Regressions for build-38 phone feedback: visible gas, material scale and light budget."""
import unittest
from pathlib import Path
from PIL import Image
class SurfaceFeedbackTests(unittest.TestCase):
    def test_high_resolution_coherent_ground_assets(self):
        for name in ('Grass','Dirt'):
            with Image.open(f'Assets/Art/Ground/{name}.png') as image:self.assertEqual(image.size,(2048,2048))
            with Image.open(f'Assets/Art/Ground/{name}Normal.png') as image:self.assertEqual(image.size,(1024,1024))
        code=Path('Assets/Shaders/WorldGround.shader').read_text()
        for term in ('IN.worldPos.xz','_SoilNormal','UnpackNormal','fullforwardshadows','_Road','ruts'):
            self.assertIn(term,code)
    def test_smoke_can_linger_but_has_a_bounded_particle_budget(self):
        code=Path('Assets/Scripts/ShotEffects.cs').read_text()
        for term in ('(.65f,1.05f)','(.065f,.105f)','main.maxParticles=96','ParticleSystemSimulationSpace.World','turbulence.enabled=true','c.tracerLength+age*c.tracerSpeed'):
            self.assertIn(term,code)
        self.assertNotIn('PrimitiveType.Sphere',code)
    def test_lighting_is_enabled_at_runtime_not_just_editor(self):
        boot=Path('Assets/Scripts/MobileBootstrap.cs').read_text()
        self.assertIn('ShadowQuality.HardOnly',boot);self.assertIn('shadowDistance=28',boot)
        scene=Path('Assets/Editor/CottageMap.cs').read_text()
        for term in ('AmbientMode.Trilight','Skybox/Procedural','LightShadows.Hard','shadowCascades=0'):
            self.assertIn(term,scene)
        hud=Path('Assets/Scripts/MobileHud.cs').read_text();self.assertIn('new Vector2(.5f,1)',hud);self.assertIn('RANGE  //',hud)
        self.assertNotIn('PlayerPrefs.DeleteKey',hud.split('private void Update()')[1].split('private void ApplySafeArea')[0])
if __name__=='__main__':unittest.main()
