from pathlib import Path
import unittest

class InventoryMovementRepairs(unittest.TestCase):
    def test_swap_cycles_owned_slots_not_nearby_world(self):
        code=Path('Assets/Scripts/WeaponController.cs').read_text()
        swap=code.split('public void UseSidearm()',1)[1].split('public void DropActiveWeapon()',1)[0]
        self.assertIn('inventory[(current+1)%inventoryCount]',swap)
        self.assertNotIn('nearest',swap)
        apply=code.split('private void ApplyWeapon(',1)[1].split('private void RestoreSupportPose',1)[0]
        self.assertNotIn('.Drop(',apply)
        self.assertIn('equipped.Store(rounds,reserve)',apply)
    def test_movement_uses_real_displacement_and_explicit_mantle(self):
        code=Path('Assets/Scripts/TestPlayer.cs').read_text()
        self.assertIn('transform.position-beforeMove',code)
        self.assertIn('input.Jump && input.Move.y>0',code)
        self.assertIn('Physics.OverlapCapsuleNonAlloc',code)
        self.assertIn('controller.radius*2',code)
    def test_real_player_loop_inventory_case_is_release_required(self):
        self.assertIn('CollectedWeaponsCycleAwayFromPickupsWithoutLosingAmmo',Path('Tools/require_gameplay_qa.py').read_text())
        self.assertIn('DROP WEAPON',Path('Assets/Scripts/MobileHud.cs').read_text())

if __name__=='__main__':unittest.main()
