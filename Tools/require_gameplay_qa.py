"""An action's exit code or a Passed/0-tests XML is never acceptance."""
import xml.etree.ElementTree as E
from pathlib import Path
required={'CollectedWeaponsCycleAwayFromPickupsWithoutLosingAmmo','MovementDoesNotRunAgainstWallsOrAutoMantleWithoutJump','TouchIconsPressedStatesAndLayoutDragPreserveSafeBounds','PresentationAudioLayersAreAudibleAndPoolIsBounded','ConfigsAreSerializedAndReferenceValuesMatch','CadenceHasNoBankedShotsAndStaysFrameRateIndependent','RecoilNoiseBoundHipAsymmetryAndMountReduction','SameFrameSemiFireAndActualADSTimingAllWeapons','ReloadCancellationConservationAndDroppedAmmo','MovementSlideDiveAndMantleOnRealController','FiveMinuteCombatSoak500Shots50Reloads','DamageMarkersAndHealthAreSynchronous','DefaultPistolDropsAsTexturedGunOnlyAndRestoresAuthoredSlot','SprintOutDelayAndADSCancellationUseActualInputLoop','PhysicalMountLocksMovementAndReducesActualShotImpulseRMS','ActualADS30RoundWallPatternAt15m','CapturePoseAndReloadEvidence_NotFingerContactAcceptance'}
found={}
for path in Path('QA/test-results').rglob('*results.xml'):
    root=E.parse(path).getroot()
    for test in root.iter('test-case'):
        name=test.get('name');result=test.get('result')
        if name in required:found[name]=result
missing=required-found.keys();failed={k:v for k,v in found.items() if v!='Passed'}
if missing or failed:raise SystemExit('Gameplay gate failed. Missing='+str(sorted(missing))+'; not passed='+str(failed))
print('All',len(required),'named Unity Play Mode checks executed and passed. This gate does not certify finger contact or Android performance.')
