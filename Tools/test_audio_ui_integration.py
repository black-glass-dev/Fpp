"""Source-only integration guards. Native PCM/pixel/menu cases are separate evidence."""
import json
import unittest
from pathlib import Path

class AudioUiIntegration(unittest.TestCase):
    def test_real_mixer_authoring_and_routing_are_connected(self):
        setup=Path('Assets/Editor/PresentationAudioSetup.cs').read_text()
        for token in ['CreateMixerControllerAtPath','AddChildToParent','exposedParameters','AudioCompressionFormat.Vorbis','importSettings.preloadAudioData=true']:
            self.assertIn(token,setup)
        audio=Path('Assets/Scripts/PresentationAudio.cs').read_text()
        for token in ['Instantiate(authored)','source.outputAudioMixerGroup=groups[bus]','mixer.SetFloat','BusVolume','MusicStub']:
            self.assertIn(token,audio)
        self.assertNotIn('AudioClip.Create',audio)
        self.assertIn('PresentationAudioSetup.Apply()',Path('Assets/Editor/CottageMap.cs').read_text())

    def test_native_pcm_and_menu_are_mandatory_not_clip_energy_substitutes(self):
        gate=Path('Tools/require_gameplay_qa.py').read_text()
        for name in ['AudioMixerRoutesVorbisClipsAndExportsActualStereoPCM','GraphicsAudioMenuChangesRealSettingsAndBlocksGameplay']:
            self.assertIn(name,gate)
        capture=Path('Assets/Tests/PlayMode/PresentationAudioOutputTests.cs').read_text()
        for token in ['AudioRenderer.Start()','AudioRenderer.Render(buffer)','AudioSettings.Reset(previous)','buffer.Dispose()']:
            self.assertIn(token,capture)
        self.assertIn('Unity.Collections',json.loads(Path('Assets/Tests/PlayMode/Fpp.Gameplay.Tests.asmdef').read_text())['references'])

    def test_drs_toggle_does_not_mutate_authored_gameplay_asset(self):
        hud=Path('Assets/Scripts/MobileHud.cs').read_text()
        self.assertIn('visualPanel.activeSelf',hud)
        self.assertIn('ToggleDynamicResolution()',hud)
        self.assertNotIn('dynamicResolution=!GunplayConfig.Current',hud)
        renderer=Path('Assets/Scripts/PresentationRenderer.cs').read_text()
        self.assertIn('pipeline.renderScale=visual.maxScale',renderer)
        self.assertIn('Application.isMobilePlatform&&DynamicResolution',renderer)

    def test_fixed_floating_toggle_is_persisted_and_does_not_capture_background_in_fixed_mode(self):
        hud=Path('Assets/Scripts/MobileHud.cs').read_text()
        self.assertIn('fpp.controls.floating',hud)
        self.assertIn('joystick.ResetInput()',hud)
        self.assertIn('FLOATING STICK',hud)
        zone=Path('Assets/Scripts/FloatingJoystickZone.cs').read_text()
        self.assertIn('!control.owner.Editing && control.owner.FloatingJoystick',zone)
        control=Path('Assets/Scripts/HudControl.cs').read_text()
        self.assertIn('if(owner.FloatingJoystick)rect.localPosition=floatingOrigin',control)
        self.assertIn('data.pointerId!=finger',control)

    def test_lossless_viewmodel_and_effect_toggle_contract(self):
        source=Path('Assets/Scripts/PresentationRenderer.cs').read_text()
        for token in ['overlay.useOcclusionCulling=false','MotionVectorGenerationMode.ForceNoMotion','skin.skinnedMotionVectors=false','skin.forceMatrixRecalculationPerRender=true','fpp.visual.native-default-v1']:
            self.assertIn(token,source)
        self.assertEqual(1,source.count('GetComponent<WeaponController>()'))
        for name in ['ToggleGrain','ToggleBlur']:
            method=source.split('public void '+name+'()')[1].split('}')[0]
            self.assertNotIn('SetTier(',method)
            self.assertIn('ApplyEffectToggles()',method)
        self.assertIn('dynamicResolution: 0',Path('Assets/Resources/Gunplay/Config.asset').read_text())

    def test_hud_capture_has_real_pixel_gate_and_ungraded_final_camera(self):
        helper=Path('Assets/Scripts/RenderEvidence.cs').read_text()
        self.assertIn('data.renderPostProcessing=false',helper)
        self.assertIn('UniversalRenderPipeline.SingleCameraRequest',helper)
        self.assertIn('canvas.worldCamera=previousCamera',helper)
        self.assertLess(helper.index('camera.targetTexture=ui'),helper.index('Canvas.ForceUpdateCanvases()'))
        self.assertIn('RenderTexture.ReleaseTemporary(ui)',helper)
        self.assertNotIn('stack.Add(',helper)
        native=Path('Assets/Tests/PlayMode/GunplayPlayModeTests.cs').read_text()
        self.assertIn('yield return RapidTurnElevationEvidence(weapons)',native)
        self.assertIn('CountVisibleArmSkins(camera)',native)
        self.assertIn('AssertUngradedHud(weapons)',Path('Assets/Tests/PlayMode/GunplayPlayModeTests.cs').read_text())
        self.assertIn('image.GetPixel(160,90)',Path('Assets/Tests/PlayMode/GunplayVisualTests.cs').read_text())

if __name__=='__main__':unittest.main()
