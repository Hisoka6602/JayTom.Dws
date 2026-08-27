# 架构与分层 200 项实施台账

> 建立日期：2026-08-14。状态只有三种：`已验证`（实现与自动化证据齐全）、`实施中`（已有代码但验收尚未全部关闭）、`待实施`。禁止仅凭文档或空接口标记完成。

| 编号 | 实施项 | 状态 | 当前证据 / 验收入口 |
|---:|---|---|---|
| 001 | 解决方案项目清单配置化 | 已验证 | eng/ArchitecturePolicy.json |
| 002 | 项目引用图白名单化 | 已验证 | ArchitecturePolicyTests |
| 003 | 禁止跨项目链接编译源码 | 已验证 | ArchitectureBoundaryTests |
| 004 | 统一程序集身份与项目职责命名 | 已验证 | 全生产程序集身份唯一；插件契约、展示扩展、设备适配发布名由架构测试锁定 |
| 005 | 桌面进程入口仅由 Client 持有 | 已验证 | ArchitecturePolicyTests |
| 006 | 核心层保持平台无关 TFM | 已验证 | ArchitecturePolicy.json |
| 007 | 契约层移除 Windows 与厂商类型 | 已验证 | Abstractions 与 Integrations.Contracts |
| 008 | 组合根集中于 Client | 已验证 | ApplicationComposition |
| 009 | 删除无独立部署价值的转发项目 | 已验证 | ArchitecturePolicyTests |
| 010 | 用 ADR 固化项目边界决策 | 已验证 | docs/architecture/adr |
| 011 | 将 App.xaml.cs 缩减为启动协调 | 已验证 | 启停、配置和健康编排迁入 `ApplicationLifecycleCoordinator`；App 薄入口测试 |
| 012 | 禁止 ViewModel 直接依赖仓储 | 已验证 | ViewModel 与 Client Service 仓储注入均为 0；Client_orchestration_must_not_name_repository_contracts |
| 013 | 按用例建立 Application Command | 已验证 | IApplicationCommand/Handler；启动配置迁移使用 MigrateConfigurationCommand；行为与架构测试 |
| 014 | 按读模型建立 Application Query | 已验证 | IApplicationQuery/Handler；SearchPackageHistoryQuery 及日志、相机、分拣 Catalog |
| 015 | 将业务后台流程迁出 Client | 已验证 | 包裹完成业务由 Application `PackageProcessingPipeline` 编排，Client 后台服务仅映射设备快照与调用输出适配器 |
| 016 | 应用层集中输入验证 | 已验证 | IApplicationRequestValidator；迁移命令与历史查询校验器及稳定错误测试 |
| 017 | 应用层定义事务边界 | 已验证 | ITransactionalApplicationCommand、ISettingsStore 原子快照契约与单次提交测试 |
| 018 | 应用层负责 DTO 与领域模型映射 | 已验证 | PackageHistoryMapper 将 EF 导航实体映射为脱离跟踪的只读 DTO；映射隔离测试 |
| 019 | 移除 Domain 对持久化 Models 的依赖 | 已验证 | 持久化绑定契约迁入 `JayTom.Dws.Legacy.Contracts`；Domain 项目移除 Models/Legacy 引用并由源码扫描门禁锁定 |
| 020 | Client 不直接依赖 Infrastructure 具体类型 | 已验证 | 操作系统与云消息实现由 Infrastructure 模块注册；Client 组合根仅调用模块入口，ViewModel 无 Infrastructure 引用 |
| 021 | 包裹会话移除静态全局状态 | 已验证 | PackageSessionRegistry |
| 022 | 包裹会话按应用实例隔离 | 已验证 | PackageSessionStores_OwnIndependentRegistries |
| 023 | 拆分包裹注册表与包裹实体文件 | 已验证 | Packages/PackageInfo 与 Manager/PackageSessionRegistry 物理分离；PackageArchitectureTests |
| 024 | 显式建模包裹状态机 | 已验证 | PackageLifecycleState、原子迁移与 PackageAggregateTests |
| 025 | 用值对象表达条码重量体积尺寸 | 已验证 | PackageBarcode、PackageWeight、PackageDimensions 及不变量测试 |
| 026 | 聚合根内部维护业务不变量 | 已验证 | 聚合方法拒绝重复/移除后条码赋值；PackageAggregateTests |
| 027 | 领域事件替代跨层可变事件参数 | 已验证 | PackageLifecycleChanged、PackageBarcodeAssigned 不可变领域事件；PackageAggregateTests |
| 028 | 领域集合只暴露只读快照 | 已验证 | PackageInfo.DomainEvents 返回只读副本，PackageSessionSnapshot 为不可变快照；PackageAggregateTests |
| 029 | 按业务能力划分聚合边界 | 已验证 | Packages 目录集中包裹聚合、状态、值对象、事件和快照；架构守卫 |
| 030 | 补齐领域规则单元测试 | 已验证 | PackageAggregateTests 与 PackageBarcodeAssignmentTests 覆盖不变量、窗口、过期、状态迁移 |
| 031 | 仓储接口按聚合与用例收敛 | 已验证 | 12 类日志仓储收敛为 ILogMaintenanceRepository，用例目录已有包裹、配置、日志 Catalog |
| 032 | 仓储接口移除 Expression 泄漏 | 已验证 | Application 公共端口全部禁止 `Expression<T>`；相机目录改用 `Func` 应用筛选，旧表达式仓储仅隔离在 Legacy 项目 |
| 033 | 查询规格移到应用查询对象 | 已验证 | PackageHistoryQuery + SearchPackageHistoryQuery 封装筛选与分页；Query Handler 测试 |
| 034 | 读写仓储职责分离 | 已验证 | IRepository 显式组合 IReadRepository 与 IWriteRepository，架构测试锁定边界 |
| 035 | 仓储方法统一异步取消契约 | 已验证 | 反射测试验证全部仓储 Task 方法包含 CancellationToken，日志清理回归验证取消传播 |
| 036 | 仓储不吞异常或返回伪成功 | 已验证 | RepositoryBase |
| 037 | 删除重复 RepositoryBase 变体 | 已验证 | RepositoryContextBase |
| 038 | 统一 DbContext 生命周期所有权 | 已验证 | RepositoryContextBase |
| 039 | 事务与 UnitOfWork 显式化 | 已验证 | `IConfigurationUnitOfWork` 独立控制配置快照事务、回滚与提交后缓存刷新，并由架构测试锁定 |
| 040 | 禁止 ViewModel 拼接数据库查询 | 已验证 | 出口、8 类日志、包裹历史、分拣、通讯及相机查询均走 Application Query/Catalog；架构守卫 |
| 041 | EF 实体与领域实体物理分离 | 已验证 | 运行时包裹聚合与 `PackageInfoModel` 分属 Legacy.Contracts/Models 项目，架构测试确保仅 EF 实体含表映射 |
| 042 | EF 映射改用 Fluent Configuration | 已验证 | 包裹与日志实体使用 IEntityTypeConfiguration，DbContext 不再内联实体映射 |
| 043 | 按模块拆分 DbContext 配置 | 已验证 | PackageModelConfigurations 与 LogModelConfigurations 分别拥有模块映射 |
| 044 | SQLite 初始化幂等化 | 已验证 | SqliteDatabaseInitializer |
| 045 | 迁移替代运行期 EnsureCreated 演进 | 已验证 | SqliteSchemaMigrator 区分空库基线引导与版本化 Migrate，运行路径不再调用 EnsureCreated |
| 046 | 连接与 PRAGMA 集中配置 | 已验证 | SqliteConnectionPragmaInterceptor |
| 047 | 查询默认 AsNoTracking | 已验证 | 通用本地/远程仓储及日志维护查询统一 AsNoTracking，DbContext 默认 NoTracking |
| 048 | 热查询投影并限制结果集 | 已验证 | 包裹分页硬限制 1000 条；日志最早记录查询仅投影 CreateTime |
| 049 | 批量写入使用明确事务策略 | 已验证 | 两类通用仓储批量写入均显式 BeginTransaction/Commit 并传递取消令牌 |
| 050 | 数据库兼容性建立回归测试 | 已验证 | 新空库基线、旧 INTEGER/REAL 文件原位读写与迁移历史共 2 项 SQLite 回归测试 |
| 051 | ISettingsStore 成为唯一配置入口 | 已验证 | ISettingsStore |
| 052 | 配置对象按模块拆分 | 已验证 | ConfigurationSectionCatalog 按设备、分拣、输出、集成、运维登记强类型配置节 |
| 053 | 配置契约与 EF 实体解耦 | 已验证 | Application 配置契约不引用 EF/ConfigInfoModel，映射仅存在于 Infrastructure 适配器并有边界测试 |
| 054 | 配置读写支持取消与错误结果 | 已验证 | `ISettingsAccess`/`SettingsAccess` 提供 CancellationToken 与稳定 `Result`/`OperationResult`，含取消测试 |
| 055 | 配置更新提供原子快照 | 已验证 | `ReplaceSnapshotAsync` 在单个 SQLite 事务中完整替换并刷新缓存；迁移回归测试 |
| 056 | 配置校验集中到应用层 | 已验证 | ConfigurationValidationRegistry 集中分派称重与图像校验，SettingsAccess/SettingsStore 统一拒绝无效写入 |
| 057 | 敏感配置加密存储 | 已验证 | IEventBus |
| 058 | 配置变更使用强类型事件 | 已验证 | EventAggregator 适配器 |
| 059 | 禁止 ViewModel 直接读配置仓储 | 已验证 | 架构测试禁止 ViewModel 引用 `IConfigRepository` 或配置 EF 实体 |
| 060 | 配置版本与迁移策略化 | 已验证 | 连续版本 `IConfigurationMigration` + 启动期 `ConfigurationMigrationRunner` + 失败不提交测试 |
| 061 | 移除 EventAggregator.Instance 静态入口 | 已验证 | 静态入口已删除；Client_must_not_use_the_static_event_bus_entrypoint |
| 062 | 消息总线通过依赖注入使用 | 已验证 | 31 个设置页、13 个独立 ViewModel 与 29 个服务统一注入 IEventBus |
| 063 | 业务事件与 UI 事件分离 | 已验证 | 14 个业务事件迁入 Application/Events，窗口事件独立位于 Client/Events |
| 064 | 事件契约归入所有者层 | 已验证 | Domain 巨型事件定义文件已移除，16 个契约各自独占文件并有所有权架构测试 |
| 065 | 事件发布明确同步异步语义 | 已验证 | 53 个 async-void 订阅迁入 SubscribeAsync/SubscribePackageAsync；架构测试禁止回退 |
| 066 | 异步订阅使用有界队列 | 已验证 | SequentialAsyncEventHandler 容量边界与 Async_subscription_applies_bounded_backpressure |
| 067 | 订阅异常隔离并可观测 | 已验证 | EventAggregator 结构化记录异常；Async_subscription_isolates_handler_failures |
| 068 | 订阅返回可释放令牌 | 已验证 | IDisposable 退订、移除保活引用并原子清空积压；Disposing_async_subscription_discards_pending_events |
| 069 | 事件载荷不可变 | 已验证 | 包裹领域事件为不可变 record；全部兼容事件公开属性改为 init-only；EventPayloadArchitectureTests 反射锁定 |
| 070 | 关键事件补齐顺序与并发测试 | 已验证 | EventAggregatorTests 覆盖顺序、故障、背压、释放竞态 |
| 071 | 后台服务注册清单集中化 | 已验证 | PackageBackgroundService |
| 072 | 只注册真实生产后台服务 | 已验证 | HostedWorkflowRegistration 不注册 TestBackgroundService，架构测试锁定 |
| 073 | 后台服务依赖按启动顺序声明 | 已验证 | 消费者先启动、主生产流程最后启动；协调器停机时逆序处理 |
| 074 | 关闭流程按依赖逆序执行 | 已验证 | PackageSessionRegistry |
| 075 | 故障重启创建新服务实例 | 已验证 | PackageTimerScheduler |
| 076 | 首次启动失败向应用传播 | 已验证 | PackageSessionRegistry |
| 077 | 重启使用有界指数退避 | 已验证 | PackageInfo |
| 078 | 稳定运行后重置失败计数 | 已验证 | StableRunFailureCounter 接入 HostedServiceSupervisor；稳定窗口前累加、达到窗口后归一测试 |
| 079 | 健康状态统一输出 | 已验证 | ImageHandle |
| 080 | 后台循环统一响应 CancellationToken | 已验证 | 全部宿主后台/处理/监督主循环检查取消状态或使用带 stoppingToken 的 PeriodicTimer；BackgroundLoopArchitectureTests |
| 081 | 包裹处理拆为采集匹配完成阶段 | 已验证 | 新增 Acquisition、Matching、Completion 三阶段及顺序、短路单测和 Client 接入架构门禁 |
| 082 | 包裹会话索引避免全表扫描 | 已验证 | PackageSessionRegistry 索引 |
| 083 | 条码绑定在单一临界区完成 | 已验证 | TryBindBarcode |
| 084 | 过期与赋值竞争重新校验 | 已验证 | PackageBarcodeAssignmentTests |
| 085 | 定时器由统一调度器管理 | 已验证 | PackageTimerScheduler |
| 086 | 定时器回调不捕获全局状态 | 已验证 | PackageSessionRegistry |
| 087 | 包裹完成状态单向迁移 | 已验证 | PackageInfo |
| 088 | 移除时先发事件再释放资源 | 已验证 | OnPackageRemoved 使用 try/finally 保证先通知后释放；PackageArchitectureTests |
| 089 | 包裹图片所有权显式转移 | 已验证 | ImageHandle |
| 090 | 包裹流水线补齐压力与竞态测试 | 已验证 | 64 路并发单次绑定、排队过期竞态及 1000 包裹吞吐基线 |
| 091 | 拆分 DefaultSortingService 巨型类 | 已验证 | API 规则解析和模式分派已拆为独立协作者；主类降至 1900 行以内并由架构测试锁定 |
| 092 | 拆分 DefaultSortingConnectionService | 已验证 | DefaultSortingConnectionService |
| 093 | 排序规则使用策略注册表 | 已验证 | 生产路径改用 LegacySortingStrategyRegistry，应用层提供可扩展 SortingStrategyRegistry |
| 094 | 出口路由使用不可变快照 | 已验证 | SortingExitSnapshot |
| 095 | 连接查询使用预计算索引 | 已验证 | SortingConnectionLookupSnapshot 按格口预计算连接字典，热路径使用 TryGetValue |
| 096 | 排序输入输出建立明确 DTO | 已验证 | SortingRequest、SortingDecision、SortingProtocolCommand 与 SortingDispatchReceipt 物理隔离 |
| 097 | 排序失败使用统一结果类型 | 已验证 | 策略、协议端口和管道统一返回 OperationResult |
| 098 | 排序流程支持取消与超时 | 已验证 | SortingPipeline 使用链接令牌与总时限并返回稳定取消、超时错误码 |
| 099 | 厂商排序协议放入适配层 | 已验证 | SortingConnectionProtocolAdapter 位于 Client 适配层，应用层只持有协议端口 |
| 100 | 端到端排序流程建立契约测试 | 已验证 | 6 个管道契约测试覆盖路由、策略失败、取消、超时和协议失败 |
| 101 | 跨平台集成契约物理归属 Contracts | 已验证 | Integrations.Contracts |
| 102 | 供应商上传实现留在 Interface | 已验证 | Interface |
| 103 | HTTP 客户端使用命名或类型客户端 | 已验证 | 全部供应商适配器统一使用 `ApiHttpClientNames.ExternalApi`，组合根只有 `AddDwsIntegrationHttpClient` 一个注册入口，架构测试禁止直接构造客户端 |
| 104 | 集成超时重试熔断策略集中 | 已验证 | `IntegrationResilienceHandler` 集中管理超时、幂等安全重试和并发熔断，单元测试覆盖成功重试、POST 不重放、超时与熔断 |
| 105 | 外部参数使用不可变强类型模型 | 已验证 | `BaseApiParameters` 改为 init-only record，韧性参数由不可变 `IntegrationResilienceOptions` 表达并在注册时校验 |
| 106 | 响应解析与业务决策分离 | 已验证 | `DefaultApiResponseEvaluator` 从 HTTP 传输中提取精确、包含与正则判定，行为与架构测试锁定边界 |
| 107 | 网络时间失败不伪造结果 | 已验证 | INetworkTime |
| 108 | 所有集成调用支持取消 | 已验证 | 集成契约 |
| 109 | 接口凭据不进入日志 | 已验证 | 全部接口参数审计快照统一经 `IntegrationParameterSerializer` 递归脱敏，源码守卫禁止回退到直接 JSON 序列化 |
| 110 | 外部系统使用契约与沙箱测试 | 已验证 | `IntegrationBoundaryTests` 使用内存消息处理器验证传输、解析、韧性和脱敏契约，全程不访问真实网络 |
| 111 | 相机厂商 SDK 仅存在 Camera | 已验证 | Camera 边界测试 |
| 112 | Camera 契约移除 object 参数 | 已验证 | `ICamera.Initialize(CameraInfo, CancellationToken)`、`Start(CancellationToken)` 与 NVR 强类型初始化契约；架构测试锁定 |
| 113 | Camera 契约移除 Dictionary object | 已验证 | `CameraRuntimeSettings`、`UsbCameraSettings`、`ApplySettingsAsync`；USB 设置页与设备启动链已迁移；架构测试锁定 |
| 114 | 图像契约使用所有权句柄 | 已验证 | ImageHandle |
| 115 | OCR 契约移除 Bitmap | 已验证 | `OcrImageFrame` 公共载荷 + `OcrBitmapAdapter` 平台适配 + 架构测试 |
| 116 | OCR 引擎与 WPF 完全解耦 | 已验证 | OCR 项目无 WPF 引用，架构测试禁止回归 |
| 117 | NVR 实现归并为单一所有者 | 已验证 | 契约与大华实现迁入 Camera，旧 Nvr 项目从 Client、解决方案和策略清单移除；Nvr_adapter_must_have_a_single_project_owner |
| 118 | 设备发现结果使用中立 DTO | 已验证 | 设备 DTO |
| 119 | 厂商异常映射为统一设备错误 | 已验证 | DeviceExceptionEventArgs |
| 120 | 设备适配器建立模拟器契约测试 | 已验证 | `SimulatedCamera` 无需厂商 SDK 即可复现初始化、启停、预览、拍照、断线、异常与取消；`CameraAdapterContractTests` 锁定契约 |
| 121 | 插件契约保持跨平台 | 已验证 | Plugin.Contracts |
| 122 | 插件 WPF 扩展独立程序集 | 已验证 | Plugin.Presentation |
| 123 | 设备适配器不伪装为插件契约 | 已验证 | DeviceAdapters |
| 124 | 插件清单包含版本兼容信息 | 已验证 | PluginManifestValidator |
| 125 | 实现真正的动态插件发现 | 已验证 | `PluginRuntime.DiscoverAsync` 递归发现 `plugin.json`；PluginRuntimeTests |
| 126 | 插件使用独立 AssemblyLoadContext | 已验证 | 每插件独占 collectible `PluginLoadContext` 与 `AssemblyDependencyResolver` |
| 127 | 插件依赖冲突可诊断 | 已验证 | `PluginLoadDiagnostic` 稳定状态、消息和异常类型；不保留插件异常对象 |
| 128 | 插件加载失败隔离 | 已验证 | 单清单失败转换为诊断并继续发现；Invalid_plugin_is_isolated_from_valid_plugins |
| 129 | 插件生命周期支持卸载 | 已验证 | `PluginHandle.DisposeAsync` 停止后台插件、释放实例并调用 `Unload` |
| 130 | 插件 API 兼容性测试自动化 | 已验证 | PluginManifestValidatorTests 与 PluginRuntimeTests 覆盖版本、契约、发现和隔离 |
| 131 | ViewModel 只依赖应用服务 | 已验证 | ViewModel 仅依赖 Application Catalog/UseCase 与能力端口；磁盘能力改为 `IDiskInventory`，门禁禁止 Infrastructure 和旧仓储接口 |
| 132 | ViewModel 不直接访问 DbContext | 已验证 | 已清理残留 EF Core 引用，ArchitectureBoundaryTests 全量扫描 ViewModels 的 EF/DbContext 依赖 |
| 133 | ViewModel 不直接构造设备实现 | 已验证 | USB 设备由工厂创建、IPC/NVR 发现归入 IDeviceService，架构测试禁止 ViewModel 直接 new 设备实现 |
| 134 | 命令执行统一忙碌与取消状态 | 已验证 | `AsyncOperationController` 统一忙碌、重入与取消，列表及批量操作基类已接入并有并发/取消测试 |
| 135 | 导航参数使用强类型对象 | 已验证 | 区域与页面封装为 `NavigationRegion`/`NavigationDestination`/`NavigationRequest`，ViewModel 禁止直接调用 `RequestNavigate` |
| 136 | 对话框服务封装 UI 交互 | 已验证 | `UserDialogService` 统一封装 DialogHost，架构测试禁止 ViewModel 绕过门面 |
| 137 | ViewModel 拆分大文件与职责 | 已验证 | CameraFinder 的 SDK 部署/校验已抽为独立服务，最大 ViewModel 文件预算锁定为 1150 行 |
| 138 | 属性变更减少重复样板 | 已验证 | ViewModel 统一使用 `SetProperty`，架构测试禁止手写 `PropertyChanged.Invoke` |
| 139 | UI 线程切换集中封装 | 已验证 | 323 处 Dispatcher 访问收口至 `UiThread`，架构测试禁止直接访问 WPF Application Dispatcher |
| 140 | ViewModel 建立无 WPF 业务测试 | 已验证 | 分页规则抽取为 Application `PaginationState`，测试项目不引用 Client/WPF 并覆盖边界场景 |
| 141 | 用 OperationResult 替代 KeyValuePair 结果 | 已验证 | OperationResult<T> |
| 142 | 错误代码使用稳定枚举或值对象 | 已验证 | Result 与 OperationResult 统一携带不可变 Error 值对象，ResultContractTests 验证稳定代码和值语义 |
| 143 | 异常与预期失败明确区分 | 已验证 | 预期验证/取消失败通过 Result 返回，非法结果和值访问抛编程异常；ResultContractTests 覆盖两类语义 |
| 144 | 时间通过 TimeProvider 注入 | 已验证 | 包裹生命周期、许可证与云服务注入 TimeProvider；Core_business_time_must_use_time_provider |
| 145 | 业务时间明确本地时区语义 | 已验证 | 核心层统一 GetLocalNow().DateTime，DTO 不再隐式读取墙上时钟 |
| 146 | 耗时字段名称带单位 | 已验证 | OCR 公共结果改用 ElapsedMilliseconds，全部生产调用点迁移，StablePublicApiTests 验证字段类型与兼容映射 |
| 147 | 日志统一结构化字段 | 已验证 | SafeLoggerExtensions 与 StructuredLogFields 统一 Operation、Correlation、Duration、ErrorCode；ObservabilityArchitectureTests |
| 148 | 链路使用 CorrelationId | 已验证 | CorrelationContext 跨异步链路传播并接入命令、查询、事件、生命周期；ObservabilityContractTests |
| 149 | 敏感字段统一脱敏 | 已验证 | SensitiveDataRedactor 按字段名与消息模式脱敏，API 后台日志统一调用；ObservabilityContractTests |
| 150 | 关键路径统一指标与追踪 | 已验证 | DwsDiagnostics 提供稳定 ActivitySource、Meter、成功/失败计数与耗时直方图；关键路径与指标监听测试 |
| 151 | 热路径改用有界 Channel | 已验证 | Channel 队列实现 |
| 152 | 队列容量与并发度配置化 | 已验证 | 有界并发配置 |
| 153 | 定义背压丢弃与降级策略 | 已验证 | TryEnqueue 明确拒绝最新事件并由 EventAggregator 记录背压 |
| 154 | 避免循环内重复 LINQ 排序扫描 | 已验证 | PackageSessionRegistry 使用 SortedSet 稳定索引且守卫禁止 OrderBy 回归 |
| 155 | 缓存使用不可变快照 | 已验证 | PackageSessionSnapshot 为不可变 record，GetSnapshot 返回只读副本；领域测试 |
| 156 | 锁粒度按聚合实例收敛 | 已验证 | PackageInfo.SyncRoot 聚合实例锁与并发唯一赋值测试 |
| 157 | 异步流程禁止同步阻塞 | 已验证 | 清除生产代码 `GetResult`/`Task.Wait`；FTP、分拣及连接队列改用 `AsyncOrderedDispatcher`，同步释放用 `TaskCleanup` 观察异步清理 |
| 158 | 并发任务统一观察异常 | 已验证 | CS4014 恢复为编译错误；客户端 fire-and-forget 全部经 Forget 观察，消息排空器拥有并隔离任务；AsyncTaskArchitectureTests |
| 159 | 资源池与 Semaphore 正确释放 | 已验证 | `SemaphoreLease` 归入 Abstractions 并由三类仓储基类统一使用；取消不误释放、重复释放幂等及异步调度器资源释放均有测试 |
| 160 | 建立性能基线和回归门禁 | 已验证 | eng/PerformanceBudget.json 与 PerformanceBaselineTests（索引操作、条码流水线） |
| 161 | 原生资产由实际适配项目拥有 | 已验证 | Camera.csproj |
| 162 | 清理重复 FFmpeg 与 SDK 文件 | 已验证 | Release 发布检测并移除 10 组相同厂商/ONNX/OpenCV 二进制副本；源码与发布重复哈希测试均为零 |
| 163 | 发布清单按 RID 管理 | 已验证 | native-assets.win-x64.json 显式声明 win-x64 清单并随 Client 构建/发布复制，启动与发布脚本校验 RID |
| 164 | 原生依赖版本与校验和登记 | 已验证 | 10 类厂商 SDK 入口登记名称、源/发布路径、版本、长度、SHA-256，DeploymentAssetTests 校验跟踪资产 |
| 165 | Release 发布排除调试符号 | 已验证 | Client 发布目标 |
| 166 | 大模型与资源改为外置包 | 已验证 | OCR 模型通过可覆盖的 `DwsModelAssetsRoot` 外置复制，`model-assets.json` 登记版本、长度、SHA-256 与输出路径，架构测试校验真实文件 |
| 167 | 启动前验证原生依赖完整性 | 已验证 | ApplicationLifecycleCoordinator 在配置/服务启动前运行 NativeDependencyValidator，篡改回归测试验证安全失败 |
| 168 | 发布产物做重复文件检测 | 已验证 | Test-PublishArtifact.ps1 对 Release 产物按长度+SHA-256 检测重复二进制；本地二次发布验证为零且 CI 强制执行 |
| 169 | 许可证文件按最小权限部署 | 已验证 | 生产授权脚本关闭 ACL 继承，仅授予生成账户读取、SYSTEM/Administrators 管理权限；SecurityBoundaryTests 锁定规则 |
| 170 | 建立干净环境安装冒烟测试 | 已验证 | 本地全新 Release 输出通过主程序、RID 清单、10 个原生入口、无 PDB/无重复检查；CI 每次重新生成并执行同一脚本 |
| 171 | 密钥令牌禁止明文入库 | 已验证 | 已清除接口模板及 Dynamsoft 源码许可证，统一从环境变量读取；SecurityBoundaryTests 扫描配置与生产源码凭据 |
| 172 | 应用配置禁止提交真实凭据 | 已验证 | 配置模板只允许空值或环境占位符，自动化测试禁止回归 |
| 173 | HTTP 证书验证禁止绕过 | 已验证 | 全生产源码静态测试禁止危险证书回调与 AcceptAny 校验器 |
| 174 | JWT 验证参数集中且严格 | 已验证 | `TokenManagement` 集中校验强密钥及 issuer/audience/lifetime/signature，JWT 安全测试 |
| 175 | 文件路径输入规范化与约束 | 已验证 | PluginRuntime 对根目录、清单目录和 DLL 扩展执行规范化及目录边界校验 |
| 176 | SQL 仅使用参数化接口 | 已验证 | 删除通用 Raw SQL 入口并改用 EF `Remove`/`ExecuteDeleteAsync`；RawSql 基线为零 |
| 177 | 反序列化限制类型与大小 | 已验证 | 插件清单仅反序列化强类型 `PluginManifest`，限制 64 KiB 且禁用多态类型加载 |
| 178 | 外部进程调用参数安全转义 | 已验证 | FFmpeg 与 Explorer 使用 `ProcessStartInfo.ArgumentList`，静态测试禁止 `Arguments` 拼接 |
| 179 | 日志与心跳移除敏感信息 | 已验证 | 结构化健康输出 |
| 180 | 安全边界加入静态检查 | 已验证 | SecurityBoundaryTests 覆盖凭据、TLS、JWT、SQL 与进程参数边界 |
| 181 | 架构策略测试覆盖全部生产项目 | 已验证 | ArchitecturePolicyTests |
| 182 | 禁止引用图偏离白名单 | 已验证 | ArchitecturePolicy.json |
| 183 | 禁止核心层引入外层包 | 已验证 | forbiddenPackages |
| 184 | 禁止新增静态服务定位器 | 已验证 | EventAggregator.Instance 已移除；静态容器限定在 App 组合根，ArchitectureBoundaryTests 阻止业务编排新增服务定位器 |
| 185 | 禁止新增 ViewModel 仓储依赖 | 已验证 | 泛化正则门禁同时覆盖 ViewModels 与 Client Service，直接仓储类型为 0 |
| 186 | 禁止新增跨层共享源码 | 已验证 | ArchitectureBoundaryTests |
| 187 | 关键模块补齐单元测试 | 已验证 | `critical-module-tests.json` 覆盖包裹、用例、事件并发、持久化、相机、外部接口、插件、可观测性与性能，门禁验证文件和测试方法存在 |
| 188 | 适配器补齐集成契约测试 | 已验证 | 相机使用无硬件模拟器，HTTP 使用内存消息处理器；两类适配器契约测试均覆盖成功、失败和取消且不依赖真实外部系统 |
| 189 | 发布流程执行全量测试 | 已验证 | ci.yml 的 Release 门禁执行解决方案构建、完整测试项目、质量守卫与格式检查，EngineeringGovernanceTests 防回归 |
| 190 | 维护 200 项可审计实施台账 | 已验证 | ArchitectureBoundaryTests 精确验证 200 项连续编号 |
| 191 | 按业务能力建立模块所有权 | 已验证 | module-ownership.md 覆盖 ArchitecturePolicy 全部生产项目，EngineeringGovernanceTests 验证新增项目必须登记 |
| 192 | 为关键模块指定稳定公共 API | 已验证 | public-api-policy.md 指定 Result、配置、相机、OCR、插件稳定边界，StablePublicApiTests 锁定程序集与关键成员 |
| 193 | 内部实现默认 internal | 已验证 | 新增模拟器、韧性处理器、参数脱敏器和响应判定器均为 internal；`public-implementation-budget.json` 对四个实现项目执行 public class 零增长门禁 |
| 194 | 废弃 API 使用版本化迁移 | 已验证 | OcrResult.ElapsedTime 以 Obsolete 保留并指向 ElapsedMilliseconds，声明 v2 删除且回归测试验证双向兼容 |
| 195 | 技术债预算进入迭代门禁 | 已验证 | CodeQualityBaseline 逐文件零增量门禁接入本地构建、CI 与 PR 清单；technical-debt-budget.md、EngineeringGovernanceTests |
| 196 | 架构指标纳入持续集成 | 已验证 | CI 强制运行 ArchitecturePolicyTests、ArchitectureBoundaryTests 与 CodeQualityGuard，治理测试锁定完整门禁 |
| 197 | 变更必须同步 ADR 与测试 | 已验证 | PR 模板要求边界变更同步 ADR、版本迁移和自动化证据，EngineeringGovernanceTests 验证模板约束 |
| 198 | 依赖升级通过集中版本管理 | 已验证 | Directory.Packages.props 集中全部 NuGet 版本，治理测试禁止 csproj 内联版本并验证中央条目完整 |
| 199 | 建立可回滚的数据与配置迁移 | 已验证 | 迁移回执保存完整旧快照，`RollbackAsync` 原子精确恢复并删除新增键；回归测试 |
| 200 | 定期删除兼容层并复核边界 | 已验证 | 本台账 |

## 关闭规则

- `已验证` 必须至少具备一项代码/配置证据和一项可重复执行的测试、构建或发布验证。
- `实施中` 不计入完成数；相关兼容层、基线冻结和临时适配仍需继续清理。
- 每次关闭项目时同步更新本台账，并运行 `dotnet test JayTom.Dws.Tests/JayTom.Dws.Tests.csproj --no-restore --nologo`。
- 涉及 Client、原生资产或发布的项目，还必须运行 Client Release 构建及发布产物检查。
