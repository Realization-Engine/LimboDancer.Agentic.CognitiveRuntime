import unittest
from pathlib import Path
from check_ui_encoding import check_text,check_files
class EncodingTests(unittest.TestCase):
 def test_assets(self):check_files(Path(__file__).resolve().parent)
 def test_valid_unicode(self):check_text('Assign \u2192 deliver; Whole Map \u203a Normandy; B\u00e1rbara; \u00b7','valid')
 def test_corruption(self):
  for original in ['\u2192','\u203a','\u00b7','\u00e1']:
   broken=original
   for _ in range(2):
    broken=broken.encode('utf-8').decode('cp1252')
    with self.assertRaises(ValueError):check_text(broken,'broken')
if __name__=='__main__':unittest.main()
