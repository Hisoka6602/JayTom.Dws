# 第二批架构与分层 200 项实施台账

本台账只记录在首批台账之后实施的第二批改造。关闭规则：代码或配置证据真实存在，且至少有一个可执行自动化测试文件承担回归验证；状态统一使用 `Verified`。

| 编号 | 分类 | 已实施事项 | 状态 | 主要证据 | 自动化验证 |
|---:|---|---|---|---|---|
| S2-001 | Legacy boundary migration | 将历史 DTO 从 Domain 迁入 Legacy.Contracts | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-002 | Legacy boundary migration | 将历史仓储契约从 Domain 迁入 Legacy.Contracts | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-003 | Legacy boundary migration | 将持久化实体命名空间统一为 Models | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-004 | Legacy boundary migration | 将历史服务契约隔离到 Legacy.Contracts.Services | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-005 | Legacy boundary migration | 将应用服务实现移出 Domain 命名空间 | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-006 | Legacy boundary migration | 将下位机协议隔离到 Legacy.Contracts.DownstreamProtocols | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-007 | Legacy boundary migration | 将历史包裹管理契约隔离到 Legacy.Contracts.Packages | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-008 | Legacy boundary migration | 移除 Domain 对历史事件聚合文件的所有权 | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-009 | Legacy boundary migration | 建立旧 Domain 命名空间零回流门禁 | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-010 | Legacy boundary migration | 建立兼容层可删除性与单向依赖边界 | Verified | `JayTom.Dws.Legacy.Contracts/JayTom.Dws.Legacy.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/DomainIsolationArchitectureTests.cs` |
| S2-011 | Physical module boundaries | 新建平台无关 Camera.Contracts 项目 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-012 | Physical module boundaries | 新建独立 Plugin.Runtime 项目 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-013 | Physical module boundaries | 保持 Plugin.Abstractions 只承载稳定契约 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-014 | Physical module boundaries | 保持 Camera.Contracts 使用净 net10.0 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-015 | Physical module boundaries | 将插件加载实现从契约程序集移出 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-016 | Physical module boundaries | 将相机厂商 SDK 留在 Camera 适配层 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-017 | Physical module boundaries | 将外部集成契约归入 Integrations.Contracts | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-018 | Physical module boundaries | 将持久化模型物理归入 Models 项目 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-019 | Physical module boundaries | 将兼容契约物理归入 Legacy.Contracts 项目 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-020 | Physical module boundaries | 将新增项目纳入解决方案与依赖白名单 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/ProjectDependencyTests.cs` |
| S2-021 | Application use-case pipeline | 建立统一 Application Command 管道 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-022 | Application use-case pipeline | 在命令执行前集中运行输入校验 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-023 | Application use-case pipeline | 为幂等命令建立稳定幂等键契约 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-024 | Application use-case pipeline | 在管道中读取并返回幂等结果 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-025 | Application use-case pipeline | 仅在成功后持久化幂等结果 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-026 | Application use-case pipeline | 为事务命令建立显式标记契约 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-027 | Application use-case pipeline | 事务命令缺少 UnitOfWork 时返回结构化错误 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-028 | Application use-case pipeline | 在成功事务命令后统一提交工作单元 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-029 | Application use-case pipeline | 在用例边界记录成功失败和耗时 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-030 | Application use-case pipeline | 在用例边界传播关联标识与取消令牌 | Verified | `JayTom.Dws.Application/UseCases/ApplicationCommandPipeline.cs` | `JayTom.Dws.Tests/Application/ApplicationCommandPipelineTests.cs` |
| S2-031 | Domain model | 为包裹聚合引入强类型 PackageId | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-032 | Domain model | 将包裹生命周期转换封装在聚合内 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-033 | Domain model | 将条码赋值规则封装为领域行为 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-034 | Domain model | 将重量和尺寸表达为不可变值对象 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-035 | Domain model | 以不可变领域事件记录包裹状态变化 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-036 | Domain model | 为分拣规则建立强类型匹配模型 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-037 | Domain model | 为出口锁建立独立聚合边界 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-038 | Domain model | 为设备身份建立不可变 DeviceDescriptor | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-039 | Domain model | 为授权状态建立 LicenseAggregate | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-040 | Domain model | 以整数饱和算法实现领域重试策略 | Verified | `JayTom.Dws.Domain/Packages/PackageAggregate.cs` | `JayTom.Dws.Tests/Domain/StageTwoDomainModelTests.cs` |
| S2-041 | Stable contracts and results | 用 OperationResult 统一预期失败返回 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-042 | Stable contracts and results | 以稳定错误码替代异常文本判断 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-043 | Stable contracts and results | 保持结果错误对象不可变 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-044 | Stable contracts and results | 为二进制资产建立强类型引用 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-045 | Stable contracts and results | 为相机帧建立显式所有权租约 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-046 | Stable contracts and results | 为相机能力建立只读能力快照 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-047 | Stable contracts and results | 为相机工厂建立平台无关契约 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-048 | Stable contracts and results | 为插件事件建立强类型 EventArgs | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-049 | Stable contracts and results | 从插件公共契约移除 Exception 对象 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-050 | Stable contracts and results | 为外部调用保留取消令牌契约 | Verified | `JayTom.Dws.Abstractions/Results/OperationResult.cs` | `JayTom.Dws.Tests/Application/ResultContractTests.cs` |
| S2-051 | Persistence and assets | 建立数据库外二进制资产存储端口 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-052 | Persistence and assets | 将声音内容从 EF 映射中移除 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-053 | Persistence and assets | 将物流图标内容从 EF 映射中移除 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-054 | Persistence and assets | 数据库只保存稳定资产引用 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-055 | Persistence and assets | 按受控分类目录保存二进制资产 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-056 | Persistence and assets | 使用临时文件和原子替换写入资产 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-057 | Persistence and assets | 限制单个资产最大写入尺寸 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-058 | Persistence and assets | 限制调用方读取资产的最大尺寸 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-059 | Persistence and assets | 阻止资产引用逃逸受控根目录 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-060 | Persistence and assets | 在默认配置与输出流程接入资产存储 | Verified | `JayTom.Dws.Infrastructure/Storage/FileBinaryAssetStore.cs` | `JayTom.Dws.Tests/Infrastructure/FileBinaryAssetStoreTests.cs` |
| S2-061 | Database evolution | 区分 SQLite 空库基线与旧库升级 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-062 | Database evolution | 空库按当前 EF 模型一次性建表 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-063 | Database evolution | 空库创建后写入完整迁移历史 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-064 | Database evolution | 避免对当前模型重复执行加列迁移 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-065 | Database evolution | 旧库升级前探测可选模块表 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-066 | Database evolution | 旧库升级前探测目标列是否存在 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-067 | Database evolution | 以幂等方式增加声音资产引用列 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-068 | Database evolution | 以幂等方式增加物流声音引用列 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-069 | Database evolution | 以幂等方式增加物流图标引用列 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-070 | Database evolution | 保留旧 INTEGER REAL 数据库原位兼容 | Verified | `JayTom.Dws.Infrastructure/Migrations/SqliteSchemaMigrator.cs` | `JayTom.Dws.Tests/Persistence/SqliteCompatibilityTests.cs` |
| S2-071 | Configuration and secrets | 建立版本化配置包络 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-072 | Configuration and secrets | 建立配置版本迁移运行器 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-073 | Configuration and secrets | 建立配置模块与节描述目录 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-074 | Configuration and secrets | 在应用层集中执行配置校验 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-075 | Configuration and secrets | 通过 UnitOfWork 原子提交配置 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-076 | Configuration and secrets | 建立 ISecretStore 敏感配置端口 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-077 | Configuration and secrets | 使用 AES-256-GCM 加密敏感配置 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-078 | Configuration and secrets | 从外部密钥提供器读取配置密钥 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-079 | Configuration and secrets | 使用原子文件替换保存密文 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-080 | Configuration and secrets | 对无效密钥引用返回结构化错误 | Verified | `JayTom.Dws.Infrastructure/Configuration/EncryptedFileSecretStore.cs` | `JayTom.Dws.Tests/Infrastructure/EncryptedFileSecretStoreTests.cs` |
| S2-081 | Messaging and events | 为集成事件建立版本化包络 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-082 | Messaging and events | 为集成事件建立分区序号 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-083 | Messaging and events | 为集成事件携带 CorrelationId | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-084 | Messaging and events | 定义事务 Outbox 写入端口 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-085 | Messaging and events | 定义 Outbox 有界领取端口 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-086 | Messaging and events | 定义幂等 Inbox 开始处理端口 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-087 | Messaging and events | 定义 Inbox 完成标记端口 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-088 | Messaging and events | 定义死信持久化端口 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-089 | Messaging and events | 定义死信可控重放端口 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-090 | Messaging and events | 保持异步事件订阅有界且可释放 | Verified | `JayTom.Dws.Application/Messaging/IReliableMessagingStore.cs` | `JayTom.Dws.Tests/Application/EventAggregatorTests.cs` |
| S2-091 | Client service composition | 将生产后台服务清单集中到组合模块 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-092 | Client service composition | 从生产注册中排除测试后台服务 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-093 | Client service composition | 将启动顺序显式编码到服务描述 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-094 | Client service composition | 将停止顺序固定为依赖逆序 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-095 | Client service composition | 用 ApplicationLifecycleCoordinator 缩减 App 启动职责 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-096 | Client service composition | 用 HostedServiceSupervisor 统一监督后台服务 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-097 | Client service composition | 后台服务重启使用新实例 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-098 | Client service composition | 后台故障重启应用有界退避 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-099 | Client service composition | 稳定运行窗口后重置失败计数 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-100 | Client service composition | 将平台适配器注册集中到单一入口 | Verified | `JayTom.Dws.Client/Composition/HostedWorkflowRegistration.cs` | `JayTom.Dws.Tests/Architecture/CompositionBoundaryTests.cs` |
| S2-101 | ViewModel boundaries | 禁止 ViewModel 直接依赖 DbContext | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-102 | ViewModel boundaries | 禁止 ViewModel 直接依赖仓储契约 | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-103 | ViewModel boundaries | 禁止 ViewModel 直接构造设备实现 | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-104 | ViewModel boundaries | 列表操作统一忙碌与取消状态 | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-105 | ViewModel boundaries | 批量操作统一重入保护 | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-106 | ViewModel boundaries | 导航参数改用强类型 NavigationRequest | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-107 | ViewModel boundaries | 对话交互统一通过 UserDialogService | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-108 | ViewModel boundaries | 磁盘信息改由 IDiskInventory 端口提供 | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-109 | ViewModel boundaries | 属性变更统一通过 SetProperty | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-110 | ViewModel boundaries | 建立 ViewModel 文件规模回归预算 | Verified | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` | `JayTom.Dws.Tests/Architecture/PresentationArchitectureTests.cs` |
| S2-111 | WPF presentation ownership | 将 WindowsAction 物理归还 Client | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-112 | WPF presentation ownership | 将 WindowsActionType 物理归还 Client | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-113 | WPF presentation ownership | Application 不再拥有窗口操作事件 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-114 | WPF presentation ownership | 业务事件与窗口事件分开目录 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-115 | WPF presentation ownership | 窗口事件只被桌面进程消费 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-116 | WPF presentation ownership | UI 线程切换集中封装到 UiThread | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-117 | WPF presentation ownership | 区域导航键集中为稳定常量 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-118 | WPF presentation ownership | 页面目标集中为稳定常量 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-119 | WPF presentation ownership | WPF 对话实现隐藏在展示服务后 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-120 | WPF presentation ownership | 用架构测试锁定事件文件单类型原则 | Verified | `JayTom.Dws.Client/Events/WindowsAction.cs` | `JayTom.Dws.Tests/Architecture/EventOwnershipArchitectureTests.cs` |
| S2-121 | External integrations | 将集成公共命名空间统一为 Integrations.Contracts | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-122 | External integrations | 将供应商实现统一归入 Integrations 适配命名空间 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-123 | External integrations | 外部 HTTP 客户端使用集中命名 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-124 | External integrations | 外部参数使用不可变契约 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-125 | External integrations | 集中校验集成韧性选项 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-126 | External integrations | 统一集成超时策略 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-127 | External integrations | 仅对幂等请求启用自动重试 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-128 | External integrations | 集中实现断路保护 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-129 | External integrations | 外部参数日志统一递归脱敏 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-130 | External integrations | 使用无真实网络沙箱测试集成边界 | Verified | `JayTom.Dws.Integrations.Contracts/JayTom.Dws.Integrations.Contracts.csproj` | `JayTom.Dws.Tests/Architecture/IntegrationArchitectureTests.cs` |
| S2-131 | Camera and OCR | 相机公共契约移除 System.Drawing | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-132 | Camera and OCR | 相机公共契约移除厂商 SDK 类型 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-133 | Camera and OCR | 相机帧通过 ReadOnlyMemory 暴露数据 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-134 | Camera and OCR | 相机帧租约实现幂等释放 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-135 | Camera and OCR | 相机帧校验正长度 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-136 | Camera and OCR | 相机帧校验正宽高 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-137 | Camera and OCR | 相机帧校验有效步幅 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-138 | Camera and OCR | 相机帧通道显式声明容量 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-139 | Camera and OCR | 相机帧通道显式声明背压策略 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-140 | Camera and OCR | 旧 NVR 适配实现隔离到 Legacy 命名空间 | Verified | `JayTom.Dws.Camera.Contracts/CameraContracts.cs` | `JayTom.Dws.Tests/CameraContractsTests.cs` |
| S2-141 | Plugin runtime security | 插件契约与运行时实现物理分离 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-142 | Plugin runtime security | 插件包加载前校验清单大小 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-143 | Plugin runtime security | 插件程序集加载前校验 SHA-256 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-144 | Plugin runtime security | 插件包使用 RSA-PSS SHA-256 验签 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-145 | Plugin runtime security | 插件签名覆盖全部安全相关清单字段 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-146 | Plugin runtime security | 插件签名字段使用规范化 JSON | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-147 | Plugin runtime security | 插件信任根由宿主外部注入 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-148 | Plugin runtime security | 插件权限必须命中宿主允许清单 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-149 | Plugin runtime security | 插件签名密钥支持撤销 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-150 | Plugin runtime security | 插件密钥标识拒绝路径穿越字符 | Verified | `JayTom.Dws.Plugin.Runtime/PluginPackageVerifier.cs` | `JayTom.Dws.Tests/Plugin/PluginPackageVerifierTests.cs` |
| S2-151 | Concurrency and resources | 有序分发器使用有界 Channel | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-152 | Concurrency and resources | 非阻塞入队显式返回背压结果 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-153 | Concurrency and resources | 无损工作队列使用等待式背压 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-154 | Concurrency and resources | 异步订阅处理器支持释放 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-155 | Concurrency and resources | 释放时停止接受新事件 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-156 | Concurrency and resources | 后台任务异常统一被观察 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-157 | Concurrency and resources | 仓储信号量使用共享租约 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-158 | Concurrency and resources | 信号量租约释放保持幂等 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-159 | Concurrency and resources | 单调截止时间避免墙上时钟跳变 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-160 | Concurrency and resources | 重试次数与稳定窗口分别建模 | Verified | `JayTom.Dws.Application/Workflows/NonBlockingOrderedDispatcher.cs` | `JayTom.Dws.Tests/NonBlockingOrderedDispatcherTests.cs` |
| S2-161 | License security | 许可证改用版本化签名包络 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-162 | License security | 许可证使用 RSA-PSS SHA-256 签名 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-163 | License security | 生产验证只接受外部公钥信任根 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-164 | License security | 许可证生成只接受显式私钥 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-165 | License security | 许可证支持 SigningKeyId 轮换 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-166 | License security | 许可证支持已撤销密钥集合 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-167 | License security | 许可证密钥标识拒绝路径穿越 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-168 | License security | 许可证文件限制最大读取尺寸 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-169 | License security | 机器标识使用稳定 SHA-256 摘要 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-170 | License security | 许可证时间校验通过 TimeProvider 注入 | Verified | `JayTom.Dws.License/LicenseManager.cs` | `JayTom.Dws.Tests/License/LicenseSecurityTests.cs` |
| S2-171 | Observability and resilience | 建立统一 ActivitySource | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-172 | Observability and resilience | 建立统一 Meter | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-173 | Observability and resilience | 统一记录操作成功计数 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-174 | Observability and resilience | 统一记录操作失败计数 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-175 | Observability and resilience | 统一记录操作耗时直方图 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-176 | Observability and resilience | 使用 AsyncLocal 传播 CorrelationId | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-177 | Observability and resilience | 结构化日志字段使用稳定常量 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-178 | Observability and resilience | 敏感字段通过统一脱敏器处理 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-179 | Observability and resilience | 重试退避设置上限并避免浮点溢出 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-180 | Observability and resilience | 核心业务时间通过可替换时钟获取 | Verified | `JayTom.Dws.Abstractions/Observability/DwsDiagnostics.cs` | `JayTom.Dws.Tests/Observability/ObservabilityContractTests.cs` |
| S2-181 | Build and repository governance | CI 使用 Release 配置构建完整解决方案 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-182 | Build and repository governance | CI 运行完整测试项目 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-183 | Build and repository governance | CI 运行代码质量守卫 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-184 | Build and repository governance | CI 发布桌面产物 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-185 | Build and repository governance | CI 验证发布产物完整性 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-186 | Build and repository governance | NuGet 版本集中到 Directory.Packages.props | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-187 | Build and repository governance | 启用 NuGet 安全审计 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-188 | Build and repository governance | 新增项目必须登记模块所有者 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-189 | Build and repository governance | 公开实现数量使用零增长预算 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-190 | Build and repository governance | 数据库质量基线只允许审计后更新 | Verified | `.github/workflows/ci.yml` | `JayTom.Dws.Tests/Architecture/EngineeringGovernanceTests.cs` |
| S2-191 | Tests and closure governance | 为第二批改造建立精确 200 项机器台账 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-192 | Tests and closure governance | 第二批台账编号必须连续且唯一 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-193 | Tests and closure governance | 第二批每项状态必须为 Verified | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-194 | Tests and closure governance | 第二批每项必须登记代码证据 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-195 | Tests and closure governance | 第二批每项必须登记自动化验证 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-196 | Tests and closure governance | 台账证据路径必须真实存在 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-197 | Tests and closure governance | 台账验证文件必须包含可执行 Fact | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-198 | Tests and closure governance | 人类可读台账必须覆盖全部编号 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-199 | Tests and closure governance | 架构策略覆盖 Camera.Contracts 与 Plugin.Runtime | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |
| S2-200 | Tests and closure governance | 全量构建测试与质量守卫作为最终关闭门 | Verified | `eng/ArchitecturePolicy.json` | `JayTom.Dws.Tests/Architecture/StageTwoImplementationRegisterTests.cs` |

## 验收入口

- 机器可读源：`eng/ArchitectureStage2Register.json`
- 台账完整性：`StageTwoImplementationRegisterTests`
- 全量验收：Release 解决方案构建、完整测试集、`JayTom.Dws.CodeQualityGuard`

