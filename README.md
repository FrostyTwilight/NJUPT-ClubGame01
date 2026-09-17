# NJUPTClubGame

> 基于 **Godot 4.8 + C# (.NET 10)** 的 2D 抓钩（Grappling Hook）动作游戏。
> 玩家向绿色锚点发射绳索、借钟摆摆动穿越地形，沿途激活检查点、躲避红色陷阱，并在终点与 Boss 展开多阶段战斗。

---

## 目录

- [玩法机制](#玩法机制)
- [操作方式](#操作方式)
- [快速开始](#快速开始)
- [项目结构](#项目结构)
- [技术架构](#技术架构)
- [状态机设计](#状态机设计)
- [物理与判定](#物理与判定)
- [Boss 战设计](#boss-战设计)
- [关卡与预制体](#关卡与预制体)
- [可调参数](#可调参数)
- [碰撞层约定](#碰撞层约定)
- [已知限制](#已知限制)
- [许可](#许可)

---

## 玩法机制

1. **瞄准与发射**：枪口始终朝向鼠标（已挂住锚点时改为朝向锚点）。按下鼠标左键发射射线，命中绿色锚点即建立绳索连接。
2. **钟摆摆动**：连接后每帧施加向心加速度与绳长约束力，角色围绕锚点做钟摆运动，可借惯性飞越空隙。
3. **断绳**：射线命中红色区域或地图瓦片时会自动断绳；也可用鼠标右键 / 空格主动剪断。
4. **检查点与陷阱**：进入检查点区域即记录重生位置；碰到红色陷阱（岩浆）会立即回到上一处检查点。
5. **脱困**：按 `F2` 尝试回到上一处检查点（见[已知限制](#已知限制)，该键当前未接线）。
6. **Boss 战**：关卡终点是一个多阶段 Boss。触碰 Boss 造成伤害，Boss 会随机瞬移并释放子弹与激光。

---

## 操作方式

| 操作 | 按键 | 输入动作 | 说明 |
| --- | --- | --- | --- |
| 向左移动 | `A` | `Left` | 地面水平左移 |
| 向右移动 | `D` | `Right` | 地面水平右移 |
| 跳跃 / 断绳 | `Space` | `Jump` | 起跳时先断绳；空中同样可用 |
| 发射 / 连接抓钩 | 鼠标左键 | `Fire` | 朝鼠标方向发射射线，命中锚点后连接 |
| 剪断绳索 | 鼠标右键 | `Line_Break` | 断开当前锚点连接 |
| 回到检查点 | `F2` | `Suicide` | 输入动作已定义，**当前事件名不匹配，尚未生效** |
| 绳长 + | 小键盘 `+` | `Line_Up` | 输入动作已定义，**当前 `Player2.cs` 未读取** |
| 绳长 − | 小键盘 `-` | `Line_Down` | 输入动作已定义，**当前 `Player2.cs` 未读取** |

> 键位定义见 `project.godot` 的 `[input]` 段。主场景 `main.tscn` 的 `Labels` 节点下布置了多块 `Label` 作为关卡引导。

---

## 快速开始

### 环境要求

- **Godot 4.8（.NET 版）**
- **.NET SDK 10**

### 编辑器运行

1. 用 Godot 4.8 (.NET) 打开 `project.godot`；
2. 等待 C# 项目自动构建完成；
3. 按 `F5` 运行，主场景为 `main.tscn`。

### 命令行构建

```bash
dotnet build NJUPTClubGame.sln
```

### 导出

仓库内置 **Windows Desktop (x86_64)** 导出预设，产物输出到：

```
binary/NJUPTClubGame.exe   # PCK 内嵌
```

---

## 项目结构

```
njupt-club-game/
├── project.godot              # 工程配置：主场景、输入映射、碰撞层命名（2~7）
├── NJUPTClubGame.csproj       # .NET 项目文件
├── NJUPTClubGame.csproj.old   # 旧版 csproj（PublishAot 实验配置，未启用）
├── NJUPTClubGame.sln          # .NET 解决方案
├── export_presets.cfg         # Windows Desktop 导出预设
├── new_label_settings.tres    # 关卡提示文字样式（font_size = 32）
├── icon.svg                   # 项目图标
├── LICENSE                    # GNU GPL v3.0
│
├── main.tscn                  # ★ 主场景：教程关卡 + 关卡终点 Boss 战
├── main_2.tscn                # 早期测试关卡（保留）
│
├── scripts/
│   ├── Player2.cs             # 玩家：瞄准、射线判定、锚点约束、击杀/重生、绘制
│   ├── Boss.cs                # Boss：多阶段状态机、子弹、激光
│   ├── RespawnPoint.cs        # 检查点：玩家进入即记录重生位置
│   └── CustomFSM.cs           # 通用异步有限状态机（含跨实例广播）
│
├── prefabs/
│   ├── player.tscn            # 玩家预制体（枪、抓钩射线、击杀检测区）
│   ├── anchor.tscn            # 绿色锚点（StaticBody2D）
│   ├── respawn_point.tscn     # 检查点预制体（Area2D）
│   └── boss_bullet.tscn       # Boss 子弹（RigidBody2D，零重力）
│
├── tilemap/
│   └── new_tile_set.tres      # TileSet 定义（64×64，黑白瓦片）
│
└── textures/
    ├── tex_white_tile.png     # 白色瓦片（无碰撞，装饰）
    ├── tex_black_tile.png     # 黑色瓦片（方形碰撞，实体平台）
    ├── tile_red_area.tres     # 红/粉区纹理
    ├── tile_kill_area.tres    # 击杀区纹理（半透明红，子弹复用）
    ├── tile_tips.tres         # 提示区纹理（浅蓝）
    └── boss.tres              # Boss 本体纹理（黄色）
```

> `binary/`、`.godot/`、`.vs/`、`.codegraph/`、`.omo/run-continuation` 均已 gitignore。

---

## 技术架构

| 维度 | 选型 | 说明 |
| --- | --- | --- |
| 引擎 | Godot 4.8 (dev) | `config/features = ("4.8", "C#", "Forward Plus")` |
| 语言 | C# / .NET 10 | `Godot.NET.Sdk/4.8.0-dev.3`，`TargetFramework=net10.0` |
| 玩家 | `CharacterBody2D` | 手工运动与约束，见 `scripts/Player2.cs` |
| Boss | `Area2D` | 多阶段状态机，见 `scripts/Boss.cs` |
| 状态管理 | 自研异步 FSM | `scripts/CustomFSM.cs`：`Task` + `CancellationToken` + `AsyncLocal`，支持跨实例 `BroadcastEvent` |
| 2D 物理 | Godot 内置 2D 物理 | 抓钩约束在 `_PhysicsProcess` 手工施加 |
| 碰撞检测 | `RayCast2D` + `Area2D` | 枪口射线判定锚点/陷阱；脚下 Area2D 判定击杀 |
| 渲染 | `_Draw()` | 绳索虚线/实线、Boss 激光均由 `DrawLine` 绘制 |
| 粒子 | `GPUParticles2D` | Boss 受击爆炸特效 |
| 地图 | `TileMapLayer` + 64×64 `TileSet` | 白色瓦片装饰、黑色瓦片实体碰撞 |
| 渲染后端 | Forward+ / Windows D3D12 | `rendering_device/driver.windows="d3d12"` |

> 说明：`project.godot` 声明了 3D 物理引擎 `Jolt Physics`，但当前玩法完全基于 2D 物理。

---

## 状态机设计

`CustomFSM` 以「每个状态 = 一个 `async` 方法」组织逻辑：`CancellationToken` 在切换时终止旧状态，`SendEvent` 触发转移或唤醒 `WaitForEvent`，用线性 `await` 表达强时序流程。

- `AddTransition` 注册的转移随状态切换被清空；
- `AddGlobalTransition` 注册的转移跨状态常驻（如玩家的 `PLAYER RESPAWN`）；
- `BroadcastEvent(ev)` 为静态方法，会把事件发送给**所有**已注册的 FSM 实例（Boss 与玩家共用同一事件名实现联动）；
- `_Notification(NotificationPredelete)` 会自动调用 `Destroy()` 从全局列表注销。

### 玩家状态（`Player2`）

```
State_Idle ──FIRE──▶ State_Fire ──FINISHED──▶ State_AnchorIdle
   ▲                                              │
   │                                              ├─LINE_BREAK──▶ State_AnchorLineBreak ──▶ State_Idle
   │                                              └─(TOUCH_NEW_ANCHOR 转移已注释)
   └──────────────── State_Respawn ◀── PLAYER RESPAWN（全局转移）
```

| 状态 | 职责 |
| --- | --- |
| `State_Idle` | 待机。`anchorDistance` 重置为 `MaxRaycastDistance`，注册 `FIRE` |
| `State_Fire` | 建立连接。优先当前帧 `raycast_anchor`，0.2s 内回退 `raycast_prev_anchor`；无锚点回 `Idle` |
| `State_AnchorIdle` | 已挂住锚点，等待 `LINE_BREAK`（换锚 / 冲刺转移当前被注释） |
| `State_AnchorLineBreak` | 清空 `CurrentAnchor`，回到 `Idle` |
| `State_Respawn` | 传送到最近检查点并清零速度（无检查点则直接返回） |
| `State_Dash` | 收绳冲刺（实现保留，转移被注释，当前不可达） |

### Boss 状态（`Boss`）

```
State_Init ──▶ State_FirstIdle ──HIT──▶ State_FirstHit ──▶ State_Hit
                                                             │
                             ┌───────────────────────────────┘
                             ▼
                        State_NextPhase ──NEXT X──▶ State_Idle
                             │                        │
                             └──WIN──▶ State_Win      ├─SHOOT──▶ State_Shoot ──▶ State_Idle
                                                      └─LASER──▶ State_LaserPrepare ──▶ State_Laser ──▶ State_Idle
   （任意状态）── PLAYER RESPAWN（全局）──▶ State_PlayerDie ──▶ State_Init
```

---

## 物理与判定

### 锚点约束（`Player2._PhysicsProcess`）

设 `offset = anchor.GlobalPosition - GlobalPosition`，`distance = |offset|`，`dir = offset.Normalized()`：

- **超出绳长**（`distance > anchorDistance`）：
  1. 追加向心加速度 `dir * (v_tangential² / distance)`，形成钟摆；
  2. 抵消向外的径向速度分量；
  3. 追加回弹力 `dir * (distance - anchorDistance) * LineDistanceForceFactor`。
- **短于绳长**（`distance < anchorDistance`）：追加同样的回弹力把角色推离锚点。

### 连接建立（`Attach`）

`anchorDistance = |offset| * AttachLengthFactor`，并钳制不小于 `MinLineDistance`。

### 运动与阻尼

- 地面：`Jump` 会先 `LINE_BREAK` 再起跳；水平速度用 `MoveToward` 平滑归零。
- 空中且已挂绳：允许跳跃（断绳 + 起跳，先抵消下坠速度再叠加跳跃冲量）。
- 每帧统一施加线性阻尼：`velocity *= (1 - LinerDamp * delta)`。

### 死亡与重生

- 玩家脚下挂着 `Kill Detect`（`Area2D`，`collision_mask = 32` 即第 6 层 Kill Area）。一旦有 `Area2D`/`Body2D` 进入，即调用 `CustomFSM.BroadcastEvent("PLAYER RESPAWN")`。
- 该广播同时通知玩家 FSM（`State_Respawn` 传送回检查点）与 Boss FSM（`State_PlayerDie` 重置 Boss）。
- `RespawnPoint`（`Area2D`）在 `BodyEntered` 收到 `Player2` 时调用 `SetRespawnPoint()` 记录检查点。

### 瞄准与命中（`Player2._Process`）

- 无锚点时枪口朝鼠标，否则朝当前锚点；`GunRayCast.TargetPosition = (anchorDistance, 0)`。
- 命中第 5 层（Green Area）→ 记录锚点并缓存 0.2s；命中第 4 层（Red Area）或 `TileMapLayer` → `LINE_BREAK`。

### 绘制（`Player2._Draw`）

- 未连接：从枪口到射线目标绘制虚线，未命中为 `LineMissing`(红)，命中锚点为 `LineCaught`(绿)；
- 已连接：绘制到锚点的实线 `LineJoint`(深绿)。

---

## Boss 战设计

`Boss` 是一个 `Area2D`（第 7 层 Boss，`collision_mask = 4` 检测玩家），通过 `BossPhase` 逐级变强。

| 机制 | 触发条件 | 行为 |
| --- | --- | --- |
| 受击 `HIT` | 玩家 `BodyEntered` 进入 Boss | 播放爆炸粒子，瞬移到随机 `Boss Points`（距离需 > √1000），0.3s 后进入下一阶段 |
| 子弹 `SHOOT` | `BossPhase ≥ 2`，Idle 中随机触发 | 等待 1~4s 后朝玩家方向生成子弹 |
| 激光 `LASER` | `BossPhase ≥ 5`，Idle 中随机触发 | 先 3s 瞄准预警（绿黄，持续跟随玩家），再 0.5s 蓄力（黄），随后开火（红）并生成高速子弹 |
| 地面陷阱 | `BossPhase ≥ 7` | 启用 `Boss Red Area`（第 6 层 Kill Area）覆盖地面 |
| 胜利 `WIN` | `BossPhase > 11` | 显示 `Boss Win` 面板，并广播 `PLAYER RESPAWN` 重置战斗 |

**子弹**（`prefabs/boss_bullet.tscn`）：`RigidBody2D`，`gravity_scale = 0`、`can_sleep = false`；生成时碰撞层为 0，短暂延迟后设为 `1 << 5`（第 6 层 Kill Area），从而被玩家的 `Kill Detect` 检测到并触发重生。

**激光**由 `Boss._Draw` 用 `DrawLine` 从 Boss 位置绘制到 `laserTarget`，颜色随状态在 `LaserPrepareColor` → `LaserReadyColor` → `LaserFireColor` 间切换。

---

## 关卡与预制体

### 主场景 `main.tscn`

| 节点 | 说明 |
| --- | --- |
| `map` | `TileMapLayer` 搭建的地形 |
| `Player` | `prefabs/player.tscn` 实例 |
| `Anchors` | 锚点容器：`Move Anchor`（含 `Anchor24` 与 `AnimationPlayer`，循环移动锚点）、`Anchor` ~ `Anchor49` |
| `RespawnPoints` | 检查点容器：`RespawnPoint` ~ `RespawnPoint13` |
| `Labels` | 引导文字 `Label` ~ `Label9` |
| `Boss` | Boss 本体（`Area2D`）+ `Sprite2D` + `Hit Exp` 粒子 |
| `Boss Points` | 9 个瞬移候选点 |
| `Boss Start Point` | Boss 初始位置 |
| `Boss Red Area` | 地面击杀区（第 6 层），阶段 7 后启用 |
| `Boss Win` | 隐藏的胜利面板（"你赢了 感谢游玩"） |

### 预制体

| 预制体 | 根节点 | 关键配置 |
| --- | --- | --- |
| `player.tscn` | `CharacterBody2D` | `collision_layer=7`、`collision_mask=3`；`Gun/RayCast2D`（mask=26）、`Kill Detect`（`Area2D`, mask=32）；`Camera2D` zoom=0.5 |
| `anchor.tscn` | `StaticBody2D` | `collision_layer=16`（第 5 层）、`collision_mask=0`；绿色方块 |
| `respawn_point.tscn` | `Area2D` | `collision_mask=4`（第 3 层 Player）；`Point` 子节点定义重生坐标 |
| `boss_bullet.tscn` | `RigidBody2D` | 零重力、初始无碰撞层，由脚本在延迟后设为第 6 层 |

---

## 可调参数

### 玩家（`Player2.cs` / `player.tscn`）

| 参数 | 脚本默认 | 预制体覆盖 | 含义 |
| --- | --- | --- | --- |
| `Speed` | `300.0` | — | 地面水平移动速度 |
| `JumpVelocity` | `-400.0` | — | 跳跃初速度（负值向上） |
| `LineDistanceForceFactor` | `1.0` | `0.2` | 绳长偏差的回弹力系数 |
| `AttachLengthFactor` | `0.75` | `0.6` | 连接时绳长 = 距离 × 该系数 |
| `MinLineDistance` | `10.0` | — | 最小绳长 |
| `MaxRaycastDistance` | `500.0` | `600.0` | 未连接时射线最大长度 |
| `LineWidth` | `2.0` | `8.0` | 线条宽度 |
| `LineDashWidth` | `2.0` | `5.0` | 虚线短划长度 |
| `LinerDamp` | `0.1` | `0.5` | 线性阻尼系数 |
| `LineMissing` / `LineCaught` / `LineJoint` | `Red` / `Green` / `DarkGreen` | — | 虚线未命中 / 虚线命中 / 实线连接 颜色 |

### Boss（`Boss.cs` / `main.tscn`）

| 参数 | 脚本默认 | 场景覆盖 | 含义 |
| --- | --- | --- | --- |
| `BossPhase` | `0` | — | 当前阶段（受击 +1） |
| `BulletVel` | `10.0` | `500.0` | 子弹冲量系数 |
| `LaserLineWidth` | `3.0` | — | 激光线宽 |
| `LaserPrepareColor` | `GreenYellow` | — | 瞄准预警色 |
| `LaserReadyColor` | `Yellow` | — | 蓄力色 |
| `LaserFireColor` | `Red` | — | 开火色 |

---

## 碰撞层约定

| 层号 | 名称 | 位值 | 用途 |
| --- | --- | --- | --- |
| 2 | 地图 | 2 | `TileMapLayer` 瓦片碰撞 |
| 3 | Player | 4 | 玩家 `CharacterBody2D` |
| 4 | Red Area | 8 | 陷阱/断线区，射线命中即断绳 |
| 5 | Green Area | 16 | 锚点，射线命中即连接 |
| 6 | Kill Area | 32 | 击杀区（陷阱 / Boss 子弹 / Boss 地面红区），玩家检测到即重生 |
| 7 | Boss | 64 | Boss 本体，玩家进入即造成伤害 |

- **玩家预制体**：`collision_layer = 7`，`collision_mask = 3`
- **击杀检测区**：`collision_layer = 0`，`collision_mask = 32`
- **枪口射线**：`collision_mask = 26`（= 2 + 8 + 16），`collide_with_areas = true`
- **锚点**：`collision_layer = 16`，`collision_mask = 0`
- **检查点**：`collision_mask = 4`

---

## 已知限制

- **`F2` 未接线**：`Player2._Process` 发送的是 `"RESPAWN"`，但 `_Ready` 注册的全局转移是 `"PLAYER RESPAWN"`，两者不匹配，按 `F2` 当前不会生效。
- **无检查点时无重生兜底**：`State_Respawn` 在 `respawnPoint == null` 时直接返回（不再重载场景）。
- **换锚与冲刺被禁用**：`State_AnchorIdle` 中的 `TOUCH_NEW_ANCHOR`、`FIRE → State_Dash` 转移均被注释。
- **绳长调整未接线**：`Line_Up` / `Line_Down`（小键盘 `+` / `-`）已在 `project.godot` 定义，但 `Player2.cs` 未读取。
- **绳索无形变**：连接态为直线绘制，遇到障碍物直接断线而非绕行。
- **Boss 子弹延迟设层**：子弹生成后经过一段延迟才被设为 Kill Area 层，延迟期间不会造成伤害。
- **残留文件**：`main_2.tscn`、`NJUPTClubGame.csproj.old` 与部分 `.png~` 备份仍保留在仓库中。

---

## 许可

本项目基于 [GNU General Public License v3.0](LICENSE) 发布。
