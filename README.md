# NJUPTClubGame

# 重写中......

> 基于 **Godot 4.8 + C# (.NET 10)** 的 2D 抓钩（Grappling Hook）物理原型游戏。
> 玩家发射抓钩挂住绿色锚点，借助钟摆摆动在平台间穿梭，并需要避开会剪断绳索的红色区域。

---

## 目录

- [玩法机制](#玩法机制)
- [操作方式](#操作方式)
- [快速开始](#快速开始)
- [技术架构](#技术架构)
- [目录结构](#目录结构)
- [状态机设计](#状态机设计)
- [物理实现](#物理实现)
- [可调参数](#可调参数)
- [碰撞层约定](#碰撞层约定)
- [已知限制](#已知限制)
- [许可](#许可)

---

## 玩法机制

游戏围绕「抓钩摆荡」这一核心机制展开：

1. **瞄准与发射**：枪口始终朝向鼠标（已挂住锚点时改为朝向锚点）。按下左键发射射线，命中锚点即建立连接。
2. **钟摆摆动**：连接锚点后，每帧施加向心加速度与绳长约束力，角色围绕锚点做钟摆运动，可借惯性飞越空隙。
3. **自动换锚**：射线检测到新的锚点时自动切换连接目标，摆动中心随之转移。
4. **断线**：射线扫到红色区域会立即断线；也可用右键主动断线，恢复自由落体。
5. **地面移动**：未连接锚点时，角色可在地面用 `A` / `D` 移动、空格跳跃。

---

## 操作方式

| 操作 | 按键 | 输入动作 | 说明 |
| --- | --- | --- | --- |
| 向左移动 | `A` | `Left` | 地面水平左移 |
| 向右移动 | `D` | `Right` | 地面水平右移 |
| 跳跃 | `Space` | `Jump` | 起跳（`JumpVelocity` 为负值，Y 轴向上） |
| 发射 / 连接抓钩 | 鼠标左键 | `Fire` | 先断线，再朝鼠标方向发射射线并尝试连接锚点 |
| 主动断线 | 鼠标右键 | `Line_Break` | 断开当前锚点连接 |
| 绳长 + | 小键盘 `+` | `Line_Up` | 输入动作已定义，**当前 `Player2.cs` 未读取** |
| 绳长 − | 小键盘 `-` | `Line_Down` | 输入动作已定义，**当前 `Player2.cs` 未读取** |

> 键位定义见 `project.godot` 的 `[input]` 段。场景 `main_2.tscn` 中另有一块 `Label` 节点提供实时操作提示。

---

## 快速开始

### 环境要求

- **Godot 4.8（.NET 版）**
- **.NET SDK 10**

### 编辑器运行

1. 用 Godot 4.8 (.NET) 打开 `project.godot`；
2. 等待 C# 项目自动构建完成；
3. 按 `F5` 运行，主场景为 `main_2.tscn`。

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

## 技术架构

| 维度 | 选型 | 说明 |
| --- | --- | --- |
| 引擎 | Godot 4.8 (dev) | `config/features = ("4.8", "C#", "Forward Plus")` |
| 语言 | C# / .NET 10 | `Godot.NET.Sdk/4.8.0-dev.3`，`TargetFramework=net10.0` |
| 玩家 | `CharacterBody2D` | 自研运动与约束，见 `scripts/Player2.cs` |
| 状态管理 | 自研异步 FSM | `scripts/CustomFSM.cs`，`Task` + `CancellationToken` + `AsyncLocal` |
| 2D 物理 | Godot 内置 2D 物理 | 抓钩约束在 `_PhysicsProcess` 中手工施加 |
| 碰撞检测 | `RayCast2D` | `Gun/RayCast2D` 判定锚点 / 断线区，逐帧 `ForceRaycastUpdate()` |
| 绳索渲染 | `_Draw()` + `DrawLine` / `DrawDashedLine` | 未连接时虚线预览，连接后实线 |
| 地图 | `TileMapLayer` + 64×64 `TileSet` | 白色瓦片装饰、黑色瓦片实体碰撞 |
| 渲染后端 | Forward+ / Windows D3D12 | `rendering_device/driver.windows="d3d12"` |

> 说明：`project.godot` 声明了 3D 物理引擎 `Jolt Physics`，但当前玩法完全基于 2D 物理。

---

## 目录结构

```
njupt-club-game/
├── project.godot            # Godot 工程配置（输入映射、碰撞层命名、主场景）
├── NJUPTClubGame.csproj     # .NET 项目文件
├── NJUPTClubGame.sln        # .NET 解决方案
├── export_presets.cfg       # Windows Desktop 导出预设
├── icon.svg                 # 项目图标
├── LICENSE                  # GNU GPL v3.0
│
├── main_2.tscn              # 当前主场景：地图 / 玩家 / 锚点 / 红区
├── main.tscn                # 早期原型场景（已弃用，仅存档）
│
├── scripts/
│   ├── Player2.cs           # 玩家：瞄准、射线检测、锚点约束、绘制、状态机
│   └── CustomFSM.cs         # 通用异步有限状态机
│
├── prefabs/
│   └── anchor.tscn          # 锚点预制体（StaticBody2D，碰撞层 5）
│
├── tilemap/
│   ├── new_tile_set.tres    # TileSet 定义（64×64，黑白瓦片）
│   └── tile_01.tscn         # 瓦片场景
│
└── textures/
    ├── tex_white_tile.png   # 白色瓦片（无碰撞，装饰）
    ├── tex_black_tile.png   # 黑色瓦片（方形碰撞，实体平台）
    ├── gun.png              # 枪械纹理
    ├── hook.png             # 钩爪纹理
    └── player.png           # 玩家纹理
```

> `binary/`、`.godot/`、`.vs/`、`.codegraph/`、`.omo/run-continuation` 均已 gitignore。

---

## 状态机设计

玩家逻辑由 `CustomFSM` 驱动：每个状态是一个 `async` 方法，用 `CancellationToken` 在切换时终止旧状态，用 `SendEvent` 触发转移，从而以线性 `await` 代码表达强时序流程，避免回调地狱。

### 状态流转

```
State_Idle
   │  FIRE（鼠标左键）
   ▼
State_Fire
   │  FINISHED（异步任务完成自动触发）
   ▼
State_AnchorIdle ── LINE_BREAK ──▶ State_AnchorLineBreak ──▶ State_Idle
   │
   └── TOUCH_NEW_ANCHOR ──▶ State_TouchNewArchor ── FINISHED ──▶ State_AnchorIdle
```

### 状态职责

| 状态 | 职责 |
| --- | --- |
| `State_Idle` | 待机。将 `anchorDistance` 重置为 `MaxRaycastDistance`，注册 `FIRE` 转移 |
| `State_Fire` | 建立连接。优先用当前帧 `raycast_anchor`，0.2s 内回退到 `raycast_prev_anchor`；无锚点则回到 `Idle` |
| `State_AnchorIdle` | 已挂住锚点。等待 `LINE_BREAK` 或 `TOUCH_NEW_ANCHOR` |
| `State_TouchNewArchor` | 检测到新锚点，`Attach()` 切换连接后回到 `AnchorIdle` |
| `State_AnchorLineBreak` | 清空 `CurrentAnchor`，回到 `Idle` |

### `CustomFSM` 接口

| 方法 | 作用 |
| --- | --- |
| `SwitchToState(state)` | 切换到新状态并终止当前状态 |
| `AddTransition(event, state)` | 注册事件 → 目标状态的转移 |
| `AddGlobalTransition(event, state)` | 注册全局转移（任意状态均可触发） |
| `SendEvent(event)` | 触发转移，或唤醒等待该事件的 `WaitForEvent` |
| `Update(delta)` | 每帧调用，重置切换计数并广播 `EVENT_UPDATE` |
| `WaitForEvent(event)` | 异步等待指定事件 |
| `NextFrame()` | 等待下一帧（等价 `WaitForEvent("UPDATE")`） |

预定义常量：`EVENT_UPDATE = "UPDATE"`、`EVENT_FINISHED = "FINISHED"`。

---

## 物理实现

### 锚点约束（`Player2._PhysicsProcess`）

设 `offset = anchor.GlobalPosition - GlobalPosition`，`distance = |offset|`，`dir = offset.Normalized()`：

- **超出绳长**（`distance > anchorDistance`）时：
  1. 追加向心加速度 `dir * (v_tangential² / distance)`，产生钟摆效果；
  2. 若径向速度为负（远离锚点），将其抵消；
  3. 追加回弹力 `dir * (distance - anchorDistance) * LineDistanceForceFactor`。
- **短于绳长**（`distance < anchorDistance`）时：同样追加回弹力把角色推离锚点。

### 连接建立（`Attach`）

`anchorDistance = |offset| * 0.75`，并钳制不小于 `MinLineDistance`。

### 瞄准与命中（`Player2._Process`）

- `CanShootHook`（无锚点）时枪口朝鼠标，否则朝当前锚点；
- `GunRayCast.TargetPosition = (anchorDistance, 0)` 并 `ForceRaycastUpdate()`；
- 命中碰撞层 5（Green Area）→ 记录 `raycast_anchor` 并缓存 0.2s；命中碰撞层 4（Red Area）→ 发送 `LINE_BREAK`。

### 绘制（`Player2._Draw`）

- 未连接：从枪口到射线目标点绘制虚线，未命中为 `LineMissing`(红)，命中锚点为 `LineCaught`(绿)；
- 已连接：绘制到锚点的实线 `LineJoint`(深绿)。

---

## 可调参数

以下属性通过 `[Export]` 暴露，可在 Godot Inspector 中调整（脚本默认值 vs 场景覆盖值）：

| 参数 | 脚本默认 | `main_2.tscn` 覆盖 | 含义 |
| --- | --- | --- | --- |
| `Speed` | `300.0` | — | 地面水平移动速度 |
| `JumpVelocity` | `-400.0` | — | 跳跃初速度（负值向上） |
| `LineDistanceForceFactor` | `1.0` | `0.2` | 绳长偏差的回弹力系数 |
| `AdditionalForceFactor` | `10.0` | `0.5` | 附加力系数（当前未生效，见已知限制） |
| `MinLineDistance` | `10.0` | — | 连接时的最小绳长 |
| `MaxRaycastDistance` | `500.0` | `600.0` | 未连接时的射线最大长度 |
| `LineWidth` | `2.0` | `8.0` | 线条宽度 |
| `LineDashWidth` | `2.0` | `5.0` | 虚线短划长度 |
| `LineMissing` | `Red` | — | 射线未命中颜色 |
| `LineCaught` | `Green` | — | 射线命中锚点颜色 |
| `LineJoint` | `DarkGreen` | — | 已连接线条颜色 |

其他：`Player/Camera2D` 缩放为 `Vector2(0.5, 0.5)`。

---

## 碰撞层约定

| 层号 | 名称 | 位值 | 用途 |
| --- | --- | --- | --- |
| 2 | 地图 | 2 | `TileMapLayer` 瓦片碰撞 |
| 3 | Player | 4 | 玩家 `CharacterBody2D` |
| 4 | Red Area | 8 | 断线区域（`Area2D`），射线命中即断线 |
| 5 | Green Area | 16 | 锚点区域（`StaticBody2D`），射线命中即连接 |

- **玩家节点**：`collision_layer = 3`，`collision_mask = 3`
- **锚点预制体**：`collision_layer = 16`，`collision_mask = 0`
- **枪口射线**：`collision_mask = 26`（= 2 + 8 + 16，即可命中地图 / 红区 / 绿区），`collide_with_areas = true`
- **TileSet 物理层 0**：`collision_layer = 3`，`collision_mask = 3`

---

## 已知限制

- **绳长调整未接线**：`Line_Up` / `Line_Down`（小键盘 `+` / `-`）已在 `project.godot` 定义，但 `Player2.cs` 未读取，无法通过按键收放绳。
- **附加力未启用**：`AdditionalForceFactor` 与 `additionVel` 已声明，但 `Attach()` 中的赋值被注释，功能未生效。
- **绳索无形变**：连接态为直线绘制，不做绳索弯曲 / 缠绕；遇到障碍物直接断线而非绕行。
- **射线逐帧强制更新**：`GunRayCast.ForceRaycastUpdate()` 每帧执行，高密度碰撞体下可能有性能开销。
- **旧原型并存**：`main.tscn` 为早期原型（内嵌 `PlayerController` / `Gun` / `Hook` / `Rope` 脚本），已非主场景，仅保留存档。

---

## 许可

本项目基于 [GNU General Public License v3.0](LICENSE) 发布。
