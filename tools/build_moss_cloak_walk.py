import bpy
import os
import math
from mathutils import Vector, Matrix

# Run with Blender 5.2: blender.exe --background --python build_moss_cloak_walk.py
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SOURCE = os.path.join(ROOT, "assets", "moss-cloak-rig-parts.png")
OUT = os.path.join(ROOT, "output", "moss-cloak-walk")
BLEND = os.path.join(OUT, "moss-cloak-walk.blend")
os.makedirs(OUT, exist_ok=True)

W, H, SCALE = 1024, 1536, 0.01

def world(px, py):
    return Vector(((px - W / 2) * SCALE, 0, (H - py) * SCALE))

def clean_alpha(image):
    """Remove only the near-neutral light checkerboard; keep the warm ivory mask."""
    pixels = list(image.pixels[:])
    for i in range(0, len(pixels), 4):
        r, g, b = pixels[i], pixels[i + 1], pixels[i + 2]
        mx, mn = max(r, g, b), min(r, g, b)
        # The generated backdrop is almost white/neutral. The mask is warm, so it remains.
        if mn > 0.89 and (mx - mn) < 0.045:
            pixels[i + 3] = 0.0
    image.pixels.foreach_set(pixels)
    image.update()
    image.filepath_raw = os.path.join(OUT, "rig-parts-transparent.png")
    image.file_format = 'PNG'
    image.save()

def mat_for(image):
    mat = bpy.data.materials.new("RigParts_Alpha")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    for node in list(nodes): nodes.remove(node)
    out = nodes.new('ShaderNodeOutputMaterial')
    mix = nodes.new('ShaderNodeMixShader')
    transparent = nodes.new('ShaderNodeBsdfTransparent')
    emission = nodes.new('ShaderNodeEmission')
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image
    links.new(tex.outputs['Color'], emission.inputs['Color'])
    links.new(tex.outputs['Alpha'], mix.inputs[0])
    links.new(transparent.outputs[0], mix.inputs[1])
    links.new(emission.outputs[0], mix.inputs[2])
    links.new(mix.outputs[0], out.inputs[0])
    return mat

def add_part(name, rect, source_pivot, target_pivot, material):
    x0, y0, x1, y1 = rect
    source = world(*source_pivot)
    target = world(*target_pivot)
    corners = [world(x0,y0)-source, world(x1,y0)-source, world(x1,y1)-source, world(x0,y1)-source]
    # UV v is bottom-origin.
    u0, u1 = x0/W, x1/W
    v0, v1 = 1-y1/H, 1-y0/H
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(corners, [], [[0, 1, 2, 3]])
    mesh.uv_layers.new(name="UV")
    uv = mesh.uv_layers.active.data
    for loop, co in zip(uv, [(u0,v1), (u1,v1), (u1,v0), (u0,v0)]): loop.uv = co
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = target
    return obj

def parent_to_bone(obj, arm, bone):
    # A Child Of constraint gives deterministic, edit-friendly bone binding.
    c = obj.constraints.new('CHILD_OF')
    c.name = "Bind_" + bone
    c.target = arm
    c.subtarget = bone
    bpy.context.view_layer.update()
    c.inverse_matrix = (arm.matrix_world @ arm.pose.bones[bone].matrix).inverted()

def add_bone(edit, name, head, tail, parent=None):
    b = edit.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = edit[parent]
        b.use_connect = False
    return b

# Clear scene.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.materials, bpy.data.meshes, bpy.data.armatures, bpy.data.cameras):
    pass

image = bpy.data.images.load(SOURCE)
clean_alpha(image)
material = mat_for(image)

# Pixel-space cutouts and their intended joint pivots.
parts = {
    # rect, source pivot in the atlas, target pivot in the assembled rest pose
    'head':       ((365, 15, 625, 290), (512, 280), (512, 335)),
    'torso':      ((285, 275, 715, 750), (512, 475), (512, 505)),
    'upper_arm.L':((45, 205, 190, 580), (115, 220), (365, 390)),
    'forearm.L':  ((55, 585, 180, 835), (110, 600), (255, 500)),
    'upper_arm.R':((810, 205, 970, 580), (880, 220), (660, 390)),
    'forearm.R':  ((805, 590, 935, 840), (875, 600), (770, 500)),
    'pelvis':     ((350, 720, 625, 1015), (512, 745), (512, 755)),
    'thigh.L':    ((60, 840, 260, 1165), (160, 850), (415, 790)),
    'shin.L':     ((105, 1150, 230, 1345), (165, 1160), (415, 1065)),
    'foot.L':     ((40, 1305, 245, 1515), (150, 1320), (415, 1285)),
    'thigh.R':    ((735, 850, 955, 1165), (845, 850), (610, 790)),
    'shin.R':     ((770, 1155, 915, 1345), (845, 1160), (610, 1065)),
    'foot.R':     ((765, 1305, 980, 1515), (870, 1320), (610, 1285)),
    'vial':       ((655, 720, 760, 865), (705, 730), (570, 475)),
    'moss.L':     ((340, 1070, 455, 1455), (395, 1090), (465, 700)),
    'moss.R':     ((530, 1065, 670, 1455), (600, 1090), (555, 700)),
}
objects = {name: add_part(name, rect, source, target, material) for name, (rect, source, target) in parts.items()}

# Rig built in the neutral front-facing rest pose.
bpy.ops.object.armature_add(enter_editmode=True, location=(0,0,0))
arm = bpy.context.object
arm.name = 'MossCloak_Rig'
arm.data.name = 'MossCloak_Armature'
edit = arm.data.edit_bones
edit.remove(edit[0])
add_bone(edit, 'root', world(512, 1330), world(512, 1000))
add_bone(edit, 'pelvis', world(512, 1010), world(512, 750), 'root')
add_bone(edit, 'spine', world(512, 750), world(512, 465), 'pelvis')
add_bone(edit, 'head', world(512, 300), world(512, 105), 'spine')
add_bone(edit, 'upper_arm.L', world(375, 350), world(235, 480), 'spine')
add_bone(edit, 'forearm.L', world(235, 480), world(145, 690), 'upper_arm.L')
add_bone(edit, 'upper_arm.R', world(650, 350), world(790, 480), 'spine')
add_bone(edit, 'forearm.R', world(790, 480), world(875, 690), 'upper_arm.R')
add_bone(edit, 'thigh.L', world(420, 790), world(410, 1060), 'pelvis')
add_bone(edit, 'shin.L', world(410, 1060), world(410, 1300), 'thigh.L')
add_bone(edit, 'foot.L', world(410, 1300), world(410, 1435), 'shin.L')
add_bone(edit, 'thigh.R', world(605, 790), world(610, 1060), 'pelvis')
add_bone(edit, 'shin.R', world(610, 1060), world(610, 1300), 'thigh.R')
add_bone(edit, 'foot.R', world(610, 1300), world(610, 1435), 'shin.R')
bpy.ops.object.mode_set(mode='POSE')
for pbone in arm.pose.bones:
    pbone.rotation_mode = 'XYZ'
bpy.ops.object.mode_set(mode='OBJECT')

bindings = {
 'head':'head','torso':'spine','upper_arm.L':'upper_arm.L','forearm.L':'forearm.L',
 'upper_arm.R':'upper_arm.R','forearm.R':'forearm.R','pelvis':'pelvis',
 'thigh.L':'thigh.L','shin.L':'shin.L','foot.L':'foot.L','thigh.R':'thigh.R',
 'shin.R':'shin.R','foot.R':'foot.R','vial':'spine','moss.L':'spine','moss.R':'spine'
}
for obj_name, bone_name in bindings.items(): parent_to_bone(objects[obj_name], arm, bone_name)
# These atlas extras are retained as bindable parts but hidden from the clean body render:
# the original vial is already attached to the torso and the extracted moss is only for optional secondary motion.
for extra in ('vial', 'moss.L', 'moss.R'):
    objects[extra].hide_render = True

# A 24-frame walk cycle; the root remains in place so the loop tiles in-game.
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode='POSE')
action = bpy.data.actions.new('Walk')
arm.animation_data_create()
arm.animation_data.action = action
keyposes = {
    1:  {'thigh.L':-0.42,'shin.L':0.34,'foot.L':0.10,'thigh.R':0.42,'shin.R':-0.25,'foot.R':-0.08,'upper_arm.L':0.25,'forearm.L':-0.18,'upper_arm.R':-0.25,'forearm.R':0.18,'spine':0.035,'head':-0.025,'pelvis':0.02},
    7:  {'thigh.L':0.10,'shin.L':-0.10,'foot.L':-0.03,'thigh.R':-0.10,'shin.R':0.12,'foot.R':0.03,'upper_arm.L':-0.06,'forearm.L':0.05,'upper_arm.R':0.06,'forearm.R':-0.05,'spine':-0.018,'head':0.018,'pelvis':-0.02},
    13: {'thigh.L':0.42,'shin.L':-0.25,'foot.L':-0.08,'thigh.R':-0.42,'shin.R':0.34,'foot.R':0.10,'upper_arm.L':-0.25,'forearm.L':0.18,'upper_arm.R':0.25,'forearm.R':-0.18,'spine':-0.035,'head':0.025,'pelvis':-0.02},
    19: {'thigh.L':-0.10,'shin.L':0.12,'foot.L':0.03,'thigh.R':0.10,'shin.R':-0.10,'foot.R':-0.03,'upper_arm.L':0.06,'forearm.L':-0.05,'upper_arm.R':-0.06,'forearm.R':0.05,'spine':0.018,'head':-0.018,'pelvis':0.02},
    25: {'thigh.L':-0.42,'shin.L':0.34,'foot.L':0.10,'thigh.R':0.42,'shin.R':-0.25,'foot.R':-0.08,'upper_arm.L':0.25,'forearm.L':-0.18,'upper_arm.R':-0.25,'forearm.R':0.18,'spine':0.035,'head':-0.025,'pelvis':0.02},
}
for frame, values in keyposes.items():
    bpy.context.scene.frame_set(frame)
    # subtle vertical body compression on contact versus passing positions
    arm.pose.bones['root'].location.z = -0.045 if frame in (1,13,25) else 0.02
    arm.pose.bones['root'].keyframe_insert(data_path='location', index=2, frame=frame)
    for bone, angle in values.items():
        arm.pose.bones[bone].rotation_euler.y = angle
        arm.pose.bones[bone].keyframe_insert(data_path='rotation_euler', index=1, frame=frame)
# Blender 5.2 stores action curves in layered slots; its default Bezier interpolation
# is suitable for this soft cloth walk, so no legacy fcurve traversal is required.
bpy.ops.object.mode_set(mode='OBJECT')

# Camera and render settings.
bpy.ops.object.camera_add(location=(0, -20, 7.7), rotation=(math.radians(90), 0, 0))
cam = bpy.context.object
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 16.0
bpy.context.scene.camera = cam
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.film_transparent = True
scene.render.resolution_x = 512
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.fps = 24
scene.frame_start, scene.frame_end = 1, 24
scene.render.filepath = os.path.join(OUT, 'frames', 'walk_')
scene.render.image_settings.color_mode = 'RGBA'
os.makedirs(os.path.join(OUT, 'frames'), exist_ok=True)

# Render editable clean PNG frames. This Blender distribution has no FFMPEG
# image format registered, so the transparent PNG sequence is the preview source.
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
bpy.ops.render.render(animation=True)
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print('WALK_RIG_COMPLETE', BLEND)
