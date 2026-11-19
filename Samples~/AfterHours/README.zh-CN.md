# Tritone 功能演示集

### 面向 Tritone 的 Unity 独立小型示例

![After Hours — a night route through the city](Art/after-hours-banner.svg)

**选择一个功能，点击一次操作，直接查看结果。**

这个 Sample 由多个独立场景组成，方便逐项了解 Tritone。每个本地演示只聚焦一个框架能力，不需要先玩懂游戏。Sample Hub 是统一入口；每个场景都有明确操作和返回目录的按钮。

视觉使用墨色深蓝、暖纸色、城市线路几何和克制的青绿色。横幅是随 Sample 提供的 SVG 图形。

**语言：** [English](README.md) · 简体中文 · [日本語](README.ja.md)

---

## 在 Unity 中开始

1. 在 Unity 2022.3 LTS 安装 Tritone。
2. 在包的 **Samples** 标签导入 **Tritone Feature Playground**。
3. 执行 **Tritone → After Hours → Open Sample Hub**，然后点击 **Play**。命令会将示例准备到 `Assets/AfterHours`，并注册到 Build Settings。
4. 选择功能，按照场景内的说明操作。点击**返回示例目录**可试下一个功能。

也可以直接打开 `Assets/AfterHours/Scenes` 下的单个场景。每个场景都会告诉你该做什么、操作后发生了什么。资源交付和网络场景另有本机配置说明。

## 选择场景

| 场景 | 功能 | 操作与结果 |
| --- | --- | --- |
| `ApplicationLab` | 应用生命周期 | 查看运行中的应用，点击两次观察模块计数。 |
| `FlowLab` | 流程切换 | 点击两次，依次进入 Running 和 Ready，观察旧流程退出。 |
| `ModelLab` | 共享模型 | 连续累加两次，重置后再累加，观察计数重新开始。 |
| `ScopeLab` | 模块作用域 | 先发布一次，再点击解除绑定并发布。观察监听器不再收到消息，重置后重新绑定。 |
| `DiagnosticsLab` | 运行时诊断 | 写入模块日志，查看缓冲条数和采样帧。 |
| `DispatcherLab` | 主线程派发 | 排入回调，观察 Unity 下一次更新执行并显示线程编号。 |
| `EventLab` | 类型化事件 | 多次发布信号，观察监听器接收每条带编号的消息。 |
| `TweenLab` | 补间动画 | 播放补间，观察进度从 0% 到 100%。播放中重置可取消。 |
| `TimerLab` | 计时器 | 安排和取消一次性回调。 |
| `SceneModuleLab` | 场景模块 | 在 Lobby 和 Round 间切换，当前 Unity 场景仍保持打开。 |
| `InputLab` | 命名输入 | 点击发送 Interact 动作，观察计数增加。启用旧版键盘输入时也可按 E。 |
| `AudioLab` | 音效播放 | 取消 Game 视图静音，播放提示音，使用重置停止。 |
| `SaveLab` | 存档读写 | 存档并读回计数。离开再进入可以续计，重置会清除示例槽。 |
| `SettingsLab` | 类型化设置 | 让示例设置在 0 到 3 循环，每次修改后读回当前值。 |
| `LocalizationLab` | 本地化 | 切换语言，观察相同的 office.open 键返回不同文本。 |
| `EcsLab` | ECS — 生成并查询实体 | 点击 Spawn Marker 添加实体，观察查询数量并重置 World。 |
| `UiLab` | UI — 打开和关闭 Tritone 窗口 | 在 Popup 层打开一个生成窗口，关闭后再重新打开。 |
| `PoolLab` | 对象池 | 租出、归还、再次租用，对比引用确认对象复用。 |
| `DataWorkshop` | Tables — 加载一张类型化数据表 | 加载生成的 ClueRow 数据表，查看行数和首条记录。 |
| `ResourcesLab` | Resources 加载 | 通过 Tritone 作用域加载线索文本、查看字符数，随后释放引用。 |
| `ContentWorkshop` | 内容更新与事务 | 体验离线内容更新的暂存、提交与回滚流程。 |
| `AddressablesDesk` | Addressables 提供器配置 | 通过可选的 Addressables 配置加载同一 Sample 资源。 |
| `BundleDesk` | AssetBundle 提供器配置 | 使用本地 Bundle、清单与对应 Provider 配置加载内容。 |
| `NetworkDesk` | Network — 发送本地示例请求 | 打开独立网络示例；场景会显示可选的本地 Relay 设置。 |
| `ProtocolLab` | 握手与重连 | 用 Node.js 启动 LocalServices/relay.js 并连接，停止和重启 relay 观察状态变化。 |

## Tritone 能力覆盖

- **应用与模块：** Bootstrap 组合、模块顺序、应用/场景生命周期、Context、Scope、场景模块和场景切换。
- **运行时服务：** 诊断与结构化日志、主线程调度、类型化事件、计时器、Tween、对象池、输入、音频、Model、存档、设置和本地化。
- **实体：** Entity World、组件注册、查询和有序 System。
- **UI：** `UIWindow`、生成的 `UIView` 引用、Prefab 绑定、Popup 排序和窗口生命周期。
- **数据表：** Schema 类型化行、CSV/JSON 输入、代码生成、运行时读取和示例验证。
- **内容：** Resources、Addressables 和 AssetBundle Provider；内容清单、更新计划、完整性校验和事务恢复。
- **网络：** 生成消息、TCP Session、协议兼容、心跳和重连。
- **编辑器工作流：** Provider 设置、场景生成、UI 绑定、数据表/网络代码生成和内容构建。

仅限 Editor 的操作会在对应 Sample 和 `Tritone/After Hours/…` 菜单中说明，不会伪装成运行时按钮。

## 可选本地配置

Addressables、AssetBundle、内容更新和 Network 使用独立 Bootstrap。每次只运行你想了解的配置，它们不会与本地功能演示混在一起。

- **内容更新：** 依次运行 **Tritone → After Hours → Prepare Provider Labs** 和 **Build Local Bundle and Update Manifest**。通过 `node "<导入后的 sample 目录>/LocalServices/content-host.js"` 启动随包提供的本地内容服务。
- **Addressables：** 宿主项目需安装或启用 Unity Addressables，然后运行 **Prepare Provider Labs** 和 **Build Addressables Lab Content**。
- **AssetBundle：** 运行 **Prepare Provider Labs**，再运行 **Build Local Bundle and Update Manifest**。
- **Network：** 在终端通过 `node "<导入后的 sample 目录>/LocalServices/relay.js"` 启动 Relay，然后打开 **Network Desk**。Relay 只监听 `127.0.0.1`。

如果暂时没有可选依赖，Hub 和离线示例仍可正常使用。

## 数据与场景生成

导入 Sample 后，在 Unity 的 **Tritone → After Hours** 菜单中选择：

- **Generate Sample Scenes**：生成 Hub 和独立功能场景，并添加到 Build Settings。
- **Regenerate Table Outputs**：根据 `Schemas/Tables.json` 和 CSV 生成类型化数据行与 JSON 资源；示例 `ITableValidator` 会校验源数据。
- **Regenerate Network Messages**：根据 `Schemas/Network.json` 生成消息类型、Codec、Registry 和 Schema 指纹。
- **Generate Systems Window View** / **Bind Generated Systems Window View**：生成并绑定强类型 UI 示例 View。
- **Prepare Provider Labs**：准备各 Provider 独立使用的示例内容。
- **Build Addressables Lab Content** / **Build Local Bundle and Update Manifest**：构建相应可选资源配置。
- **Tritone → Build → Content for Active Platform**：为当前 Unity Target 运行通用内容构建。

## 目录结构

```text
AfterHours/
├── Art/                 随 Sample 提供的 SVG 横幅
├── Data/                CSV 示例源数据
├── Editor/              场景、数据表、UI、网络和内容工具
├── LocalServices/       可选本地 Relay 与内容服务
├── Runtime/             独立示例 Bootstrap、UI、数据和生成代码
├── Schemas/             数据表与网络 Schema
├── Scenes/              Hub 和独立功能/配置场景
└── Tests/EditMode/      Sample 数据、目录和规则测试
```

## 验证

在 Tritone 仓库根目录运行：

```powershell
.\.ci\Validate-Package.ps1
```

在 Unity Test Runner 中运行 `AfterHours.Sample.Tests` EditMode 测试，检查目录路由、示例数据和演示规则。

---

**[English](README.md)** · **[日本語](README.ja.md)**

