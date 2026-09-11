# NJUPTClubGame · 钩爪物理系统

一个基于 **Godot 4.8 + C# (.NET 10)** 的 2D 物理游戏原型，核心玩法是**钩爪/抓钩（Grappling Hook）**：向墙壁发射钩爪、把角色拉向锚点、围绕锚点摆荡，并支持动态收放绳与断绳。

> 本仓库为 NJUPT 社团游戏项目，重点展示「钩爪物理系统」这一进阶玩法机制的实现。

---

## 一、项目简介

玩家是一个受重力影响的 `RigidBody2D` 角色，可以在 2D 瓦片地图（TileMap）场景中左右移动、跳跃，并使用一把「钩爪枪」：

- 向鼠标所指方向发射钩爪，射线命中墙壁/地面后在命中点生成锚点与绳索；
- 绳索对角色施加**指向锚点的约束力**，把角色拉向锚点；
- 绳索绷紧后角色会围绕锚点产生**钟摆式摆荡**；
- 可随时用按键**断开绳索**恢复自由状态。

整个钩爪的状态流转由一套自研的 **async/await 有限状态机（FSM）** 驱动，物理约束在 `_IntegrateForces` 中完成，保证与物理步长同步。

### 核心文件

| 文件 | 职责 |
| --- | --- |
| `scripts/PlayerController.cs` | 玩家角色控制、钩爪受力/约束、断绳判定、FSM 状态定义 |
| `scripts/Gun.cs` | 钩爪枪：朝向鼠标、射线检测命中、发射/隐藏钩爪 |
| `scripts/Hook.cs` | 钩爪节点：用 `Line2D` 绘制绳索 |
| `scripts/CustomFSM.cs` | 通用异步状态机（`Task` + `CancellationToken` + `AsyncLocal`） |
| `main.tscn` | 主场景：TileMap、Player、Gun、Hook、Line2D、UI 提示 |

---

## 二、操作说明

| 操作 | 按键 | 对应输入动作 | 说明 |
| --- | --- | --- | --- |
| 左右移动 | `A` / `D` | `Left` / `Right` | 仅在 `CanMove` 为真时生效 |
| 跳跃 | `空格` | `Jump` | 跳跃时保持水平速度，钩挂状态下同样可跳 |
| 瞄准 / 发射钩爪 | `鼠标左键` | `Fire` | 空手时发射；已钩挂时再次按下＝**收绳牵引**（把绳长设为目标 0，快速拉近锚点） |
| 断开钩爪 | `鼠标右键` | `Line_Break` | 立即剪断绳索，恢复自由状态 |
| 放长绳索 | `小键盘 +` | `Line_Up` | 增大绳长，可荡得更远 |
| 收短绳索 | `小键盘 -` | `Line_Down` | 减小绳长，向锚点靠拢 |

> 键位定义见 `project.godot` 的 `[input]` 段；场景内也有一块 `Label` 实时提示操作。

### 玩法循环（FSM 状态）

```
Idle ──FIRE──▶ Fire ──(命中/未命中)──▶ HookIdle ──FIRE──▶ Drag
  ▲                │                        │  │
  │                │                        │  └─JUMP──▶ HookJump ──▶ HookIdle
  └────────────────┘                        └─LINE_BREAK──▶ Break ──▶ Idle
```

- **Idle**：自由移动，可发射、可跳跃。
- **Fire**：锁定移动，`Gun.Fire()` 做射线检测；命中则钩爪在 0.1s 内飞向命中点（Tween），未命中直接回到 Idle。
- **HookIdle**：钩爪已挂住，可移动、可跳跃，可再次按左键进入牵引。
- **Drag**：把目标绳长设为 0，持续把玩家拉向锚点。
- **Break**：隐藏钩爪、清零受力、恢复自由。

---

## 三、技术选型

| 维度 | 选型 | 说明 |
| --- | --- | --- |
| 引擎 | **Godot 4.8 (dev)** | `project.godot` 声明 `config/features = ("4.8", "C#", "Forward Plus")` |
| 语言 | **C# / .NET 10** | `NJUPTClubGame.csproj`：`Godot.NET.Sdk/4.8.0-dev.3`，`TargetFramework=net10.0` |
| 物理 | **Godot 2D 物理 + RigidBody2D** | 玩家为 `RigidBody2D`（`lock_rotation=true`），钩爪约束在 `_IntegrateForces` 中施加 |
| 状态管理 | **自研 async/await FSM** | `CustomFSM`：每个状态是一个 `Task`，用 `CancellationToken` 取消旧状态，`SendEvent` 触发转移，避免回调地狱 |
| 射线检测 | `RayCast2D` | 枪口 `RayCast2D` 判定命中点；玩家身上 `Move Ray Cast` 判定绳索是否被障碍物遮挡 |
| 渲染 | `Line2D` 绘制绳索、`TileMapLayer` 搭建关卡 | 绳索每帧由 `Hook._Process` 重算端点 |
| 动画 | `Tween` | 钩爪飞行使用 `TweenProperty`（Out/Quad，0.1s） |
| 渲染后端 | Forward+ / Windows 下 D3D12 | `rendering_device/driver.windows="d3d12"` |
| 导出目标 | Windows Desktop x86_64（内嵌 PCK） | `export_presets.cfg` |

**为什么用自研 FSM 而不是 Godot 的 `AnimationTree`/状态机节点？**
钩爪的逻辑是「发射 → 等待飞行完成 → 挂住 → 持续受力」这种强时序流程，用 `async` 状态函数写成线性的 `await` 代码，比节点连线更直观；`CancellationToken` 保证状态切换时旧逻辑立即终止，不会残留。

---

## 四、进阶挑战完成情况

### ✅ 门槛要求（全部完成）

1. **发射与命中**：`Gun.Fire()` 对 `RayCast2D` 调用 `ForceRaycastUpdate()`，命中后取 `GetCollisionPoint()` 作为锚点，钩爪飞向该点并显示绳索（`Hook.Visible = true`）。瞄准方向由 `Gun._Process` 用鼠标屏幕坐标反算世界坐标后 `Atan2` 得到。
2. **拉力**：`PlayerController._IntegrateForces` 计算 `offset = 玩家位置 − 锚点位置`；当 `|offset| > hook_length` 时，沿 `-offset` 方向把玩家位置按 `(distance − hook_length) × LineForceFactor` 修正，实现向锚点的持续牵引。
3. **断开**：鼠标右键触发 `LINE_BREAK`，进入 `Break` 状态调用 `Gun.HideHook()` 并清零钩挂标记，恢复自由；绷紧超过阈值时也会自动断绳。

### 进阶挑战

| 进阶项 | 状态 | 实现方式 |
| --- | --- | --- |
| 以锚点为圆心的**摆动物理** | ✅ 已实现 | 纯矢量约束：仅对「径向超出绳长」的分量做位置修正（`transform.Origin -= offset.Normalized() * (distance - hook_length) * LineForceFactor`），**保留切向速度**，因此角色自然形成以锚点为圆心、绳长为半径的钟摆式摆荡。 |
| **收绳 / 放绳**按键控制 | ✅ 已实现 | `Line_Up` / `Line_Down`（小键盘 `+`/`-`）在 `_Process` 中实时增减 `hook_length`（`LineLenModifierFactor = 10`），并做非负钳制；绳长越短拉力越强。 |
| 绳索与**障碍物的碰撞检测** | ✅ 已实现（表现为「断开」） | 当绳长被拉到 `LineBreakLimit` 以上时，玩家身上的 `Move Ray Cast` 朝锚点方向做一次射线检测；若被障碍物遮挡（`IsColliding()`），则触发 `LINE_BREAK` 断绳，并把角色吸附到碰撞点，避免穿墙。 |
| 钩爪射出时的**轨迹预览线** | ❌ 未实现 | 当前只有「枪身指向鼠标」的瞄准方向指示，以及用于判定命中的 `RayCast2D`；尚无发射前的飞行路径预览（代码中无 `_Draw` / `DrawLine` / `QueueRedraw`）。 |

> 说明：进阶 1 采用的是**矢量计算**而非物理关节（`PinJoint2D`/`DampedSpringJoint2D`），进阶 3 的行为是「被遮挡即断绳」而非「绳索弯曲」。

### 关键可调参数（`main.tscn` 中 Player 节点的导出属性）

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| `Speed` | `400` | 水平移动速度 |
| `JumpHight` | `-600` | 跳跃初速度（Y 轴向上为负） |
| `LineForceFactor` | `0.35` | 绳长超出时的径向修正强度（拉力手感） |
| `LineLenModifierFactor` | `10` | 每帧收/放绳的长度增量 |
| `LineBreakLimit` | `20` | 超过绳长多少后开始检测遮挡并断绳 |

---

## 五、运行方式

### 环境要求

- **Godot Engine 4.8（.NET 版）**
- **.NET SDK 10**（或与 `Godot.NET.Sdk/4.8.0-dev.3` 兼容的版本）

### 从编辑器运行

1. 用 Godot 4.8 (.NET) 打开本目录（选择 `project.godot`）；
2. 等待 C# 项目构建完成；
3. 按 `F5` 运行主场景 `main.tscn`。

### 命令行构建

```bash
dotnet build NJUPTClubGame.sln
```

### 导出

仓库已内置 Windows Desktop 导出预设，导出产物输出到 `binary/NJUPTClubGame.exe`。

---

## 六、已知限制 / 可改进方向

- **轨迹预览线缺失**：可在 `Gun` 中用 `_Draw()` + `DrawDashedLine()` 绘制从枪口到 `RayCast2D.GetCollisionPoint()` 的虚线，实现发射前路径预览。
- **绳索不做形变**：目前绳索是两点直线（`Line2D`），被遮挡时直接断开，未实现绳索在障碍物上的弯曲/缠绕。
- **绳长由位置约束模拟**：未使用 `PinJoint2D` 等物理关节，极端参数下可能出现轻微穿透或抖动。
- `Gun.HookVel` 导出字段目前未被使用（钩爪飞行由固定 0.1s 的 Tween 完成）。

---

## 许可

见 [LICENSE](LICENSE)。
