import bpy
import os
import math
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SOURCE = os.path.join(ROOT, "assets", "moss-cloak-side-rig-parts-v2.png")
OUT = os.path.join(ROOT, "output", "moss-cloak-walk-right-v2")
BLEND = os.path.join(OUT, "moss-cloak-walk-right-v2.blend")
os.makedirs(os.path.join(OUT, "frames"), exist_ok=True)

# The atlas layout was authored on a 1536-square design grid. The generated PNG
# is 1254-square, so all crop data is converted proportionally.
DESIGN = 1536.0
WORLD_SCALE = 0.01


def d(value, actual):
    return value * actual / DESIGN


def scene_point(px, py):
    """Map design-grid pixels to the X/Z animation plane."""
    return Vector(((px - DESIGN / 2) * WORLD_SCALE, 0.0, (DESIGN - py) * WORLD_SCALE))


def clean_magenta(image):
    """Convert the uniform #FF00FF generation background into real alpha."""
    pixels = list(image.pixels[:])
    for i in range(0, len(pixels), 4):
        r, g, b = pixels[i], pixels[i + 1], pixels[i + 2]
        magenta = min(r, b) - g
        if r > 0.30 and b > 0.30 and magenta > 0.02:
            # Remove the matte aggressively. A short feather keeps antialiasing
            # without the green halo caused by dividing low-alpha RGB values.
            alpha = max(0.0, min(1.0, 1.0 - (magenta - 0.02) / 0.18))
            pixels[i + 3] = alpha
            if alpha < 0.015:
                pixels[i] = pixels[i + 1] = pixels[i + 2] = 0.0
            elif alpha < 0.98:
                pixels[i] = min(r, g + 0.10)
                pixels[i + 2] = min(b, g + 0.10)
    # The source atlas is a 24-bit RGB PNG. Mutating its in-memory alpha and
    # saving it keeps the original three-channel layout, losing transparency
    # after the .blend is reopened. Create a new four-channel image explicitly.
    rgba = bpy.data.images.new(
        "SideRig_Atlas_RGBA",
        width=image.size[0],
        height=image.size[1],
        alpha=True,
        float_buffer=False,
    )
    rgba.colorspace_settings.name = image.colorspace_settings.name
    rgba.pixels.foreach_set(pixels)
    rgba.update()
    rgba.filepath_raw = os.path.join(OUT, "side-rig-parts-transparent-v2.png")
    rgba.file_format = 'PNG'
    rgba.save()
    return rgba


def build_material(image):
    material = bpy.data.materials.new("SideRig_Transparent")
    material.use_nodes = True
    # Blender 5.2 defaults new materials to opaque viewport rendering. Without
    # this, transparent texels appear as black rectangular image planes in
    # Material Preview even though final RGBA renders are transparent.
    material.surface_render_method = 'DITHERED'
    if hasattr(material, 'use_transparency_overlap'):
        material.use_transparency_overlap = False
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    mix = nodes.new('ShaderNodeMixShader')
    transparent = nodes.new('ShaderNodeBsdfTransparent')
    emission = nodes.new('ShaderNodeEmission')
    texture = nodes.new('ShaderNodeTexImage')
    texture.image = image
    texture.interpolation = 'Linear'
    links.new(texture.outputs['Color'], emission.inputs['Color'])
    links.new(texture.outputs['Alpha'], mix.inputs[0])
    links.new(transparent.outputs[0], mix.inputs[1])
    links.new(emission.outputs[0], mix.inputs[2])
    links.new(mix.outputs[0], output.inputs[0])
    return material


def add_atlas_part(name, rect, source_pivot, target_pivot, depth, material, width, height):
    x0, y0, x1, y1 = rect
    source = scene_point(*source_pivot)
    target = scene_point(*target_pivot)
    verts = [
        scene_point(x0, y0) - source,
        scene_point(x1, y0) - source,
        scene_point(x1, y1) - source,
        scene_point(x0, y1) - source,
    ]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], [[0, 1, 2, 3]])
    mesh.uv_layers.new(name="UV")
    u0, u1 = d(x0, width) / width, d(x1, width) / width
    v0, v1 = 1.0 - d(y1, height) / height, 1.0 - d(y0, height) / height
    for loop, uv in zip(mesh.uv_layers.active.data, [(u0, v1), (u1, v1), (u1, v0), (u0, v0)]):
        loop.uv = uv
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = target
    obj.location.y = depth
    return obj


def add_bone(edit_bones, name, head, tail, parent=None):
    bone = edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    if parent:
        bone.parent = edit_bones[parent]
        bone.use_connect = False
    return bone


def bind_rigid_part(obj, armature, bone_name):
    constraint = obj.constraints.new('CHILD_OF')
    constraint.name = "Bind_" + bone_name
    constraint.target = armature
    constraint.subtarget = bone_name
    bpy.context.view_layer.update()
    constraint.inverse_matrix = (armature.matrix_world @ armature.pose.bones[bone_name].matrix).inverted()


def set_rotation(pose_bone, frame, degrees):
    # Bone-local Y runs along the bone. Local Z is the in-plane hinge axis for
    # this X/Z cutout rig, so it produces visible shoulder/hip/knee rotation.
    pose_bone.rotation_euler.z = math.radians(degrees)
    pose_bone.keyframe_insert(data_path='rotation_euler', index=2, frame=frame)


# Reset to a predictable empty scene.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

source_image = bpy.data.images.load(SOURCE)
width, height = source_image.size[:]
image = clean_magenta(source_image)
material = build_material(image)

# rect; pivot inside source cutout; pivot in assembled right-facing rest pose; depth.
# Negative Y is closer to camera. Far limbs are behind the cloak, near limbs in front.
specs = {
    'head':           ((95, 45, 410, 515), (265, 455), (805, 475), -0.18),
    'torso':          ((570, 45, 865, 610), (700, 90), (790, 445), 0.00),
    'cape_back':      ((995, 45, 1415, 620), (1205, 100), (760, 450), 0.12),
    'upper_arm.Far':  ((180, 630, 405, 915), (275, 650), (755, 545), 0.24),
    'forearm.Far':    ((465, 635, 665, 910), (535, 660), (730, 725), 0.23),
    'upper_arm.Near': ((820, 625, 1045, 925), (910, 650), (785, 545), -0.28),
    'forearm.Near':   ((1080, 635, 1285, 920), (1160, 665), (815, 725), -0.30),
    'pelvis':         ((40, 945, 270, 1235), (150, 975), (770, 885), -0.06),
    'thigh.Far':      ((300, 935, 455, 1235), (370, 955), (750, 900), 0.22),
    'shin.Far':       ((530, 965, 650, 1205), (590, 1030), (750, 1140), 0.21),
    'foot.Far':       ((700, 1000, 925, 1240), (780, 1180), (750, 1340), 0.20),
    'thigh.Near':     ((950, 935, 1110, 1235), (1025, 955), (790, 900), -0.22),
    'shin.Near':      ((1150, 970, 1250, 1205), (1200, 1035), (790, 1140), -0.23),
    'foot.Near':      ((1275, 1020, 1515, 1245), (1365, 1180), (790, 1340), -0.24),
    'potion':         ((455, 1250, 610, 1525), (525, 1265), (865, 600), -0.34),
    'fringe.Near':    ((735, 1260, 825, 1515), (780, 1270), (735, 820), -0.32),
}
objects = {
    name: add_atlas_part(name, rect, source, target, depth, material, width, height)
    for name, (rect, source, target, depth) in specs.items()
}

# The generated torso already contains its cloak volume. Retain cape_back as an
# editable optional layer but hide it by default to avoid doubling the silhouette.
objects['cape_back'].hide_render = True

# Armature rest pose follows the actual right-facing anatomy.
bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
rig = bpy.context.object
rig.name = 'MossCloak_Right_Rig'
rig.data.name = 'MossCloak_Right_Armature'
edit = rig.data.edit_bones
edit.remove(edit[0])
add_bone(edit, 'root', scene_point(770, 1390), scene_point(770, 1260))
add_bone(edit, 'pelvis', scene_point(770, 930), scene_point(780, 760), 'root')
add_bone(edit, 'spine', scene_point(780, 760), scene_point(790, 470), 'pelvis')
add_bone(edit, 'head', scene_point(795, 455), scene_point(810, 250), 'spine')
add_bone(edit, 'upper_arm.Far', scene_point(755, 545), scene_point(730, 720), 'spine')
add_bone(edit, 'forearm.Far', scene_point(730, 720), scene_point(760, 860), 'upper_arm.Far')
add_bone(edit, 'upper_arm.Near', scene_point(785, 545), scene_point(815, 720), 'spine')
add_bone(edit, 'forearm.Near', scene_point(815, 720), scene_point(845, 860), 'upper_arm.Near')
add_bone(edit, 'thigh.Far', scene_point(750, 900), scene_point(750, 1140), 'pelvis')
add_bone(edit, 'shin.Far', scene_point(750, 1140), scene_point(750, 1340), 'thigh.Far')
add_bone(edit, 'foot.Far', scene_point(750, 1340), scene_point(865, 1360), 'shin.Far')
add_bone(edit, 'thigh.Near', scene_point(790, 900), scene_point(790, 1140), 'pelvis')
add_bone(edit, 'shin.Near', scene_point(790, 1140), scene_point(790, 1340), 'thigh.Near')
add_bone(edit, 'foot.Near', scene_point(790, 1340), scene_point(905, 1360), 'shin.Near')
add_bone(edit, 'potion', scene_point(850, 585), scene_point(855, 735), 'spine')
add_bone(edit, 'fringe.Near', scene_point(735, 815), scene_point(735, 1030), 'pelvis')
bpy.ops.object.mode_set(mode='POSE')
for bone in rig.pose.bones:
    bone.rotation_mode = 'XYZ'
bpy.ops.object.mode_set(mode='OBJECT')

bindings = {
    'head': 'head', 'torso': 'spine', 'upper_arm.Far': 'upper_arm.Far',
    'forearm.Far': 'forearm.Far', 'upper_arm.Near': 'upper_arm.Near',
    'forearm.Near': 'forearm.Near', 'pelvis': 'pelvis',
    'thigh.Far': 'thigh.Far', 'shin.Far': 'shin.Far', 'foot.Far': 'foot.Far',
    'thigh.Near': 'thigh.Near', 'shin.Near': 'shin.Near', 'foot.Near': 'foot.Near',
    'potion': 'potion', 'fringe.Near': 'fringe.Near', 'cape_back': 'spine',
}
for object_name, bone_name in bindings.items():
    bind_rigid_part(objects[object_name], rig, bone_name)

# A readable in-place rightward walk: contact, down, opposite contact, passing.
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='POSE')
action = bpy.data.actions.new('Walk_Right')
rig.animation_data_create()
rig.animation_data.action = action

poses = {
    1: {
        'thigh.Near': -27, 'shin.Near': 10, 'foot.Near': 5,
        'thigh.Far': 25, 'shin.Far': -34, 'foot.Far': 12,
        'upper_arm.Near': 19, 'forearm.Near': -12,
        'upper_arm.Far': -20, 'forearm.Far': 13,
        'spine': -2.0, 'head': 1.5, 'pelvis': 1.5, 'potion': -5, 'fringe.Near': 3,
    },
    7: {
        'thigh.Near': -6, 'shin.Near': 18, 'foot.Near': -4,
        'thigh.Far': 4, 'shin.Far': -12, 'foot.Far': 4,
        'upper_arm.Near': 5, 'forearm.Near': -5,
        'upper_arm.Far': -5, 'forearm.Far': 6,
        'spine': 1.0, 'head': -0.8, 'pelvis': -1.0, 'potion': 4, 'fringe.Near': -3,
    },
    13: {
        'thigh.Near': 25, 'shin.Near': -34, 'foot.Near': 12,
        'thigh.Far': -27, 'shin.Far': 10, 'foot.Far': 5,
        'upper_arm.Near': -20, 'forearm.Near': 13,
        'upper_arm.Far': 19, 'forearm.Far': -12,
        'spine': 2.0, 'head': -1.5, 'pelvis': -1.5, 'potion': 5, 'fringe.Near': -3,
    },
    19: {
        'thigh.Near': 4, 'shin.Near': -12, 'foot.Near': 4,
        'thigh.Far': -6, 'shin.Far': 18, 'foot.Far': -4,
        'upper_arm.Near': -5, 'forearm.Near': 6,
        'upper_arm.Far': 5, 'forearm.Far': -5,
        'spine': -1.0, 'head': 0.8, 'pelvis': 1.0, 'potion': -4, 'fringe.Near': 3,
    },
    25: {
        'thigh.Near': -27, 'shin.Near': 10, 'foot.Near': 5,
        'thigh.Far': 25, 'shin.Far': -34, 'foot.Far': 12,
        'upper_arm.Near': 19, 'forearm.Near': -12,
        'upper_arm.Far': -20, 'forearm.Far': 13,
        'spine': -2.0, 'head': 1.5, 'pelvis': 1.5, 'potion': -5, 'fringe.Near': 3,
    },
}
for frame, pose in poses.items():
    bpy.context.scene.frame_set(frame)
    root = rig.pose.bones['root']
    root.location.z = -0.08 if frame in (1, 13, 25) else 0.06
    root.location.x = 0.0
    root.keyframe_insert(data_path='location', frame=frame)
    for bone_name, angle in pose.items():
        # The painted boot openings are not circular joint caps. Keeping the
        # ankle rigid prevents a visible cutout gap while the shin still swings.
        if bone_name.startswith('foot.'):
            angle = 0.0
        set_rotation(rig.pose.bones[bone_name], frame, angle)
bpy.ops.object.mode_set(mode='OBJECT')

# Camera and transparent render settings.
bpy.ops.object.camera_add(location=(0, -22, 7.25), rotation=(math.radians(90), 0, 0))
camera = bpy.context.object
camera.name = 'Camera_RightWalk'
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 15.5
bpy.context.scene.camera = camera
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.film_transparent = True
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.fps = 24
scene.frame_start = 1
scene.frame_end = 24
scene.render.filepath = os.path.join(OUT, 'frames', 'walk_right_')

# Helpful viewport defaults for opening the file interactively.
rig.show_in_front = True
rig.data.display_type = 'BBONE'
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
bpy.ops.render.render(animation=True)
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print('RIGHT_WALK_V2_COMPLETE', BLEND)
