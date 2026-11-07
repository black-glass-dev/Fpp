"""Data/integration guardrails, explicitly NOT substitutes for Play Mode or Android profiling."""
from pathlib import Path
import re,unittest
class GunplayContractTests(unittest.TestCase):
    def value(self,name,key):
        text=Path('Assets/Resources/Gunplay/'+name+'.asset').read_text()
        return float(re.search(r'^  '+key+r': ([.\d]+)',text,re.M).group(1))
    def test_reference_weapon_data_is_serialized(self):
        for name,rpm,mag,ads in [('mark23',480,12,.16),('glock17',540,17,.16),('field',450,17,.16),('ak47',600,30,.28)]:
            self.assertEqual(self.value(name,'rpm'),rpm);self.assertEqual(self.value(name,'magSize'),mag);self.assertEqual(self.value(name,'adsTimeSec'),ads)
            self.assertGreater(self.value(name,'reloadEmptySec'),self.value(name,'reloadTacticalSec'))
    def test_gameplay_does_not_poll_raw_input(self):
        for name in ['WeaponController','TestPlayer','Ballistics','GunplayModel']:
            text=Path('Assets/Scripts/'+name+'.cs').read_text()
            self.assertNotRegex(text,r'\bInput\.(Get|touch|gyro)')
        self.assertIn('IInputProvider',Path('Assets/Scripts/GameplayInput.cs').read_text())
    def test_required_move_states_and_no_player_rigidbody(self):
        text=Path('Assets/Scripts/TestPlayer.cs').read_text()
        for state in ['Idle','Walk','Sprint','TacticalSprint','Crouch','Prone','Slide','Dive','Airborne','Mantle','Mount','Dead']:self.assertIn(state,text)
        self.assertNotIn('AddComponent<Rigidbody>',text)
        self.assertEqual(self.value('Config','slideMultiplier'),1.35)
    def test_events_cover_prompt_two_and_foley_is_authored(self):
        bus=Path('Assets/Scripts/GunplayEvents.cs').read_text()
        for name in ['ShotFired','ShotTailRequest','ShellEject','ShellLand','ReloadFoley','Footstep','Hitmarker','Impact','Damage','Feed','ADSIn','ADSOut','Pickup','Ricochet']:self.assertIn(name,bus)
        setup=Path('Assets/Editor/GunplaySetup.cs').read_text()
        self.assertIn('AnimationUtility.SetAnimationEvents',setup);self.assertIn('Instantiate(source)',setup)
    def test_playmode_is_a_release_gate_and_evidence_is_not_versioned_art(self):
        workflow=Path('.github/workflows/android.yml').read_text()
        self.assertIn('testMode: playmode',workflow);self.assertIn('summarize_gameplay_qa.py',workflow)
        self.assertIn('QA/runtime/',Path('.gitignore').read_text())
        tests=Path('Assets/Tests/PlayMode/GunplayPlayModeTests.cs').read_text()
        self.assertIn('Time.realtimeSinceStartup-start<300',tests);self.assertIn('Assert.AreEqual(500,shots)',tests)
        self.assertIn('ActualADS30RoundWallPatternAt15m',Path('Assets/Tests/PlayMode/GunplayVisualTests.cs').read_text())
if __name__=='__main__':unittest.main()
