"""Source-only integration guards. Native PCM/pixel/menu cases are separate evidence."""
import json
import unittest
from pathlib import Path

class AudioUiIntegration(unittest.TestCase):
    def test_real_mixer_authoring_and_routing_are_connected(self):
        setup=Path('Assets/Editor/PresentationAudioSetup.cs').read_text()
        for token in ['CreateMixerControllerAtPath','AddChildToParent','exposedParameters','AudioCompressionFormat.Vorbis','sample.preloadAudioData=true']:
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

    def test_hud_capture_has_real_pixel_gate_and_ungraded_final_camera(self):
        helper=Path('Assets/Scripts/RenderEvidence.cs').read_text()
        self.assertIn('data.renderPostProcessing=false',helper)
        self.assertIn('stack.Remove(camera)',helper)
        self.assertIn('AssertUngradedHud(weapons)',Path('Assets/Tests/PlayMode/GunplayPlayModeTests.cs').read_text())
        self.assertIn('image.GetPixel(160,90)',Path('Assets/Tests/PlayMode/GunplayVisualTests.cs').read_text())

if __name__=='__main__':unittest.main()
