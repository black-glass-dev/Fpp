"""Bake GLB static scene nodes to mobile OBJ parts; preserve original archives externally.
Animations/rigs are NOT exported. Decimated UVs use nearest-source-vertex transfer.
"""
import argparse, hashlib, json, re, tempfile
from pathlib import Path
import numpy as np
from PIL import Image
import trimesh
from scipy.spatial import cKDTree


def reduce_preserving_uv(mesh, face_count):
    import pymeshlab
    uv=np.asarray(mesh.visual.uv)
    ms=pymeshlab.MeshSet()
    # MeshLab's array constructor leaves wedge texture indices unassigned (-1).
    # Its OBJ+MTL importer initializes real texture slots and UV chart ownership.
    with tempfile.TemporaryDirectory() as folder:
        obj=Path(folder)/'source.obj';mesh.export(obj)
        ms.load_new_mesh(str(obj))
        ms.meshing_remove_duplicate_vertices()
        ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum=face_count,
            preserveboundary=False,preservenormal=True,extratcoordw=3.0)
    reduced=ms.current_mesh();faces=reduced.face_matrix()
    points=reduced.vertex_matrix()[faces].reshape(-1,3)
    tex=reduced.wedge_tex_coord_matrix().reshape(-1,2)
    normals=reduced.vertex_normal_matrix()[faces].reshape(-1,3)
    # OBJ/Unity needs per-vertex UVs: split only actual UV/normal seams, not every face.
    packed=np.round(np.column_stack([points,tex,normals]),7)
    _,unique,inverse=np.unique(packed,axis=0,return_index=True,return_inverse=True)
    return trimesh.Trimesh(vertices=points[unique],faces=inverse.reshape(-1,3),
        vertex_normals=normals[unique],process=False,
        visual=trimesh.visual.TextureVisuals(uv=tex[unique],material=mesh.visual.material))


def convert(source, output, budget_total=16000, preserve_uv=False):
    scene = trimesh.load(source, force='scene', process=False)
    slug = re.sub(r'[^a-zA-Z0-9]+', '_', source.stem).strip('_')
    folder = output / slug
    folder.mkdir(parents=True, exist_ok=True)
    parts, textures = [], {}
    original_faces = sum(len(g.faces) for g in scene.geometry.values())
    total = max(1, original_faces)
    for index, node in enumerate(sorted(scene.graph.nodes_geometry)):
        matrix, key = scene.graph[node]
        original = scene.geometry[key]
        if not isinstance(original, trimesh.Trimesh):
            continue
        mesh = original.copy()
        material = getattr(mesh.visual, 'material', None)
        uv = getattr(mesh.visual, 'uv', None)
        budget = max(100, int(budget_total * len(mesh.faces) / total))
        if preserve_uv and uv is not None and len(mesh.faces)>budget:
            mesh=reduce_preserving_uv(mesh,budget)
        elif len(mesh.faces) > max(20000, budget):
            reduced = mesh.simplify_quadric_decimation(face_count=budget)
            if uv is not None:
                _, near = cKDTree(mesh.vertices).query(reduced.vertices)
                reduced.visual = trimesh.visual.TextureVisuals(uv=uv[near], material=material)
            mesh = reduced
        mesh.apply_transform(matrix)
        tex_path = ''
        image = getattr(material, 'baseColorTexture', None)
        color = getattr(material, 'baseColorFactor', None)
        if image is not None:
            image = image.copy().convert('RGBA')
            image.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
            digest = hashlib.sha256(image.tobytes()).hexdigest()
            if digest not in textures:
                tex_name = 'Texture_%02d.png' % len(textures)
                image.save(folder / tex_name, optimize=True)
                textures[digest] = tex_name
            tex_path = (folder / textures[digest]).as_posix()
        obj = folder / ('Part_%03d.obj' % index)
        text = trimesh.exchange.obj.export_obj(mesh, include_texture=True,
            write_texture=False, include_color=False, include_normals=True, header='FPP static mobile conversion')
        obj.write_text('\n'.join(l for l in text.splitlines() if not l.startswith(('mtllib ', 'usemtl '))) + '\n')
        # No external MTL required: Editor explicitly binds the texture/material from this catalog.
        factor = [float(x) for x in color] if color is not None else [255,255,255,255]
        if max(factor) > 1: factor = [x / 255 for x in factor]
        parts.append(dict(model=obj.as_posix(), texture=tex_path, color=factor,
            cutout=(getattr(material, 'alphaMode', '') == 'MASK' or 'Tree' in slug), triangles=len(mesh.faces)))
    weapon = any(s in slug.lower() for s in ['gun','pistol','kriss','mark_23','ak_47','glock'])
    with source.open('rb') as stream:
        source_hash = hashlib.file_digest(stream,'sha256').hexdigest()
    return dict(name=source.stem, weapon=weapon, sourceSha256=source_hash,
        sourceTriangles=original_faces, parts=parts)

if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('source',type=Path)
    parser.add_argument('--output',type=Path,default=Path('Assets/PackModels'))
    parser.add_argument('--catalog',type=Path,default=Path('Assets/PackModels/catalog.json'))
    args=parser.parse_args()
    sources=sorted(p for p in args.source.rglob('*') if p.suffix.lower() in ('.glb', '.gltf', '.obj'))
    if not sources: raise SystemExit('No supported GLB/glTF/OBJ models found')
    models=[]
    for source in sources:
        print('Converting',source,flush=True)
        models.append(convert(source,args.output))
    args.catalog.parent.mkdir(parents=True, exist_ok=True)
    args.catalog.write_text(json.dumps(dict(models=models),indent=2)+'\n')
    print('Converted',len(models),'models;',sum(sum(p['triangles'] for p in m['parts']) for m in models),'triangles')
