from PIL import Image,ImageDraw
from pathlib import Path
im=Image.new('RGBA',(256,192));ink=(232,230,225,255)
for idx in range(11):
 t=Image.new('RGBA',(64,64));d=ImageDraw.Draw(t)
 if idx==0:
  d.rounded_rectangle((25,23,39,51),radius=2,outline=ink,width=3);d.polygon([(25,23),(32,10),(39,23)],fill=ink)
 elif idx==1:
  d.ellipse((15,15,49,49),outline=ink,width=3)
  for p in [(32,7,32,23),(32,41,32,57),(7,32,23,32),(41,32,57,32)]:d.line(p,fill=ink,width=3)
 elif idx==2:
  d.polygon([(23,15),(41,15),(37,49),(19,49)],outline=ink,width=3);d.line((45,25,53,25,53,44,47,44),fill=ink,width=3);d.polygon([(43,44),(50,38),(50,50)],fill=ink)
 elif idx==3:
  d.rounded_rectangle((20,25,44,50),radius=8,outline=ink,width=3)
  for x,h in [(22,13),(29,8),(36,10),(43,18)]:d.line((x,32,x,h),fill=ink,width=4)
  d.line((20,37,12,29),fill=ink,width=5)
 elif idx==4:
  d.line((12,23,51,23),fill=ink,width=3);d.polygon([(51,23),(41,15),(41,31)],fill=ink);d.line((51,42,12,42),fill=ink,width=3);d.polygon([(12,42),(22,34),(22,50)],fill=ink)
 elif idx==5:
  d.line((15,38,32,20,49,38),fill=ink,width=5);d.line((32,22,32,53),fill=ink,width=4)
 elif idx==6:
  d.polygon([(22,13),(39,13),(38,34),(52,43),(52,50),(17,50),(17,42),(24,34)],outline=ink,width=4)
 elif idx==7:
  d.ellipse((25,10,37,22),fill=ink);d.line((31,24,22,36,40,38,43,51),fill=ink,width=5);d.line((22,36,12,49),fill=ink,width=4)
 elif idx==8:
  d.ellipse((10,27,21,38),fill=ink);d.line((23,33,44,35,53,43),fill=ink,width=5);d.line((29,35,20,48),fill=ink,width=4)
 elif idx==9:
  for a in [(32,8,32,56),(8,32,56,32)]:d.line(a,fill=ink,width=3)
  for p in [[(32,8),(26,17),(38,17)],[(32,56),(26,47),(38,47)],[(8,32),(17,26),(17,38)],[(56,32),(47,26),(47,38)]]:d.polygon(p,fill=ink)
 else:
  d.rectangle((10,32,54,49),outline=ink,width=3);d.line((20,27,20,15,44,15,44,27),fill=ink,width=4)
 im.alpha_composite(t,((idx%4)*64,(idx//4)*64))
p=Path('Assets/Resources/Presentation/TouchIcons.png');im.save(p,optimize=True)
