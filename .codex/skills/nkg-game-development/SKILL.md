---
name: nkg-game-development
description: 当 Codex 需要在 NKGGameFramework 仓库中构建、扩展、调试、评审或解释游戏功能、sample、adapter 集成、runtime loop、Procedure 流程、ECS 系统、技能、Buff、行为树、debug host、节点图或玩法原型时使用。
---

# NKG 游戏开发

使用本 skill 以 NKGGameFramework 的框架原生方式完成游戏开发任务。保持主包引擎无关，通过 `RuntimeContext.Update` 驱动游戏，用 Procedure 管整体流程，用 ECS 管局内模拟，并运行最小相关验证。

## 第一轮判断

1. 先阅读任务相关源码、测试和 sample，再设计。
2. 实现涉及 runtime、Procedure、ECS、Gameplay、Nodes、adapter、debug hosting 或 sample 时，阅读 `references/framework-patterns.md`。
3. 先给任务分类：
   - 阶段切换 -> Procedure。
   - 局内模拟 -> ECS。
   - 技能、状态、AI -> Gameplay tags、skills、buffs、behavior trees、registries。
   - 引擎对象、资源、输入、音频、UI -> Adapter、宿主或 sample。
   - 调试捕获、协议、UI -> Diagnostics、Hosting 或 Hosting.Web。
4. 保持 `src/NKGGameFramework` 不依赖 Unity、Godot、Web、ASP.NET、hosting、editor、asset-pipeline 或 UI-control。
5. 框架行为增加聚焦测试到 `tests/NKGGameFramework.Tests`；只有演示行为或 API 使用示例需要 sample 时才改 sample。

## 核心规则

- 外部宿主驱动 `RuntimeContext.Update`。World/Scene 应由 runtime module 或 sample 在这一帧内推进。
- 整体流程使用 `ProcedureModule`：启动、登录、加载、大厅、进入/离开局内、结算、保存、关闭。
- 局内玩法使用 ECS：`World`、`Scene`、`struct IComponent`、`ISceneComponent`、`EcsSystem`、`QuerySystem<...>`、`SystemUpdateContext.Commands`。
- 可能跨操作存活的实体引用使用 `EntityRef`。
- 状态门禁使用 `GameplayTagContainer` / `GameplayTagQuery`。
- 能力系统优先使用 `SkillManager`、`BuffManager`、`BehaviorTreeDefinition`、`BehaviorActionRegistry` 和 effect registries。
- 高频临时引用对象用 `MemoryPool<T>` + `IPoolItem`；可复用宿主对象用 `ObjectPool<T>` + `PoolObject`；高频事件用 `GameEventArgs` + `Rent` / `FirePooled`。
- Runtime 异步契约使用 `UniTask`；框架序列化默认使用 `OdinGameSerializer`，除非现有代码另有约定。
- `Nodes` 只承载图数据和校验；编辑器 UI 放在 adapter/web/host。
- 不要创建并行 debug pipeline。使用 capture profile 和 `GameDebugFrameCapturePipeline`。

## 验证

运行最小相关验证：

```powershell
dotnet test .\NKGGameFramework.sln
npm --prefix .\src\NKGGameFramework.Hosting.Web test
npm --prefix .\src\NKGGameFramework.Hosting.Web run build
.\eng\verify-engine-independence.ps1
```

交付时说明实际运行了哪些检查，以及相关检查跳过的原因。
