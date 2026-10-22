class_name BalanceConfig
extends Resource

## 远征玩法共享的平衡参数。距离使用像素，速度使用像素/秒，持续时间使用秒。
@export_group("玩家与剑")
## 忆身可承受的最大伤害点数。
@export var max_health: float = 5
## 每次开启远征时的生命值。
@export var starting_health: float = 5
## 每次开启远征时的剑冢品阶。
@export var starting_fire_level: int = 1
## 神识储量上限。
@export var max_ember: float = 100
## 每次开启远征时的神识储量。
@export var starting_ember: float = 100
## 忆身水平移动速度，单位为像素/秒。
@export var player_move_speed: float = 250
## 跳跃时施加的重力加速度，单位为像素/秒²。
@export var gravity: float = 900
## 起跳速度，单位为像素/秒。
@export var jump_impulse: float = 440
## 单次突进的移动距离，单位为像素。
@export var dash_distance: float = 220
## 突进移动速度，单位为像素/秒。
@export var dash_speed: float = 1400
## 连续两次突进之间的等待时间，单位为秒。
@export var dash_cooldown: float = .55
## 御剑时飞剑的最高移动速度，单位为像素/秒。
@export var sword_control_speed: float = 700
## 飞剑追随鼠标目标的响应强度，数值越大转向越快。
@export var sword_control_smoothness: float = 12
## 飞剑控制目标相对飞剑当前位置的最大偏移，单位为像素。
@export var sword_control_max_lead: float = 180
## 飞剑攻击时的移动速度，单位为像素/秒。
@export var sword_attack_speed: float = 1000
## 进入或退出御剑控制的过渡时长，单位为秒。
@export var sword_control_transition_duration: float = .22
## 飞剑归鞘速度，单位为像素/秒。
@export var sword_return_speed: float = 1200
## 主火恢复神识的范围半径，单位为像素。
@export var sword_tomb_radius: float = 100
## 飞剑每次命中造成的统一伤害点数。
@export var sword_damage: float = 50
## 铁匠铺打造一把飞剑所需的铸剑铁料。
@export var sword_craft_material_cost: int = 3
## 铁匠铺交互范围，单位为像素。
@export var blacksmith_range: float = 120
## 御剑持续操控时的命中判定半径，单位为像素。
@export var sword_hit_radius: float = 90
## 御剑持续操控时两次命中之间的间隔，单位为秒。
@export var sword_hit_cooldown: float = .12
## 每次保留最近剑式的最长时长，单位为秒。
@export var sword_record_max_duration: float = 4
## 剑式被视为有效录制所需的最短时长，单位为秒。
@export var sword_record_min_duration: float = .5
## 剑式被视为有效录制所需的最短累计飞行距离，单位为像素。
@export var sword_record_min_distance: float = 50
## 从剑重放伤害相对飞剑基础伤害的倍率。
@export var sword_replay_damage_multiplier: float = .5
## 飞剑主动攻击的命中判定半径，单位为像素。
@export var enemy_attack_hit_radius: float = 70
## 飞剑主动攻击之间的间隔，单位为秒。
@export var attack_cooldown: float = .25
@export_group("灵蕴与神识")
## 剑冢灵蕴上限；当前界面按百分比展示。
@export var max_fuel: float = 100
## 每次开启远征时的灵蕴值。
@export var starting_fuel: float = 70
## 灵蕴高于此值时显示主火第 2 阶段，单位同灵蕴。
@export var fire_stage_threshold1: float = 25
## 灵蕴高于此值时显示主火第 3 阶段，单位同灵蕴。
@export var fire_stage_threshold2: float = 50
## 灵蕴高于此值时显示主火第 4 阶段，单位同灵蕴。
@export var fire_stage_threshold3: float = 75
## 每秒基础灵蕴衰减值。
@export var fuel_base_decay: float = .42
## 剑冢每升一阶增加的每秒灵蕴衰减值。
@export var fuel_decay_per_fire_level: float = .08
## 在主火旁投入一块剑骸恢复的灵蕴值。
@export var fuel_feed_amount: float = 17
## 强炼剑铭时恢复的灵蕴值。
@export var name_burn_fuel_amount: float = 42
## 强炼剑铭增加的剑煞值。
@export var name_burn_dream_ash: float = 2
## 归葬剑铭时减少的剑煞值。
@export var name_return_dream_ash_reduction: float = 1
## 不受御剑控制时每秒自然恢复的神识值。
@export var ember_passive_recovery: float = 7
## 处于主火或临时火阵范围内时每秒恢复的神识值。
@export var ember_tomb_recovery: float = 45
## 飞剑离开恢复范围后每秒消耗的基础神识值。
@export var ember_control_base_drain: float = 4
## 御剑距离消耗系数；距离每增加此像素数，每秒额外消耗 1 点神识。
@export var ember_control_distance_drain: float = 85
@export_group("敌人与遭遇")
## 地图中心到左右边界的距离，单位为像素。
@export var world_half_width: float = 3600
## 村庄从剑冢向左右延伸的距离，单位为像素。
@export var village_radius: float = 550
## 主角进入此距离后，附近怪物才开始寻敌，单位为像素。
@export var enemy_aggro_range: float = 360
## 怪物未发现目标时的游走速度，单位为像素/秒。
@export var enemy_wander_speed: float = 40
## 怪物未发现目标时围绕当前位置游走的最大距离，单位为像素。
@export var enemy_wander_radius: float = 180
## 白天持续时间，单位为秒。
@export var day_duration: float = 90
## 夜晚持续时间，单位为秒。
@export var night_duration: float = 60
## 普通敌人的生命值。
@export var enemy_health: float = 100
## 远征开始后首次生成敌人的等待时间，单位为秒。
@export var enemy_initial_spawn_delay: float = 6
## 敌人基础生成间隔，单位为秒。
@export var enemy_spawn_base_interval: float = 7.5
## 剑冢每提升一阶，敌人生成间隔减少的秒数。
@export var enemy_spawn_fire_level_factor: float = 1
## 每点剑煞使敌人生成间隔减少的秒数。
@export var enemy_spawn_dream_ash_factor: float = .12
## 敌人生成间隔的下限，单位为秒。
@export var enemy_spawn_min_interval: float = 2.4
## 猎手移动速度，单位为像素/秒。
@export var hunter_speed: float = 105
## 啮阵兽与盗铭伥的移动速度，单位为像素/秒。
@export var other_enemy_speed: float = 75
## 猎手触发攻击的距离，单位为像素。
@export var hunter_attack_range: float = 34
## 猎手两次攻击之间的间隔，单位为秒。
@export var hunter_attack_cooldown: float = 1.3
## 忆身受击后的无敌时长，单位为秒。
@export var player_hurt_duration: float = 1
## 啮阵兽靠近主火到此距离时开始侵蚀，单位为像素。
@export var extinguisher_attack_range: float = 38
## 啮阵兽每次攻击扣除的灵蕴值。
@export var extinguisher_fuel_damage: float = 6
## 啮阵兽两次攻击之间的间隔，单位为秒。
@export var extinguisher_attack_cooldown: float = 2.2
## 盗铭伥触发偷取剑骸的距离，单位为像素。
@export var thief_attack_range: float = 35
## 盗铭伥两次偷取尝试之间的间隔，单位为秒。
@export var thief_attack_cooldown: float = 1.5
## 敌人受击数字的显示时长，单位为秒。
@export var enemy_damage_feedback_duration: float = .55
## 敌人受伤动画时长，单位为秒。
@export var enemy_hurt_duration: float = .2
## 敌人死亡动画时长，单位为秒。
@export var enemy_death_animation_duration: float = .25
## 已搜空尸体的保留时长，单位为秒。
@export var enemy_corpse_lifetime: float = 18
## 尚未搜刮尸体的最长保留时长，单位为秒。
@export var enemy_unclaimed_corpse_lifetime: float = 60
@export_group("搜刮与成长")
## 完成一次搜刮所需的持续时间，单位为秒。
@export var search_duration: float = 2.2
## 玩家进入此距离内可搜刮宝箱或尸体，单位为像素。
@export var search_range: float = 72
## 宝箱剑骸掉落数量下限，包含该值。
@export var chest_wood_min: int = 3
## 宝箱剑骸掉落数量上限，不包含该值。
@export var chest_wood_max_exclusive: int = 6
## 尸体剑骸掉落数量下限，包含该值。
@export var corpse_wood_min: int = 1
## 尸体剑骸掉落数量上限，不包含该值。
@export var corpse_wood_max_exclusive: int = 4
## 宝箱奖励随机数的取值数量；当前每 4 种结果中有 1 种疗伤丹奖励。
@export var chest_bonus_roll_count: int = 4
## 使用一枚疗伤丹恢复的生命点数。
@export var healing_potion_restore: float = 2
## 剑冢每升一阶需要的剑骸数量倍数，实际花费为当前品阶乘以此值。
@export var fire_upgrade_wood_per_level: int = 6
## 剑冢最高品阶；达到此品阶会开启最终剑潮。
@export var max_fire_level: int = 3
## 最终剑潮持续时长，单位为秒。
@export var final_night_duration: float = 30
## 最终剑潮开始时立即生成的敌人数。
@export var final_night_initial_enemy_count: int = 6
## 忆身重凝时扣除的灵蕴值。
@export var respawn_fuel_penalty: float = 15
## 忆身重凝时损失的剑骸比例分母；2 表示损失一半。
@export var respawn_wood_loss_divisor: int = 2
## 剑标持续时间，单位为秒。
@export var torch_lifetime: float = 22
## 剑标恢复神识的范围半径，单位为像素。
@export var torch_radius: float = 95
## 布置剑标需要的剑骸数量。
@export var torch_wood_cost: int = 1
## 临时剑阵持续时间，单位为秒。
@export var campfire_lifetime: float = 55
## 临时剑阵恢复神识的范围半径，单位为像素。
@export var campfire_radius: float = 170
## 布置临时剑阵需要的剑骸数量。
@export var campfire_wood_cost: int = 3
## 初始及后续宝箱生成计时基准，单位为秒。
@export var chest_initial_spawn_interval: float = 12
## 场上未搜刮宝箱的数量达到此值时暂停生成新宝箱。
@export var chest_max_unsearched_count: int = 3
