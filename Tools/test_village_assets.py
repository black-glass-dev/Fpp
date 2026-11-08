import io
import json
from pathlib import Path
import unittest
import zipfile
from audit_village_archives import safe_entries, PACKS

class VillageAssets(unittest.TestCase):
    def test_all_supplied_archives_have_exact_metadata_sizes(self):
        self.assertEqual([73435128,14175387,94866448],[p[2] for p in PACKS])
        inventory=json.loads(Path('QA/village-source-inventory.json').read_text())
        self.assertEqual(18,sum(len(p['entries']) for p in inventory['packs']))

    def test_zip_slip_and_duplicate_members_are_rejected(self):
        for names in [['../unsafe.glb'],['/unsafe.glb'],['C:/unsafe.glb'],['same.glb','same.glb']]:
            memory=io.BytesIO()
            with zipfile.ZipFile(memory,'w') as archive:
                for name in names:archive.writestr(name,b'abc')
            memory.seek(0)
            with zipfile.ZipFile(memory) as archive:
                with self.assertRaises(ValueError):safe_entries(archive)

    def test_import_preserves_sources_and_has_a_mandatory_scene_gate(self):
        stage=Path('Tools/stage_village_assets.py').read_text()
        self.assertIn('shutil.copyfile(source, target)',stage)
        self.assertNotIn('prepare_models',stage)
        self.assertIn('len(models) != 18',stage)
        manifest=json.loads(Path('Packages/manifest.json').read_text())
        self.assertIn('5c36f4405012b13ad14ec871b77ba063c53cbbc3',manifest['dependencies']['com.atteneder.gltfast'])
        self.assertIn('SuppliedVillageModelsPondAndExpandedRoutesExist',Path('Tools/require_gameplay_qa.py').read_text())
        builder=Path('Assets/Editor/VillageExpansion.cs').read_text()
        for token in ['village-placement.json','MeshCollider','Village pond water','18','new[]{"North boundary"']:
            self.assertIn(token,builder)

if __name__=='__main__':unittest.main()
