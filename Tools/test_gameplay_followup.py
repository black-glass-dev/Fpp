"""Regressions from the ten build-27 phone screenshots; engine checks live in EnvironmentAudit."""
import json
import re
import unittest
from pathlib import Path
import numpy as np
import trimesh

class PhoneFollowupTests(unittest.TestCase):
    def test_reticle_uses_full_camera_viewport_not_notch_safe_area(self):
        code=Path('Assets/Scripts/MobileHud.cs').read_text()
        self.assertIn('Panel("Reticle",go.transform,Color.clear)',code)
        self.assertNotIn('Panel("Reticle",safeRoot',code)
        self.assertIn('safeRoot.anchorMin',code) # action controls still respect notches

    def test_real_magazine_and_action_transforms_are_animated(self):
        code=Path('Assets/Scripts/WeaponRigParts.cs').read_text()
        self.assertIn('space.rotation*(new Vector3',code)
        self.assertIn('magazine.position+=delta',code)
        self.assertIn('action.position-=space.forward',code)
        controller=Path('Assets/Scripts/WeaponController.cs').read_text()
        self.assertIn('heldRig.RetargetMagazine',controller)
        self.assertIn('heldVisual.transform.SetParent(gunBone,true)',controller)
        self.assertIn('else PlayClip("reload",reloadDuration)',controller)
        self.assertNotIn('rigAnimation.enabled=false',controller)
        self.assertNotIn('leftUpper.rotation=',controller)
        self.assertIn('angle>35f?35f/angle:1f',controller)
        self.assertIn('if(equipped==null)PlayClip("reload",1.6f)',controller)

    def test_pistol_partition_is_nonempty_and_preserves_every_original_face(self):
        models=json.loads(Path('Assets/PackModels/catalog.json').read_text())['models']
        for model in models:
            if model['name'] not in ('Glock 17','Gun pistol m'):continue
            for part in model['parts']:
                mesh=trimesh.load(part['model'],force='mesh',process=False)
                y=mesh.vertices[mesh.faces].mean(axis=1)[:,1]
                low,high=mesh.bounds[:,1];mag=y<low+(high-low)*.20;slide=y>low+(high-low)*.69;frame=~(mag|slide)
                self.assertEqual(int(mag.sum()+slide.sum()+frame.sum()),len(mesh.faces))
                self.assertFalse(np.any(mag&slide))
                for group in (mag,slide,frame):self.assertGreater(int(group.sum()),100)
                self.assertTrue(np.isfinite(mesh.visual.uv).all())

    def test_smoke_module_and_build_referenced_materials(self):
        manifest=json.loads(Path('Packages/manifest.json').read_text())
        self.assertEqual(manifest['dependencies']['com.unity.modules.particlesystem'],'1.0.0')
        setup=Path('Assets/Editor/ViewmodelSetup.cs').read_text()
        self.assertIn('controller.smokeMaterial=EffectMaterial',setup)
        self.assertIn('controller.tracerMaterial=EffectMaterial',setup)

    def test_flash_is_visible_immediately_and_long_enough_at_thirty_fps(self):
        code=Path('Assets/Scripts/ShotEffects.cs').read_text()
        duration=float(re.search(r'flashUntil=Time.time\+([.\d]+)f',code).group(1))
        self.assertGreater(duration,2/30)
        self.assertIn('flash.SetActive(true)',code)
        self.assertIn('smoke.Emit(5)',code)
        self.assertIn('if(!smoke.isPlaying)smoke.Play()',code)
        self.assertIn('Mathf.Min(.8f,Vector3.Distance(world.transform.position,hit)*.6f)',code)
        self.assertIn('[DefaultExecutionOrder(100)]',code)
        self.assertIn('private void LateUpdate()',code)
        self.assertIn('overlay.WorldToViewportPoint(muzzle.position)',code)
        self.assertIn('world.ViewportPointToRay(viewport)',code)

    def test_sights_and_default_muzzle_use_actual_mesh_landmarks(self):
        anchors=Path('Assets/Editor/ViewmodelAnchors.cs').read_text()
        self.assertIn('bones[i].localToWorldMatrix*binds[i]',anchors)
        self.assertIn('muzzle.SetParent(gunBone,true)',anchors)
        controller=Path('Assets/Scripts/WeaponController.cs').read_text()
        self.assertIn('weaponMount.InverseTransformPoint(heldRig.sight.position)',controller)
        builder=Path('Assets/Editor/SecondaryWeaponRigBuilder.cs').read_text()
        self.assertIn('WeaponController.CentralFrontSight(points)',builder)
        self.assertIn('-basePosition.x-(equipped!=null?secondaryAimSide:defaultSightRest.x)',controller)
        repair=Path('Assets/Editor/MosqueSurfaceRepair.cs').read_text()
        self.assertIn('innerX=4.2f,innerZ=3.8f',repair)
        self.assertIn('c.z+3.7f,c.z+4.6f',repair)
        self.assertNotIn('c.z+6.1f',repair)

    def test_tree_collision_excludes_canopy(self):
        code=Path('Assets/Editor/PackModelLibrary.cs').read_text()
        self.assertIn('model.AddComponent<CapsuleCollider>()',code)
        self.assertIn('trunk.radius=.24f',code)
        self.assertIn('Object.DestroyImmediate(collider);var trunk',code)

    def test_ground_has_geometry_normals_and_matching_collision(self):
        code=Path('Assets/Editor/GroundBuilder.cs').read_text()
        self.assertIn('Mathf.PerlinNoise',code)
        self.assertIn('go.GetComponent<MeshCollider>().sharedMesh=mesh',code)
        self.assertIn('mesh.RecalculateTangents()',code)
        scene=Path('Assets/Editor/CottageMap.cs').read_text()
        self.assertIn('GroundBuilder.Create(grass)',scene)
        self.assertNotIn('Cube("Ground"',scene)
        self.assertIn('GrassNormal.png',scene)

    def test_clean_room_and_solid_walls_are_native_audited(self):
        shader=Path('Assets/Shaders/MobileScan.shader').read_text()
        self.assertIn('_InteriorRect',shader)
        self.assertNotIn('abs(normalize(i.normal).y)>.6)clip(-1)',shader)
        code=Path('Assets/Editor/EnvironmentAudit.cs').read_text()
        self.assertIn('Physics.CheckSphere',code)
        self.assertIn('Physics.SphereCast',code)
        self.assertIn('EnvironmentAudit.Validate()',Path('Assets/Editor/AndroidBuild.cs').read_text())

if __name__=='__main__':unittest.main()
