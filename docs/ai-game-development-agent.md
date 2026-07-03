# AI 游戏开发 Agent 指南

本文档面向在本仓库工作的 AI agent，用来指导它们基于 NKGGameFramework 开发游戏功能、样例和宿主集成。跨工具主入口是 `agents/nkg-game-development-agent.md`；Codex 风格可选 skill 位于 `.codex/skills/nkg-game-development/SKILL.md`。

## 总原则

NKGGameFramework 的核心思想是：主框架只承载引擎无关的运行时、流程、ECS、Gameplay、Nodes、Runtime contracts、Async、Serialization 和轻量 Debug DTO/control/frame。Unity、Godot、Web Debug、HTTP/SSE、React、资源管线、编辑器 UI 都只能通过 Adapter、Hosting、Web 或业务宿主接入。

AI 生成代码时必须先判断目标层：

- 整体游戏流程：`ProcedureModule`
- 局内模拟：ECS `World` / `Scene` / `Entity` / `System`
- 技能、Buff、行为树、标签：`Gameplay`
- 节点图数据：`Nodes`
- 引擎无关服务契约：`Runtime`
- 存档/调试序列化：`Serialization`
- 调试捕获、mutation、dump、回放：`Diagnostics`
- 本地 loopback HTTP/SSE：`Hosting`
- React 调试面板：`Hosting.Web`
- Unity/Godot 桥接：Adapter 或 sample host

## Procedure 和 ECS 的分工

游戏整体流程必须优先使用 `ProcedureModule`。这些内容属于 Procedure：

- 启动和框架初始化
- 平台、账号、登录、玩家档案
- 配置、资源、场景、语言包加载
- 主菜单、大厅、房间、匹配
- 进入局内和离开局内
- 结算、奖励、保存、退出

Procedure 可以创建或销毁 `World` / `Scene`，注册 runtime module，加载玩法定义，启动本地 debug host，并切换到下一个 Procedure。Procedure 不应承载每帧实体模拟细节。

具体局内逻辑必须优先使用 ECS。这些内容属于 ECS：

- 实体生命周期和组件组合
- 移动、生成、碰撞、伤害、治疗、死亡
- 技能 CD、技能释放、行为树 tick、Buff 生命周期
- AI、索敌、子弹、拾取物、分数、局内状态

典型结构是：`GameplayProcedure` 创建 `World` 和 `Scene`，添加系统和初始实体；`Module, IUpdateModule` 在 `RuntimeContext.Update` 内推进 `world.Update(in time)`。

```csharp
var procedures = runtime.RegisterModule(new ProcedureModule());
procedures.Initialize(
    new BootProcedure(),
    new LoadingProcedure(),
    new GameplayProcedure(),
    new SettlementProcedure(),
    new ExitProcedure());
procedures.StartProcedure<BootProcedure>();
```

```csharp
private sealed class WorldUpdateModule(World world) : Module, IUpdateModule
{
    public void Update(in GameFrameTime time)
    {
        world.Update(in time);
    }
}
```

## Runtime 规则

宿主每帧只应驱动 `RuntimeContext.Update`：

```csharp
var time = GameFrameTime.Advance(runtime.Time, deltaSeconds, realDeltaSeconds);
runtime.Update(in time);
```

`RuntimeContext.Update` 会推进 timers、按 module priority 更新 `IUpdateModule`、派发 runtime queued events，并发布 debug frame。WebDebug 的 pause/step/frame stream 也挂在这个入口上；不要把 `World.Update` 或 `Scene.Update` 当成独立调试帧出口。

## ECS 规则

ECS 是轻量、单线程、引擎无关的数据组合模型：

- 组件必须是 `struct IComponent`。
- 场景级状态使用 `class ISceneComponent`，例如输入状态、局内计分、刷怪状态。
- 查询中可以通过 `ref` 修改已有组件字段。
- 查询中禁止实体创建/销毁、组件增删等结构变化；使用 `SystemUpdateContext.Commands` 记录，系统更新后 playback。
- 跨帧或异步保存实体引用时使用 `EntityRef`，不要保存裸 `Entity`。
- 组合补全或派生逻辑使用 `IComponentAddedSystem<T>`、`IComponentUpdatedSystem<T>`、`IComponentRemovedSystem<T>`。
- Scene 级 `EventBus` 适合局内事件；queued events 在 `Scene.Update` 末尾派发。
- 系统顺序有语义时必须设置 `order`，例如 CD -> 行为树 -> Buff -> 表现同步。

示例：

```csharp
internal sealed class MovementSystem : QuerySystem<Position, Velocity>
{
    protected override void OnUpdate(EntityQuery<Position, Velocity> query, in SystemUpdateContext context)
    {
        query.ForEach((ref Position position, ref Velocity velocity, Entity _) =>
        {
            position.X += velocity.X * context.DeltaTime;
            position.Y += velocity.Y * context.DeltaTime;
        });
    }
}
```

结构变化示例：

```csharp
query.ForEach((ref Health health, Entity entity) =>
{
    if (health.Value <= 0)
    {
        context.Commands.Destroy(entity);
    }
});
```

## Gameplay 规则

Gameplay 负责引擎无关的标签、技能、Buff 和行为树。

- 状态门禁使用 `GameplayTagContainer` 和 `GameplayTagQuery`，例如沉默、免疫、阵营、元素、职业、状态、需求和阻塞条件。
- 技能学习和释放走 `SkillManager.Learn` / `SkillManager.TryCast`。不要绕开 CD、消耗、标签 gate、效果校验和释放事件。
- Buff 添加和生命周期走 `BuffManager.Apply/TryApply`、`BuffUpdateSystem`、`BuffEffectRegistry`。
- 有时序、延迟、取消、等待、黑板条件、连段或循环动作的能力使用 `BehaviorTreeDefinition`。
- 动画、特效、音频、材质、碰撞体、host command 等引擎行为通过 `BehaviorActionRegistry`、`SkillEffectRegistry`、`BuffEffectRegistry` 注册，不进入主包。
- 行为树 action 执行可能触发结构变化，所以系统应先收集实例、退出 ECS query，再更新行为树。

推荐系统顺序：

```csharp
scene.Systems.Add(new SkillCooldownSystem(order: 0));
scene.Systems.Add(new BehaviorTreeUpdateSystem(order: 10));
scene.Systems.Add(new BuffUpdateSystem(buffEffects, buffActions, order: 20));
```

## 性能与池化

AI 在生成框架代码时，应主动识别热路径分配：

- 每帧、每实体、每事件、每命令、每行为树 tick、每 debug capture 都可能触发的 class 分配，应优先考虑 `MemoryPool<T>`。
- `MemoryPool<T>` 要求 `T : class, IPoolItem`。`OnAcquire` 用于进入租用状态时的准备，`OnRelease` 必须清空所有可变字段。没有 public 无参构造时传入 factory。
- 每个从 `MemoryPool<T>.Acquire()` 租出的对象必须释放一次且只能释放一次；池会检测 double release。
- `ObjectPool<T>` 适合可复用的宿主/表现对象，例如特效实例、节点包装、资源句柄包装。对象继承 `PoolObject`，通过 `Spawn` / `Unspawn` / `ReleaseExpired` 管理生命周期。
- 高频事件不要每次 `new` 普通 record/class。继承 `GameEventArgs`，用 `Rent<T>()` 获取，用 `FirePooled` 或 `FireNowPooled` 派发，并在 `Clear()` 里重置字段。
- 不要池化 ECS 组件。组件应保持值类型，热路径结构变化用 `EcsCommandBuffer`，普通字段更新用 query 的 `ref` 参数。

## Runtime Contracts、Async、Serialization

- `Runtime` 目录只定义引擎无关服务契约：Asset、Scene、Audio、UI、Config、Localization、Presentation、MVVM。
- Runtime 异步接口统一使用 `UniTask` / `UniTask<T>`。
- 具体资源系统、场景系统、音频系统、UI 控件、配置热更、语言表等实现放在 Adapter 或业务层。
- 通用序列化优先使用 `OdinGameSerializer`。二进制适合存档、缓存、热路径；Odin JSON 适合调试、配置检查和人工查看。

## Nodes

`Nodes` 是跨平台节点图数据底座，不是编辑器 UI：

- 使用 `NodeGraph` 管理节点、端口、连线、事件和 undo/redo。
- 使用 `NodeInputAttribute` / `NodeOutputAttribute` 声明静态端口。
- 使用 dynamic port API 处理运行期端口。
- 使用 `NodeGraphDefinition` / validation / index 做配置导入导出和静态分析。
- Node、NodePort、NodePortLine 已经使用 `IPoolItem` / `MemoryPool`，扩展时要保持 reset 语义。
- Unity GraphView、Godot Control、React Canvas 等可视化 UI 放在 Adapter/Hosting/Web，不进入主包。

## Debug、Hosting、Web

调试链路分层：

- `GameDebugRuntimeRegistry` 自动发现当前进程里的 `RuntimeContext` 和 `World`。
- `Diagnostics` 负责 snapshot、mutation、dump、playback、analysis。
- `Hosting` 只负责本地 loopback HTTP/SSE transport。
- `Hosting.Web` 只负责 React 调试面板。

新增调试场景不要复制 capture pipeline，应选择或新增 `GameDebugSnapshotCaptureProfile` 并接入 `GameDebugFrameCapturePipeline`。

常用 profile：

- `LivePreview`：轻量 stream，只读。
- `StepEditable`：payload/structured，用于 mutation。
- `SingleFramePreview`：轻量单帧快照。
- `DumpRecording`：轻量 frame snapshot + ECS component store blocks。
- `DumpPlaybackPreview`：只读 dump 回放。

Mutation 默认关闭，本地开发需要显式启用。Dump 录制应保留 ECS component store blocks，不为业务组件写特化 recorder。

## Adapter 和宿主

Adapter/host 的职责是把框架数据翻译成引擎对象：

- Godot/Unity 对象、节点、资源、输入、渲染、音频、UI 不进入主包。
- Godot host command 模式应从 ECS 状态生成创建、更新、销毁命令，用 ID 维持可见对象生命周期。
- sample 可以演示真实玩法和桥接，但 core/sample 的局内状态仍应是 entity、component、scene component、score、life、command buffer 等框架数据。

## 测试和验证

新增或改动框架行为时，在 `tests/NKGGameFramework.Tests` 增加聚焦 xUnit 测试。测试命名使用行为描述，例如 `Skill_cast_fails_when_target_has_blocked_tag`。

验证命令：

```powershell
dotnet test .\NKGGameFramework.sln
npm --prefix .\src\NKGGameFramework.Hosting.Web test
npm --prefix .\src\NKGGameFramework.Hosting.Web run build
.\eng\verify-engine-independence.ps1
```

只运行与改动相关的命令即可；跨层公共 API、依赖边界、主包引用、Diagnostics/Hosting/Adapter 行为变化应扩大验证范围。

交付时说明改了哪一层、为什么放在那里、新增或改变的行为、已运行的验证命令，以及未验证原因。
