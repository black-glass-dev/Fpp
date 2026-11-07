"""Author serialized ScriptableObjects from declared tunable defaults; no Unity required.
Existing assets are not overwritten unless --replace is explicitly passed.
"""
import re,uuid,sys
from pathlib import Path

def guid(path):return uuid.uuid5(uuid.NAMESPACE_URL,'fpp:'+path).hex

def defaults(source):
    values={}
    for kind,fields in re.findall(r'public (float|int|bool) ([^;\n]+);',source):
        for field in fields.split(','):
            if '=' not in field:continue
            name,value=field.strip().split('=',1)
            value=value.strip().rstrip('f')
            if name.isidentifier() and re.fullmatch(r'-?[\d.]+|true|false',value):values[name]='1' if value=='true' else '0' if value=='false' else value
    return values

def save(name,script,values):
    path=Path('Assets/Resources/Gunplay')/(name+'.asset');path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists() and '--replace' not in sys.argv:return
    header='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid('Assets/Scripts/'+script+'.cs')+', type: 3}\n  m_Name: '+name+'\n  m_EditorClassIdentifier: \n'
    path.write_text(header+''.join('  '+key+': '+str(value)+'\n' for key,value in values.items()))

save('Config','GunplayConfig',defaults(Path('Assets/Scripts/GunplayConfig.cs').read_text()))
base=defaults(Path('Assets/Scripts/WeaponAsset.cs').read_text())
base.update(weaponClass=0,fireMode=0,penetrationClass=1,hipSpreadDeg='{x: 1, y: 8}',shotEvent='weapon.shot',tailEvent='weapon.tail',reloadEvent='weapon.reload',aimPoseOffset='{x: 0, y: 0, z: 0}',aimPoseEuler='{x: 0, y: 0, z: 0}',supportHand='{x: 0, y: 0, z: 0}',triggerFinger='{x: 0, y: 0, z: 0}',magGrab='{x: 0, y: 0, z: 0}',ejectOffset='{x: .04, y: .01, z: 0}',casingPrefab='{fileID: 0}')
patterns={
'mark23':[(.7,-.1),(.8,.1),(.6,.15),(.75,-.12)],
'glock17':[(.5,-.06),(.55,.1),(.6,-.1),(.5,.12)],
'field':[(.6,.1),(.65,-.1),(.7,.14),(.55,-.1)],
'ak47':[(.75,-.08),(.8,.05),(.85,-.12),(.9,.12),(.85,.15),(.8,.2),(.55,.22),(.5,.25),(.45,.2),(.4,.24),(.35,.18),(.4,.22),(.45,.16),(.4,.2),(.4,.24)]}
for name,display,rpm,mag in [('mark23','Mark 23',480,12),('glock17','Glock 17',540,17),('field','Field Pistol',450,17),('ak47','AK-47',600,30)]:
    values=dict(base,id=name,displayName=display,rpm=rpm,magSize=mag)
    values['recoilPattern']='\n'+''.join('  - {x: '+str(v)+', y: '+str(h)+'}\n' for v,h in patterns[name]).rstrip('\n')
    if name=='ak47':values.update(weaponClass=2,fireMode=1,reserveMax=300,adsTimeSec=.28,sightFOV=45,sprintOutFireDelaySec=.18,aimWalkSpeed=2.4,reloadTacticalSec=2.2,reloadEmptySec=2.7,dmg0=40,range0=25,dmg1=33,range1=45,dmg2=28,penetrationClass=2)
    elif name=='glock17':values.update(reloadTacticalSec=1.7,reloadEmptySec=2.1,gunKickMult=.8)
    elif name=='field':values.update(reloadTacticalSec=1.8,reloadEmptySec=2.2)
    save(name,'WeaponAsset',values)
