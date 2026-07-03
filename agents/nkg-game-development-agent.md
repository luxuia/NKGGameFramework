---
name: nkg-game-development-agent
description: 指导 AI agent 使用 NKGGameFramework 开发游戏和玩法功能。
---

# NKGGameFramework 游戏开发 Agent

你是在 NKGGameFramework 仓库内工作的 AI agent。开发游戏功能时必须贴合框架原生风格：整体游戏流程用 Procedure，局内模拟用 ECS，引擎差异通过 Adapter 接入，调试工具通过 Diagnostics/Hosting 接入。

## 优先阅读

开始实现前，先阅读任务相关代码，并查看这些项目文档：

1. `README.md`
2. `docs/ai-game-development-agent.md`
3. `.codex/skills/nkg-game-development/references/framework-patterns.md`

只有宿主工具支持 Codex 风格 skill 时，才把 `.codex/skills/nkg-game-development/SKILL.md` 作为可触发 skill 使用。`agents/nkg-game-development-agent.md` 是跨工具主入口。

## 架构硬约束

- `src/NKGGameFramework` 必须保持引擎无关。不要把 Unity、Godot、ASP.NET、React、编辑器 API、具体资源管线或 UI 控件依赖加入主包。
- 外部宿主只驱动 `RuntimeContext.Update`。`World` / `Scene` 应由 runtime module 在同一帧内推进。
- 整体游戏流程使用 `ProcedureModule`：启动、登录、配置/资源加载、大厅/菜单、房间准备、进入/离开局内、结算、保存、关闭。
- 局内或单局模拟使用 ECS：实体、组件、系统、移动、生成、战斗、碰撞、技能、Buff、行为树、分数、局内状态。
- 引擎对象、节点句柄、特效/音频/UI 命令、宿主资源放在 Adapter、宿主或 sample。核心组件只保存可序列化值、ID、handle、ref 和 tag。
- 优先使用已有 manager 和 registry：`SkillManager`、`BuffManager`、`BehaviorActionRegistry`、`SkillEffectRegistry`、`BuffEffectRegistry`、`GameplayTagRegistry`。
- 不要为新的调试 UI 功能复制调试捕获链路。使用 capture profile 和 `GameDebugFrameCapturePipeline`。

## 任务落点

- Core loop、Procedure、FSM、事件、Timer、池化：`src/NKGGameFramework/Core`
- ECS 数据、查询、系统、CommandBuffer、DebugView：`src/NKGGameFramework/Ecs`
- GameplayTag、Skill、Buff、BehaviorTree：`src/NKGGameFramework/Gameplay`
- 节点图数据、端口、撤销重做、校验：`src/NKGGameFramework/Nodes`
- 引擎无关 Asset、Scene、Audio、UI、Config、Localization、MVVM 契约：`src/NKGGameFramework/Runtime`
- Odin 序列化封装和契约：`src/NKGGameFramework/Serialization`
- Snapshot、mutation、dump、回放、分析：`src/NKGGameFramework.Diagnostics`
- 本地 loopback HTTP/SSE Debug Host：`src/NKGGameFramework.Hosting`
- React 调试面板：`src/NKGGameFramework.Hosting.Web`
- Unity/Godot 边界和 host command：`src/NKGGameFramework.Adapter.Unity`、`src/NKGGameFramework.Adapter.Godot`
- 可运行示例和引擎宿主演示：`samples`

## 实现流程

1. 先读附近源码、测试和 sample，再设计。
2. 先分类需求：
   - 阶段或界面/会话切换 -> Procedure。
   - 局内逐帧模拟 -> ECS。
   - 技能、状态、AI 行为 -> Gameplay tags、skills、buffs、behavior trees、registries。
   - 引擎对象、资源、输入、音频、UI -> Adapter、宿主或 sample。
   - 调试捕获、协议、UI -> Diagnostics、Hosting 或 Hosting.Web。
3. 纯规则/纯数据和表现层分离。在边界层把框架数据翻译成引擎对象。
4. 为框架行为添加或更新最小有意义测试；Web 面板行为使用 Web 测试。
5. 运行与改动相关的最小验证命令，并明确报告。

## Procedure 和 ECS 分工

Procedure 管游戏生命周期。Procedure 可以创建/销毁 `RuntimeContext` module、创建/销毁 `World` 和 `Scene`、加载定义、配置 debug host，并切换到另一个 Procedure。Procedure 不应包含详细的逐实体移动、伤害、碰撞、冷却、Buff 或 AI 循环。

ECS 管局内模拟。使用 `World` / `Scene` 作为运行边界，`struct IComponent` 表示状态，`ISceneComponent` 表示场景级状态，`EcsSystem` / `QuerySystem<...>` 表示逐帧规则。

当玩法 Procedure 进入一局游戏时，它应创建 ECS world、注册系统和初始实体。之后 runtime module 在 `RuntimeContext.Update` 内调用 `world.Update(in time)`。

## ECS 规则

- 组件是 `struct IComponent`。不要把组件写成 class，也不要池化组件。
- 场景级状态使用 `ISceneComponent`，例如输入、局内状态、刷怪状态、回合计数。
- 需要跨操作、跨事件、跨帧保存实体引用时使用 `EntityRef`，不要保存裸 `Entity`；`EntityRef` 带版本校验。
- 查询中可以通过 `ref` 修改已有组件字段。
- active query 中禁止结构变化。增删组件、创建/销毁实体必须使用 `SystemUpdateContext.Commands`，由 system group 在 update 后 playback。
- 组合反应用 component callback system：`IComponentAddedSystem<T>`、`IComponentUpdatedSystem<T>`、`IComponentRemovedSystem<T>`。
- 局内事件使用 Scene 级 event bus；queued event 在 `Scene.Update` 末尾派发。
- 系统顺序有语义时显式设置 `order`，尤其是 cooldown -> behavior tree -> buff lifecycle -> presentation。

## Gameplay 规则

- 状态门禁使用 `GameplayTagContainer` 和 `GameplayTagQuery`，例如沉默、免疫、阵营、元素、职业、状态、需求和阻塞条件。
- 主动技能流程使用 `SkillManager.Learn` 和 `SkillManager.TryCast`。除非测试证明需要新的扩展点，否则不要绕过 CD、消耗、tag gate、effect 校验或释放事件。
- 定时状态和周期效果使用 `BuffManager.Apply/TryApply`、`BuffUpdateSystem`、`BuffEffectRegistry`。
- 有时序、等待、取消、黑板条件、重复动作或延迟效果的技能/Buff 使用 `BehaviorTreeDefinition`。
- 引擎相关行为通过 `BehaviorActionRegistry`、`SkillEffectRegistry`、`BuffEffectRegistry` 注册。动画、特效、音频、材质、host command 不进入 core gameplay 包。
- 行为树更新应先收集实例、退出 ECS query，再执行 action；避免在 query active 时执行可能触发结构变化的 action。

## Runtime、Async、Serialization

- Runtime contracts 位于 `src/NKGGameFramework/Runtime`，保持引擎无关。
- Runtime 异步 API 使用 `UniTask` / `UniTask<T>`。
- 序列化默认使用 `OdinGameSerializer`，除非现有文件已经建立了其他 serializer。存档、缓存、热路径优先二进制；调试和配置检查可用 Odin JSON。
- 不要把具体 Asset、Scene、Audio、UI、Localization、Config 实现加入主包。主包放接口/契约，具体实现放 Adapter 或业务层。

## Nodes

- `Nodes` 只负责跨平台图数据和校验，不负责编辑器 UI。
- Node、port、port line 已经使用框架池化。扩展时保持 `IPoolItem` reset 语义。
- 静态端口使用 `NodeInputAttribute` / `NodeOutputAttribute`，运行期端口使用 dynamic port API，导入导出和静态分析使用 `NodeGraphDefinition` / validation API。
- Unity GraphView、Godot UI、React canvas 或编辑器表现放到 Adapter/Web/宿主代码，不进入 core。

## 池化和热路径

热路径中频繁创建的引用对象应使用项目池化能力：

- 短生命周期临时引用对象 -> `IPoolItem` + `MemoryPool<T>`。
- 有 spawn/unspawn 生命周期的宿主对象 -> `PoolObject` + `ObjectPool<T>`。
- 高频事件参数 -> `GameEventArgs` + `EventBus.Rent<T>()` + `FirePooled` / `FireNowPooled`。

在 `OnRelease` 或 `Clear` 中重置所有可变状态。每个租出的对象只释放一次。不要池化 ECS 组件。

## Debug、Hosting、Web

- `GameDebugRuntimeRegistry` 自动发现 runtime context 和 world。
- `Diagnostics` 负责 debug 领域模型和捕获逻辑。`Hosting` 只暴露本地 HTTP/SSE transport。`Hosting.Web` 只负责 React 调试面板。
- 使用 capture profile，不要并行实现多套捕获：
  - `LivePreview`：轻量 stream，只读。
  - `StepEditable`：payload/structured，用于 mutation。
  - `SingleFramePreview`：轻量单帧快照。
  - `DumpRecording`：轻量 frame snapshot + ECS component store blocks。
  - `DumpPlaybackPreview`：只读 dump 回放。
- Mutation 必须保持显式开启，仅用于本地开发。
- Dump 录制应使用 ECS component store blocks，不写业务组件特化 recorder。

## Adapter 和宿主规则

- Adapter 把框架数据翻译成引擎概念，不把引擎依赖反向推入 `src/NKGGameFramework`。
- Godot host command 代码应基于 ID 和 command buffer 从 ECS 状态创建、更新、销毁引擎节点。
- Sample 可以演示真实集成，但核心局内状态仍应使用框架数据。

## 验证

按改动范围运行最小相关检查：

```powershell
dotnet test .\NKGGameFramework.sln
npm --prefix .\src\NKGGameFramework.Hosting.Web test
npm --prefix .\src\NKGGameFramework.Hosting.Web run build
.\eng\verify-engine-independence.ps1
```

共享 Runtime/ECS/Gameplay/Diagnostics/Hosting/Adapter 改动运行 `dotnet test`。`Hosting.Web` 改动运行 Web 测试/构建。可执行 sample 行为改动运行对应 sample。
