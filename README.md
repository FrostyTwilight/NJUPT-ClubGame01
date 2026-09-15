# NJUPTClubGame

# 重写中......

NJUPT 社团游戏项目。一个基于 Godot 4.8 + C# 的 2D 抓钩物理原型游戏。玩家操控角色发射抓钩，挂在绿色锚点上，利用钟摆摆动机制在平台间移动，同时需要避开红色断线区域。

## 核心玩法

游戏围绕「抓钩摆荡」这一核心机制展开:

1. **发射抓钩**。枪口朝鼠标方向射出射线检测，命中绿色锚点后自动连接，形成约束线。
2. **钟摆摆动**。连接锚点后，物理系统在每一帧施加向心加速度和回弹力，模拟钟摆运动。玩家可以利用摆动惯性飞越平台间的空隙。
3. **切换锚点**。射线检测到新的锚点时，自动切换连接目标，摆动中心随之转移。
4. **断线机制**。射线命中红色区域时，钩爪连接断开，角色恢复自由落体状态。右键也可以主动断线。
5. **地面移动与跳跃**。未连接锚点时，角色在地面上通过 A/D 水平移动，空格键跳跃。

## 操作说明

| 操作 | 按键 | 说明 |
|------|------|------|
| 向左移动 | `A` | 地面水平左移 |
| 向右移动 | `D` | 地面水平右移 |
| 跳跃 | `Space` | 地面起跳 |
| 发射/连接抓钩 | 鼠标左键 | 先断开当前连接，再向鼠标方向发射射线，命中锚点后连接 |
| 主动断线 | 鼠标右键 | 断开当前锚点连接 |
| 调整绳长 (增加) | 小键盘 `+` | 已定义输入动作，当前 Player2.cs 未读取，暂未生效 |
| 调整绳长 (减少) | 小键盘 `-` | 已定义输入动作，当前 Player2.cs 未读取，暂未生效 |

## 技术选型

| 类别 | 选择 |
|------|------|
| 引擎 | Godot 4.8 (dev) |
| 语言 | C# (.NET 10, Godot.NET.Sdk 4.8.0-dev.3) |
| 渲染 | Forward Plus, Windows D3D12 |
| 2D 物理 | Godot 内置 2D 物理引擎 |
| 状态机 | 自定义异步状态机 (`CustomFSM`) |
| 导出目标 | Windows Desktop x86_64 |

> 注: 项目声明了 Jolt Physics 3D 物理引擎 (`project.godot` 中 `3d/physics_engine="Jolt Physics"`)，但实际游戏玩法仅使用 Godot 内置 2D 物理。

## 项目结构

```
NJUPTClubGame/
├── project.godot              # Godot 项目配置
├── NJUPTClubGame.csproj       # .NET 项目文件
├── NJUPTClubGame.sln          # .NET 解决方案
├── export_presets.cfg         # 导出预设
├── LICENSE                    # GNU GPL v3.0
├── main_2.tscn                # 当前主场景 (地图、玩家、锚点、断线区)
├── main.tscn                  # 旧版原型场景 (已弃用，仅保留存档)
├── icon.svg                   # 项目图标
│
├── scripts/
│   ├── Player2.cs             # 玩家脚本: 移动、射线检测、锚点约束、状态机
│   └── CustomFSM.cs           # 自定义异步有限状态机引擎
│
├── prefabs/
│   └── anchor.tscn            # 锚点预制体 (StaticBody2D, 碰撞层 5)
│
├── tilemap/
│   ├── new_tile_set.tres      # TileSet 定义 (64x64, 黑白瓦片)
│   └── tile_01.tscn           # 瓦片地图场景
│
├── textures/
│   ├── tex_white_tile.png     # 白色瓦片纹理 (无碰撞)
│   ├── tex_black_tile.png     # 黑色瓦片纹理 (带碰撞)
│   ├── gun.png                # 枪口纹理
│   ├── hook.png               # 钩爪纹理
│   └── player.png             # 玩家纹理
│
└── binary/                    # 导出输出目录 (已 gitignore)
```

## 状态机说明

玩家状态由 `CustomFSM` 管理，基于 `async/await` + `CancellationToken` 实现。每个状态是一个异步方法，通过 `AddTransition` 注册事件触发的状态转换，通过 `SendEvent` 触发转换。

### 状态流转

```
State_Idle
  │
  │ [FIRE]  ← 鼠标左键触发
  ▼
State_Fire
  │
  │ [FINISHED]  ← 异步任务完成时自动发送
  ▼
State_AnchorIdle ─────────────────────┐
  │                                   │
  │ [TOUCH_NEW_ANCHOR]                │ [LINE_BREAK]
  ▼                                   ▼
State_TouchNewArchor          State_AnchorLineBreak
  │                                   │
  │ [FINISHED]                        │ 清空 CurrentAnchor
  ▼                                   ▼
State_AnchorIdle ───────────────→ State_Idle
  │                                   ▲
  │ [LINE_BREAK]                      │
  └───────────────────────────────────┘
```

### 各状态职责

| 状态 | 职责 |
|------|------|
| `State_Idle` | 待机状态，设置 `anchorDistance = MaxRaycastDistance`，等待 FIRE 事件 |
| `State_Fire` | 执行连接，优先使用当前帧的 `raycast_anchor`，0.2 秒内回退到上一帧的 `raycast_prev_anchor`，无可用锚点则回到 Idle |
| `State_AnchorIdle` | 已连接锚点，等待 `LINE_BREAK` 或 `TOUCH_NEW_ANCHOR` 事件 |
| `State_TouchNewArchor` | 检测到新锚点，切换连接后回到 `State_AnchorIdle` |
| `State_AnchorLineBreak` | 清空 `CurrentAnchor`，立即转回 `State_Idle` |

### FSM 引擎关键接口

`CustomFSM` 提供以下方法:

- `SwitchToState(state)` 切换到新状态，自动终止当前状态
- `AddTransition(event, state)` 注册事件到目标状态的转换
- `SendEvent(event)` 发送事件，触发转换或唤醒 `WaitForEvent`
- `Update(delta)` 每帧调用，重置切换计数并广播 `EVENT_UPDATE`
- `WaitForEvent(event)` 异步等待指定事件
- `NextFrame()` 等待下一帧 (等价于 `WaitForEvent("UPDATE")`)

预定义常量: `EVENT_UPDATE = "UPDATE"`, `EVENT_FINISHED = "FINISHED"`。

## 关键可调参数

以下参数通过 `[Export]` 暴露，可在 Godot 编辑器 Inspector 面板调整:

| 参数 | 脚本默认值 | 场景覆盖值 (main_2.tscn) | 说明 |
|------|-----------|-------------------------|------|
| `Speed` | 300.0 | (未覆盖) | 地面水平移动速度 |
| `JumpVelocity` | -400.0 | (未覆盖) | 跳跃初始速度 (负值 = 向上) |
| `LineDistanceForceFactor` | 1.0 | **0.2** | 绳索距离修正力系数 (连接后根据距离差施加的回弹力) |
| `AdditionalForceFactor` | 10.0 | **0.5** | 附加力系数 (当前代码中 `additionVel` 赋值已注释，未生效) |
| `MinLineDistance` | 10 | (未覆盖) | 绳索最小距离 (连接时 `anchorDistance` 不低于此值) |
| `MaxRaycastDistance` | 500.0 | **600.0** | 未连接时射线检测最大距离 |
| `LineWidth` | 2.0 | **8.0** | 绘制线条宽度 |
| `LineDashWidth` | 2.0 | **5.0** | 虚线段长度 (仅在未连接状态下使用) |
| `LineMissing` | Red | (未覆盖) | 射线未命中任何目标时的线条颜色 |
| `LineCaught` | Green | (未覆盖) | 射线命中锚点时的虚线颜色 |
| `LineJoint` | DarkGreen | (未覆盖) | 已连接锚点时的实线颜色 |

Camera2D 缩放: `Vector2(0.5, 0.5)`

## 碰撞层配置

| 层编号 | 名称 | 用途 |
|--------|------|------|
| Layer 2 | 地图 | TileMapLayer 地图瓦片碰撞层 |
| Layer 3 | Player | 玩家 CharacterBody2D 碰撞层 |
| Layer 4 | Red Area | 断线区域 (Area2D)，射线命中即断线 |
| Layer 5 | Green Area | 锚点区域 (StaticBody2D)，射线命中即连接 |

玩家节点: `collision_layer=3`, `collision_mask=3` (与地图和自身交互)。
锚点预制体: `collision_layer=16` (第 5 层), `collision_mask=0` (不检测其他碰撞)。

## 瓦片地图

`tilemap/new_tile_set.tres` 定义了 64x64 像素的 TileSet:

- **白色瓦片** (`tex_white_tile.png`): 无碰撞多边形，仅作为视觉装饰
- **黑色瓦片** (`tex_black_tile.png`): 带方形碰撞多边形 (`-32,-32` 到 `32,32`)，作为实体平台
- 物理层 0: `collision_layer=3`, `collision_mask=3` (与地图和玩家交互)

## 运行方式

### 环境要求

- Godot 4.8 (.NET 版)
- .NET SDK 10

### 编辑器运行

1. 用 Godot 4.8 (.NET 版) 打开 `project.godot`
2. 等待 C# 项目自动构建完成
3. 按 `F5` 运行，主场景为 `main_2.tscn`

### 命令行构建

```bash
dotnet build NJUPTClubGame.sln
```

### 导出

1. 在 Godot 编辑器中打开 `项目 → 导出`
2. 选择预设 `Windows Desktop` (x86_64)
3. 点击 `导出项目`
4. 输出路径: `binary/NJUPTClubGame.exe` (PCK 内嵌)

## 已知限制 / 可改进方向

- **绳长调整未实现**。`project.godot` 中定义了 `Line_Up`/`Line_Down` 输入动作 (小键盘 +/-)，但 `Player2.cs` 中未读取这两个输入，绳索长度无法通过按键调整。
- **附加力未启用**。`AdditionalForceFactor` 参数和 `additionVel` 变量已声明，但 `Attach()` 中的赋值代码 (`additionVel = offset.Normalized() * ...`) 被注释掉了，该功能未生效。
- **绳索渲染为直线**。连接状态下使用 `DrawLine` 绘制直线，没有绳索变形、弯曲或绕过障碍物的物理模拟。障碍物会直接触发断线而非绳索缠绕。
- **旧版原型并存**。`main.tscn` 是早期原型场景 (包含内嵌的 `PlayerController`/`Gun`/`Hook` 脚本)，已不作为主场景使用，但仍在仓库中保留。
- **射线检测为逐帧强制更新**。`GunRayCast.ForceRaycastUpdate()` 每帧执行，未使用异步射线检测，可能在高密度碰撞体场景下影响性能。

## 许可

本项目基于 [GNU General Public License v3.0](LICENSE) 发布。
