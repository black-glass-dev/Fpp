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
for path in Path('QA/runtime').glob('*'):
    if path.suffix in ('.csv','.txt') and path.stat().st_size<50000:out['measurements'][path.name]=path.read_text()
out['playModeResultsPresent']=bool(out['suites'])
Path('QA/native-results-'+out['run']+'.json').write_text(json.dumps(out,indent=2))
print('Play Mode XML suites:',len(out['suites']))
