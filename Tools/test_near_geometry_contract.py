"""Local structural/math regression checks; these do NOT replace Unity/device tests."""
from pathlib import Path
import math
import unittest

class GeometryContract(unittest.TestCase):
    def test_segment_minimum_stays_between_endpoints(self):
        # Exact quadratic fitted from f(0), f(.5), f(1), matching the opt-in C# solver.
        for minimum in (-10, -.1, 0, .125, .5, .9, 1, 1.1, 10):
            f=lambda x:(x-minimum)**2
            e0,e1,em=f(0),f(1),f(.5)
            a=2*(e0+e1-2*em);b=e1-e0-a
            t=max(0,min(1,-b/(2*a))) if a>1e-20 else (0 if e0<=e1 else 1)
            self.assertAlmostEqual(t,max(0,min(1,minimum)))
            self.assertTrue(math.isfinite(t))
            self.assertLessEqual(f(t),min(e0,e1)+1e-10)

    def test_world_ao_is_not_reused_on_fpv(self):
        setup=Path('Assets/Editor/UrpPresentationSetup.cs').read_text()
        runtime=Path('Assets/Scripts/PresentationRenderer.cs').read_text()
        self.assertIn('Renderer("FPVRenderer",false,false)',setup)
        self.assertIn('list.arraySize=4',setup)
        self.assertIn('data.SetRenderer(2)',runtime)
        self.assertIn('owner.weaponCamera.GetUniversalAdditionalCameraData().SetRenderer(2)',runtime)
        self.assertIn('!Application.isMobilePlatform&&SystemInfo.supportsComputeShaders',runtime)

    def test_network_is_budgeted_actual_scan_not_texture_substitute(self):
        s=Path('Assets/Editor/CleanRoadNetwork.cs').read_text()
        self.assertIn('GameObject.Find("Road(ready to use)")',s)
        self.assertIn('RoadSurfaceBaker.Build(filter,out var surfaceMaterial)',s)
        bake=Path('Assets/Editor/RoadSurfaceBaker.cs').read_text()
        self.assertIn('coverage<.98f',bake)
        self.assertIn('source.subMeshCount!=1',bake)
        self.assertIn('input.GetTexture("baseColorTexture")',bake)
        self.assertIn('if(!inside)continue',s)
        self.assertIn('triangles>600000',s)
        self.assertNotIn('AddComponent<MeshCollider>',s)
        self.assertNotIn('Texture2D(',s)

    def test_geometry_budgets_preserve_source_and_collision_identity(self):
        s=Path('Assets/Editor/WorldDetailLod.cs').read_text()
        self.assertIn('identity.originalMesh=original',s)
        self.assertIn('options.ConstrainToEdgeSegment=true',s)
        self.assertIn('nearTotal>900000',s)
        self.assertIn('nearTotal>=sourceTotal*.4',s)
        b=Path('Assets/Editor/WorldMaterialBatching.cs').read_text()
        self.assertIn('root.worldToLocalMatrix*filter.transform.localToWorldMatrix',b)
        self.assertIn('mat.renderQueue>=3000',b)
        self.assertNotIn('DestroyImmediate(r)',b)

if __name__=='__main__':unittest.main()
