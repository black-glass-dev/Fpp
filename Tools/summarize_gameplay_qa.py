"""Publish test XML and own small evidence only. Never read activation/editor raw logs."""
import json,os,re
from pathlib import Path
import xml.etree.ElementTree as ET
out={'run':os.environ['RUN_ID'],'evidenceKind':'actual Unity Play Mode XML; device budgets and visual acceptance are separate','suites':[],'measurements':{}}
for path in Path('QA/test-results').rglob('*results.xml'):
    root=ET.parse(path).getroot()
    suite={'file':str(path),'result':root.get('result'),'total':root.get('total'),'passed':root.get('passed'),'failed':root.get('failed'),'cases':[]}
    for case in root.iter('test-case'):
        info={k:case.get(k) for k in ('name','result','duration')}
        msg=case.find('failure/message')
        if msg is not None:
            safe=[]
            for line in (msg.text or '').splitlines():
                if re.search(r'password|email|serial|token|credential|https?://',line,re.I):continue
                safe.append(re.sub(r'[^\s@]+@[^\s@]+','[EMAIL]',line))
            info['failure']='\n'.join(safe)[:3000]
        suite['cases'].append(info)
    out['suites'].append(suite)
for path in Path('QA/runtime').rglob('*'):
    if path.suffix in ('.csv','.txt','.json') and path.stat().st_size<50000:out['measurements'][str(path.relative_to('QA/runtime'))]=path.read_text()
# Keep only a bounded selection of genuine camera images in Git; full sequences stay CI artifacts.
import shutil
preview=Path('QA/Previews');preview.mkdir(exist_ok=True)
budget=1500000
out['reviewImages']=[]
for name in ('recoil-wall-ak47.png','ak47-ads.png','ak47-empty-0.40.png','ak47-sprint.png'):
    path=Path('QA/runtime')/name
    if path.exists() and path.stat().st_size<=budget:
        shutil.copyfile(path,preview/name);budget-=path.stat().st_size;out['reviewImages'].append(str(preview/name))
out['playModeResultsPresent']=bool(out['suites'])
Path('QA/native-results-'+out['run']+'.json').write_text(json.dumps(out,indent=2))
print('Play Mode XML suites:',len(out['suites']))
