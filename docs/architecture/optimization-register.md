# 结构与分层优化实施台账（60 项）

状态说明：`已完成` 表示代码和项目结构已落地；`已完成（兼容迁移）` 表示新边界已经启用，同时保留被封装的旧实现以维持现有运行行为。

| 编号 | 优化项 | 状态 | 实施证据 |
|---:|---|---|---|
| 1 | 建立解决方案项目清单与依赖基线 | 已完成 | `eng/ArchitecturePolicy.json` 覆盖生产项目 |
| 2 | 将允许的项目引用图配置化 | 已完成 | `projectReferences` 白名单 |
| 3 | 将核心项目目标框架配置化 | 已完成 | `targetFrameworks` 白名单 |
| 4 | 将核心层禁止包配置化 | 已完成 | `forbiddenPackages` 白名单 |
| 5 | 自动验证项目引用没有反向依赖 | 已完成 | `ArchitecturePolicyTests` |
| 6 | 自动验证核心层保持平台无关 | 已完成 | `Core_projects_must_remain_platform_neutral` |
| 7 | 自动验证核心层不引入 UI、日志和 ORM 包 | 已完成 | `Core_projects_must_not_take_forbidden_package_dependencies` |
| 8 | 建立新增代码质量守卫 | 已完成 | `eng/JayTom.Dws.CodeQualityGuard` |
| 9 | 将质量守卫接入 Client 编译链 | 已完成 | `BuildCodeQualityGuard`、`RunCodeQualityGuard` 目标 |
| 10 | 冻结历史技术债，禁止新增同类问题 | 已完成 | `eng/CodeQualityBaseline.json` |
| 11 | 将 Abstractions 固定为跨平台核心 | 已完成 | `JayTom.Dws.Abstractions` 使用 `net10.0` |
| 12 | 将 Domain 从 Windows TFM 降为跨平台 TFM | 已完成 | `JayTom.Dws.Domain` 使用 `net10.0` |
| 13 | 将 Application 从 Windows TFM 降为跨平台 TFM | 已完成 | `JayTom.Dws.Application` 使用 `net10.0` |
| 14 | 用平台无关坐标替代 UI/绘图库坐标 | 已完成 | `Devices/Point2D.cs` |
| 15 | 用平台无关矩形替代绘图库矩形 | 已完成 | `Geometry/Rectangle2D.cs` |
| 16 | 用平台无关颜色值替代绘图库颜色 | 已完成 | `Graphics/RgbaColor.cs` |
| 17 | 用中立串口校验枚举隔离 `System.IO.Ports` | 已完成 | `Devices/SerialParity.cs` |
| 18 | 用中立停止位枚举隔离 `System.IO.Ports` | 已完成 | `Devices/SerialStopBits.cs` |
| 19 | 用所有权句柄隔离具体图像类型 | 已完成 | `Imaging/ImageHandle.cs` |
| 20 | 将 FTP 契约统一归入 Abstractions 集成命名空间 | 已完成 | `Integrations/Ftp` |
| 21 | 移除 Domain 对 `System.Drawing.Common` 的依赖 | 已完成 | Domain 项目文件与平台中立 DTO |
| 22 | 移除 Domain 对 `System.IO.Ports` 的依赖 | 已完成 | 串口设置 DTO 改用中立枚举 |
| 23 | 移除 Domain 对 NLog 的依赖 | 已完成 | Domain 项目文件及服务源码 |
| 24 | 将视频条码具体服务编译所有权移到 Infrastructure | 已完成 | Infrastructure 链接编译、Domain 排除实现 |
| 25 | 将云服务具体实现编译所有权移到 Application | 已完成 | Application 项目编译所有权配置 |
| 26 | 将授权应用服务具体实现归入 Application | 已完成 | Application 项目编译所有权配置 |
| 27 | 将跨层事件契约集中到 Domain | 已完成 | Client 删除重复事件类型并改用统一契约 |
| 28 | 在 Application 定义消息总线接口 | 已完成 | `Messaging/IEventBus.cs` |
| 29 | 将 Prism 事件聚合器降为 Client 适配实现 | 已完成 | `EventMediators/EventAggregator.cs` |
| 30 | 以应用层会话接口替代 Client 对静态包裹管理器的直接访问 | 已完成（兼容迁移） | `PackageSessions/IPackageSessionStore.cs`，Client 零直接引用 |
| 31 | 复核并移除无业务价值的独立 WPF Host | 已完成 | 解决方案不再包含 `JayTom.Dws.Host.Wpf` |
| 32 | 将 `App.xaml` 和启动代码归还主程序 | 已完成 | `JayTom.Dws.Client/App.xaml`、`App.xaml.cs` |
| 33 | 将 Client 保持为唯一桌面可执行程序 | 已完成 | Client `OutputType=WinExe` |
| 34 | 保证 WPF 入口只有 Client 拥有 | 已完成 | `Wpf_process_entry_point_must_be_owned_by_client` |
| 35 | 在主程序内保留单一应用组合入口 | 已完成 | `ApplicationComposition` |
| 36 | 由 Client 组合根注册 ViewModel 映射 | 已完成 | `ViewModelMappingRegistration` |
| 37 | 以托管服务监督接口隔离运行期协调 | 已完成 | `IHostedServiceSupervisor` |
| 38 | 将主程序启动和关闭生命周期改为异步协调 | 已完成 | Client `App.xaml.cs`、托管服务监督器 |
| 39 | 恢复同程序集 WPF 资源所有权 | 已完成 | Client 资源字典和应用资源 URI |
| 40 | 将发布入口、图标和配置统一归 Client | 已完成 | Client 项目属性与发布配置 |
| 41 | 将持久化 DI 注册从 Client 移到 Infrastructure | 已完成 | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` |
| 42 | 删除 Client 的持久化注册实现 | 已完成 | `Client/Composition/PersistenceRegistration.cs` 已移除 |
| 43 | 抽取共享仓储上下文生命周期基类 | 已完成 | `RepositoryContextBase.cs` |
| 44 | 让通用仓储复用上下文基类 | 已完成 | `RepositoryBase<T,TContext>` |
| 45 | 让本地仓储复用上下文基类 | 已完成 | `LocalRepositoryBase<T,TContext>` |
| 46 | 消除内存缓存仓储的重复缓存字段 | 已完成 | `MemoryCacheRepositoryBase.cs` |
| 47 | 统一上下文工厂与缓存依赖的构造注入 | 已完成 | `RepositoryContextBase` 构造函数 |
| 48 | 统一上下文缓存键校验 | 已完成 | `RepositoryContextBase` 校验逻辑 |
| 49 | 新建跨平台外部集成契约项目 | 已完成 | `JayTom.Dws.Integrations.Contracts` |
| 50 | 将上传器契约从供应商实现项目分离 | 已完成 | `IDataUploader`、`IApiUploader` 单一编译所有权 |
| 51 | 将 API 参数与客户端名称常量归入契约程序集 | 已完成 | `BaseApiParameters`、`ApiHttpClientNames` |
| 52 | 将网络时间契约归入集成契约程序集 | 已完成 | `INetworkTime` |
| 53 | 将上传图像参数改为平台无关句柄 | 已完成 | `UploadImageInfo.Image` 使用 `ImageHandle` |
| 54 | 让供应商实现负责解包具体图像类型 | 已完成 | Interface 各上传适配器调用 `ImageHandle.As<T>` |
| 55 | 让 Camera 项目拥有并发布 FFmpeg 原生资产 | 已完成 | Camera 项目 `ffmpegFiles` 复制规则 |
| 56 | 以中立条码格式和映射器封装 Dynamsoft 类型 | 已完成 | `SupportedBarcodeFormat`、`DynamsoftBarcodeFormatMapper` |
| 57 | 以中立发现和通道查询方法封装大华 SDK 类型 | 已完成 | `BaseDaHuatech` 包装方法、NVR 返回中立结果 |
| 58 | 移除 Client 对 NetSDKCS 与 Dynamsoft 程序集的直接引用 | 已完成 | Client 项目文件及 `ArchitectureBoundaryTests` |
| 59 | 拆分插件契约、WPF 扩展和设备适配程序集身份 | 已完成 | `Plugin.Contracts`、`Plugin.Presentation`、`DeviceAdapters` |
| 60 | 增加插件清单兼容校验、边界回归测试与 ADR | 已完成 | `PluginManifestValidator`、64 项测试、`docs/architecture/adr` |

## 兼容迁移说明

`PackageSessionStore` 当前在应用层内部委托已有的线程安全 `PackageInfoManager`，目的是不改变包裹计时器和事件语义；Client 的 265 个静态调用点已经全部改为依赖注入接口。遗留实现已被封装在单一适配器之后，可在后续迭代中替换，而不会再次影响展示层。
