"""Reduce snapshot PNG payload without changing a single decoded camera-render pixel."""
from io import BytesIO
from pathlib import Path
from PIL import Image

for path in Path('Docs/Previews').glob('unity-*.png'):
    image=Image.open(path)
    image.load()
    data=BytesIO()
    image.save(data,format='PNG',optimize=True)
    encoded=data.getvalue()
    if len(encoded)<path.stat().st_size:
        assert Image.open(BytesIO(encoded)).tobytes()==image.tobytes()
        temporary=path.with_suffix(".tmp.png")
        temporary.write_bytes(encoded)
        temporary.replace(path) # new captures may be owned by the builder container
