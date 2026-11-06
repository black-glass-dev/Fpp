"""Import the original skinned pistol with Blender; export its rig and authored clips."""
import json, os
from pathlib import Path
import bpy
from mathutils import Vector
source=Path('.asset-staging/packs/mark_23__animated_free.glb').resolve()
output=Path('Assets/Viewmodels/Mark23');output.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
materials=[]
for material in bpy.data.materials:
    if not material.use_nodes:continue
    bsdf=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    image=None
    if bsdf and bsdf.inputs['Base Color'].is_linked:
        node=bsdf.inputs['Base Color'].links[0].from_node
        image=getattr(node,'image',None)
    if image:
        path=output/(material.name+'.png')
        image.filepath_raw=str(path.resolve());image.file_format='PNG';image.save()
        materials.append(dict(name=material.name,texture=path.as_posix()))
armatures=[o for o in bpy.data.objects if o.type=='ARMATURE']
for rig in armatures:
    if rig.animation_data:
        for track in rig.animation_data.nla_tracks: track.mute=True
    draw=next((a for a in bpy.data.actions if 'draw' in a.name.lower()),None)
    if draw:
        rig.animation_data_create();rig.animation_data.action=draw
        bpy.context.scene.frame_set(int(draw.frame_range[1]))
bpy.context.view_layer.update()
# Render the actual textured skinned pose for review (not a fabricated concept image).
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.resolution_x=960;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Review world');scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.28,.32,.4,1)
scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.8
points=[]
for obj in bpy.data.objects:
    if obj.type=='MESH':
        evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh=evaluated.to_mesh()
        points.extend(obj.matrix_world@v.co for v in mesh.vertices)
        evaluated.to_mesh_clear()
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
center=(lo+hi)/2;size=max(hi-lo)
camera_data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',camera_data);scene.collection.objects.link(camera)
camera.location=center+Vector((size*.6,-size*1.3,size*.6));camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.lens=45;scene.camera=camera
light_data=bpy.data.lights.new('Softbox','AREA');light_data.energy=800;light_data.shape='DISK';light_data.size=max(.5,size*2)
light=bpy.data.objects.new('Softbox',light_data);scene.collection.objects.link(light);light.location=center+Vector((size,-size,size*2));light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
Path('Docs/Previews').mkdir(parents=True,exist_ok=True);scene.render.filepath=str(Path('Docs/Previews/mark23-source-pose.png').resolve());bpy.ops.render.render(write_still=True)
# All original actions remain available; FBX export includes real skin weights/bind matrices.
for rig in armatures:
    rig.animation_data.action=None
    for track in rig.animation_data.nla_tracks:track.mute=False
bpy.ops.export_scene.fbx(filepath=str((output/'Mark23.fbx').resolve()),object_types={'ARMATURE','MESH'},
    use_selection=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
report=dict(materials=materials,actions=[a.name for a in bpy.data.actions],bounds=[list(lo),list(hi)],
    bones=[b.name for rig in armatures for b in rig.data.bones])
(output/'rig.json').write_text(json.dumps(report,indent=2)+'\n')
print('::notice::Skinned pistol exported with clips: '+', '.join(report['actions']))
# bpy's embedded interpreter can crash during Python shutdown after successful export.
# All output operations above are synchronous; validate files before a clean process exit.
assert (output/'Mark23.fbx').stat().st_size > 1000
import sys
sys.stdout.flush();sys.stderr.flush();os._exit(0)
