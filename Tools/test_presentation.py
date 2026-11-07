from pathlib import Path
import unittest

class PresentationGuards(unittest.TestCase):
    def test_atlas_import_is_repaired_and_editor_requires_glyphs(self):
        repair=Path('Assets/Editor/TextureRepair.cs').read_text()
        self.assertIn('"Assets/Resources/Presentation"',repair)
        self.assertIn('importer.npotScale=TextureImporterNPOTScale.None',repair)
        self.assertIn('importer.mipmapEnabled=false',repair)
        self.assertIn('atlas glyph missing',Path('Assets/Editor/HudAudit.cs').read_text())
    def test_native_gate_includes_audio_touch_and_gameplay(self):
        gate=Path('Tools/require_gameplay_qa.py').read_text()
        for name in ['PresentationAudioLayersAreAudibleAndPoolIsBounded','TouchIconsPressedStatesAndLayoutDragPreserveSafeBounds','ActualADS30RoundWallPatternAt15m','FiveMinuteCombatSoak500Shots50Reloads']:
            self.assertIn(name,gate)
    def test_audio_does_not_use_unbounded_one_shots(self):
        audio=Path('Assets/Scripts/PresentationAudio.cs').read_text()
        self.assertNotIn('PlayOneShot(',audio)
        self.assertIn('Mathf.Clamp(settings.maxVoices,8,32)',audio)
        self.assertIn('sources.Length-1',audio)
        self.assertIn('SYNTH_PLACEHOLDER',audio)

if __name__=='__main__': unittest.main()
