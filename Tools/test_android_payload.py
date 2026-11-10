import unittest
from pathlib import Path
from verify_android_payload import verify_payload

class AndroidPayloadGate(unittest.TestCase):
    def setUp(self):self.audit={'textures':10,'textureRgbaEquivalentBytesBefore':10000000,'textureBytesAfter':2000000}
    def test_signed_oversized_binary_is_rejected(self):
        for size in (0,500000000,631625961):
            with self.assertRaises(ValueError):verify_payload(self.audit,size,True)
    def test_small_binary_without_payload_audit_is_not_accepted(self):
        with self.assertRaises(ValueError):verify_payload({},200000000,True)
        with self.assertRaises(ValueError):verify_payload(self.audit,200000000,False)
    def test_audited_smaller_payload(self):self.assertTrue(verify_payload(self.audit,300000000,True))
    def test_no_startup_mesh_mutation_in_prepared_scene(self):
        s=Path('Assets/Editor/AndroidWorldPayload.cs').read_text()
        self.assertIn('DestroyImmediate(residency)',s)
        self.assertIn('DestroyImmediate(marker)',s)
        self.assertIn('UnpackPrefabInstance',s)
        self.assertIn('source.width,source.height',s)
        self.assertIn('result.mipmapCount!=source.mipmapCount',s)
        self.assertIn('TextureCompressionQuality.Best',s)
    def test_lite_boot_precedes_world_and_failed_api_is_not_used(self):
        s=Path('Assets/Editor/AndroidBuild.cs').read_text()
        self.assertIn('scenes = new[] { StartupSceneBuilder.Path,',s)
        self.assertIn('new[] { GraphicsDeviceType.OpenGLES3 }',s)
        boot=Path('Assets/Scripts/StartupGate.cs').read_text()
        self.assertIn('LoadSceneAsync',boot)
        self.assertIn('SupportsTextureFormat',boot)
        self.assertNotIn('LoadSceneAsync',boot[boot.index('private void Awake()'):boot.index('private void Record')])
