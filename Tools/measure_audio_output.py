"""BS1770 analysis of actual Unity AudioRenderer PCM, never clip-energy estimates."""
import argparse
import json
import re
import shutil
import subprocess
from pathlib import Path


def measure(path,binary):
    result=subprocess.run([binary,'-hide_banner','-nostats','-i',str(path),'-af','loudnorm=I=-14:TP=-1:LRA=11:print_format=json','-f','null','-'],capture_output=True,text=True,check=True)
    blocks=re.findall(r'\{\s*"input_i".*?\}',result.stderr,re.S)
    if not blocks:raise RuntimeError('No loudnorm metrics: '+str(path))
    data=json.loads(blocks[-1])
    result=subprocess.run([binary,'-hide_banner','-nostats','-i',str(path),'-af','ebur128=peak=true','-f','null','-'],capture_output=True,text=True,check=True)
    short=[float(x) for x in re.findall(r'\bS:\s*(-?\d+(?:\.\d+)?)',result.stderr)]
    spectrum=path.with_name(path.stem+'-spectrum.png')
    subprocess.run([binary,'-hide_banner','-loglevel','error','-y','-i',str(path),'-lavfi','showspectrumpic=s=960x540:legend=1:scale=log:color=intensity','-frames:v','1',str(spectrum)],capture_output=True,check=True)
    return {'file':str(path),'integratedLUFS':float(data['input_i']),'truePeakDBTP':float(data['input_tp']),'maxShortTermLUFS':max(short) if short else None,'spectrum':str(spectrum)}


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--directory',default='QA/runtime/audio');parser.add_argument('--enforce',action='store_true');args=parser.parse_args()
    binary=shutil.which('ffmpeg')
    if binary is None:
        import imageio_ffmpeg
        binary=imageio_ffmpeg.get_ffmpeg_exe()
    root=Path(args.directory);records=[measure(root/(name+'.wav'),binary) for name in ('combat-mix','weapons-stem','foley-stem')]
    mix,weapons,_=records
    passed=abs(mix['integratedLUFS']+14)<=1 and weapons['maxShortTermLUFS'] is not None and abs(weapons['maxShortTermLUFS']+6)<=1 and all(r['truePeakDBTP']<=-1 for r in records)
    report={'evidence':'Actual Unity stereo float PCM, independently analyzed by FFmpeg loudnorm/ebur128. Not device speaker/acoustic proof.','loudnessGatePassed':passed,'toleranceLU':1,'records':records}
    (root.parent/'audio-output-metrics.json').write_text(json.dumps(report,indent=2,allow_nan=False)+'\n');print(json.dumps(report,indent=2,allow_nan=False))
    if args.enforce and not passed:raise SystemExit('Measured audio gate failed; do not claim loudness/limiter compliance')

if __name__=='__main__':main()
