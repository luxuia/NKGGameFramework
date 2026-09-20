# NKGGameFramework (Unity UPM 包)

源自 [wqaetly/NKGGameFramework](https://github.com/wqaetly/NKGGameFramework)（net10.0 纯 C# 游戏框架底层），
本 fork 将其降级移植为 **netstandard2.1 / C# 9**，重排为标准 Unity UPM 内嵌包结构，
供 deskgame（团结引擎 2022.3.62t15）以 git submodule 方式挂载在 `Packages/com.luxuia.nkgframework` 使用。

## 引入方式（deskgame 主仓库）

```bash
git submodule add -b main https://github.com/luxuia/NKGGameFramework.git Packages/com.luxuia.nkgframework
```

主仓库 `Packages/manifest.json` 需要：

```json
"com.luxuia.nkgframework": "file:com.luxuia.nkgframework",
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11",
"com.unity.nuget.newtonsoft-json": "3.0.2"
```

## 包内容

| 程序集 | 说明 |
| --- | --- |
| `Runtime/OdinSerializer` | Odin Serializer（vendor，MIT），反序列化底座 |
| `Runtime/NKGGameFramework` | Core / Ecs / Gameplay / Nodes / Runtime / MVVM / Async / Serialization |
| `Runtime/NKGGameFramework.Diagnostics` | 快照 / dump 录制回放 / 组件 mutation |

`NonPackage~/` 保留未进包的宿主侧代码（Hosting / Hosting.Web / Adapter.* / tests），仅作参考，
Unity 不编译（`~` 后缀目录）。

## 相对上游的移植改动

- 目标框架 net10.0 → netstandard2.1，C# latest → C# 9：
  - 文件级命名空间 → block 命名空间；collection expressions / required / 主构造函数 / record struct 全部降级改写
  - `System.Threading.Channels` → `BlockingCollection`；`System.Text.Json` → `Newtonsoft.Json`
  - `Stopwatch.GetElapsedTime` / `Enum.IsDefined<T>` / `PriorityQueue` / `ReferenceEqualityComparer` /
    `IReadOnlySet` / `IsExternalInit` 等缺失 API 以 `Runtime/NKGGameFramework/Compat/` 垫片补齐
- Unity 侧以 asmdef 组织：`NKGGameFramework`（noEngineReferences）+ `NKGGameFramework.Diagnostics` + `OdinSerializer`
- 依赖：UniTask（UPM git 包）、Newtonsoft.Json（`com.unity.nuget.newtonsoft-json`）

上游原 net10 工程（`dotnet build`）不再适用于本仓库布局；Unity 编译即验证。
