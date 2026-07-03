# 框架开发模式

当 `nkg-game-development` skill 触发且任务涉及实现细节时，阅读本参考。

## 依赖边界

`src/NKGGameFramework` 是引擎无关主包。它可以依赖 standalone Odin Serializer 和 UniTask，但不能依赖 Unity、Godot、Hosting、Web、ASP.NET、编辑器 API、具体资源系统或 UI 控件。

项目依赖方向：

- `NKGGameFramework`：Core、ECS、Gameplay、Nodes、Runtime contracts、Async、Serialization、轻量 debug DTO/control/frame publisher。
- `NKGGameFramework.Diagnostics`：snapshot、mutation、dump、playback、analysis；依赖主包。
- `NKGGameFramework.Hosting`：本地 loopback HTTP/SSE debug transport；依赖 Diagnostics 和主包。
- `NKGGameFramework.Hosting.Web`：只承载 React/Vite 调试面板。
- `Adapter.Unity` / `Adapter.Godot`：引擎边界契约和 host command；主包不反向依赖 adapter。
- `samples`：可执行用法和集成示例。

当需求同时包含玩法和表现时，必须拆分：纯状态/规则放在 core 或 sample gameplay 层；引擎对象创建、特效、音频、UI、资源和 host command 放在 adapter/host 边界。

## Runtime Loop

外部宿主应只驱动一个帧入口：

```csharp
var time = GameFrameTime.Advance(runtime.Time, deltaSeconds, realDeltaSeconds);
runtime.Update(in time);
```

`RuntimeContext.Update` 会推进 timers、更新注册的 `IUpdateModule`、派发 runtime queued events，并发布 debug frame。Pause/step frame gate 挂在这里。不要围绕 `World.Update` 或 `Scene.Update` 再做一个公开帧闸。

当 world 需要随 runtime 更新时，注册 module：

```csharp
private sealed class WorldUpdateModule(World world) : Module, IUpdateModule
{
    public void Update(in GameFrameTime time)
    {
        world.Update(in time);
    }
}
```

## Procedure 和 ECS 职责

整体游戏流程和界面/会话阶段使用 `ProcedureModule`：

- 启动和框架初始化。
- 账号、登录、档案或平台连接。
- 配置、资源、本地化、场景加载。
- 主菜单、大厅、房间、匹配或组队。
- 进入局内和离开局内。
- 结算、奖励、保存和关闭。

Procedure 可以创建/销毁 `World` 和 `Scene`，注册 runtime module，配置 debug host startup，加载定义，并切换到下一个 procedure。Procedure 不应承载详细的逐实体模拟循环。

具体局内/单局逻辑使用 ECS：

- 实体生命周期和组件组合。
- 移动、近似物理规则、生成、碰撞、伤害、治疗。
- 技能 CD、行为树 tick、Buff 生命周期、tag gate。
- AI、索敌、子弹/投射物、拾取物、分数、局内状态。

如果需求描述是“游戏进入/离开/加载/保存/切换时”，从 Procedure 入手。如果描述是“每个单位/投射物/Buff/技能如何表现”，从 ECS 入手。

## ECS

使用组件表示数据，使用系统表示逐帧规则：

```csharp
internal struct Position(double x, double y) : IComponent
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
}

internal struct Velocity(double x, double y) : IComponent
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
}

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

规则：

- 组件是 `struct IComponent`；不要池化组件。
- 场景级状态使用 `class ISceneComponent`。
- 实体引用需要跨事件、Buff、行为树或帧保存时使用 `EntityRef`。
- 查询中允许对已有组件做 `ref` mutation。
- 查询迭代中禁止结构变化。使用 `context.Commands`。
- `SystemGroup` 每个 system update 创建并 playback 一个 command buffer。
- 组合回调用 `IComponentAddedSystem<T>`、`IComponentUpdatedSystem<T>`、`IComponentRemovedSystem<T>`。
- Scene events 发布 ECS 生命周期和 gameplay 事件；queued scene events 在 `Scene.Update` 后派发。

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

## GameplayTag、Skill、Buff、BehaviorTree

Gameplay gate 使用 `GameplayTagContainer` 和 `GameplayTagQuery`：

- 基础状态：`GameplayTagComponent`。
- 技能 gate：`RequiredCasterTags`、`BlockedCasterTags`、`CasterTagQuery`、`TargetTagQuery`。
- Buff 授予状态：`BuffDefinition.Tags`。
- 运行时 owned tags：`GameplayTagUtility.GetOwnedTags(entity)`。

主动技能使用 `SkillManager.Learn` 和 `SkillManager.TryCast`。定时状态和周期效果使用 `BuffManager.Apply/TryApply`、`BuffUpdateSystem`、`BuffEffectRegistry`。

技能或 Buff 需要时序、延迟、取消、黑板条件、重复动作或阶段效果时使用行为树。立即同步效果使用普通 skill effect。

三者共存时推荐系统顺序：

```csharp
scene.Systems.Add(new SkillCooldownSystem(order: 0));
scene.Systems.Add(new BehaviorTreeUpdateSystem(order: 10));
scene.Systems.Add(new BuffUpdateSystem(buffEffects, buffActions, order: 20));
```

引擎动作通过 registries 注册。动画、特效、音效、材质修改、碰撞体变化、Timeline 和 host command 属于 adapter/business registration，不进入 core。

## 池化和热路径

频繁分配的引用对象使用项目池化：

- 短生命周期临时对象 -> `MemoryPool<T>`，其中 `T : class, IPoolItem`。
- 有外部身份或 spawn/unspawn 生命周期的对象 -> `ObjectPool<T>`，其中 `T : PoolObject`。
- 热路径事件参数 -> `GameEventArgs` + `EventBus.Rent<T>()`、`FirePooled` 或 `FireNowPooled`。

在 `OnRelease` 或 `Clear` 中重置所有可变状态。每个 acquired item 只 release 一次。不要池化 ECS 组件。

示例：

```csharp
private sealed class DamageEvent : GameEventArgs
{
    public int Amount { get; set; }

    public override void Clear()
    {
        Amount = 0;
    }
}

var evt = scene.Events.Rent<DamageEvent>();
evt.Amount = amount;
scene.Events.FirePooled(evt);
```

## Runtime Contracts、Async、Serialization

Runtime contracts 保持引擎无关：

- Asset、Scene、Audio、UI、Config、Localization、Presentation、MVVM 契约位于 `Runtime`。
- 具体 Unity/Godot/FairyGUI/UGUI/Godot Control/YooAsset/Luban 实现不进入 core。
- Async API 使用 `UniTask` / `UniTask<T>`。
- Serialization 使用 `OdinGameSerializer` 承载 binary、string 和 Odin JSON payload。

存档、缓存、热路径优先 binary；调试和配置检查可用 Odin JSON。

## Nodes

Nodes 是跨平台图数据和规则：

- `NodeGraph`：可变图、事件、undo/redo。
- `Node`：静态和动态端口、连接生命周期、池化 reset。
- `NodePort` / `NodePortLine`：连接规则、类型约束、池化。
- `NodeInputAttribute` / `NodeOutputAttribute`：静态端口声明。
- `NodeGraphDefinition`、index、validation：导入导出和静态分析。

不要把 Unity GraphView、Godot UI、React canvas、编辑器窗口或视觉样式放进 core Nodes 包。

## Debug 和 Dump

只使用一条 capture pipeline。新增场景应选择或新增 `GameDebugSnapshotCaptureProfile`，并接入 `GameDebugFrameCapturePipeline`。

默认 profile：

- `LivePreview`：轻量 stream，只读。
- `StepEditable`：payload 和 structured value，用于 mutation。
- `SingleFramePreview`：轻量单帧快照。
- `DumpRecording`：轻量 frame snapshot + ECS component store blocks。
- `DumpPlaybackPreview`：只读 dump 回放。

规则：

- Mutation 仅用于本地开发，必须显式开启。
- Dump recording 保存 ECS component store blocks，不写业务组件特化 recorder。
- Diagnostics 负责 snapshot/mutation/dump/playback/analysis。
- Hosting 负责本地 HTTP/SSE transport。
- Hosting.Web 负责 React UI。

## Adapter 和 Samples

Godot/Unity adapter 把框架数据翻译成引擎概念。Godot host command flow 使用稳定 ID 和 command buffer 从 ECS 状态创建、更新、销毁节点。Entity/component/gameplay state 保持在 managed framework data 中。

Samples：

- `NKGGameFramework.Sampler`：Runtime、Procedure、ECS、serialization。
- `NKGGameFramework.SkillSystemSampler`：GameplayTag、Skill、Buff、BehaviorTree。
- `NKGGameFramework.GodotPlaneSample` 和 `GodotPlaneSample`：Godot/LeanCLR 宿主集成。

## 测试

Runtime、ECS、Gameplay、Diagnostics、Hosting、Adapter、Serialization、Nodes 行为测试放在 `tests/NKGGameFramework.Tests`。

运行：

```powershell
dotnet test .\NKGGameFramework.sln
npm --prefix .\src\NKGGameFramework.Hosting.Web test
npm --prefix .\src\NKGGameFramework.Hosting.Web run build
.\eng\verify-engine-independence.ps1
```

只改 `Hosting.Web` 时运行 Web 测试。共享 Runtime/ECS/Gameplay/Diagnostics/Hosting/Adapter 变更运行完整 `dotnet test`。可执行示例行为变更运行对应 sample。
