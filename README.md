# JayTom.Dws - 动态称重扫码系统 (Dynamic Weighing Scanning System)

## 项目概述 (Project Overview)

JayTom.Dws 是一个基于 .NET 的企业级动态称重扫码系统(DWS - Dimensioning Weighing Scanning)，集成了多种硬件设备（相机、称重设备、扫码器等）和云端服务，提供完整的物流包裹测量、称重、扫码解决方案。

## 系统架构 (System Architecture)

### 核心架构层次 (Core Architecture Layers)

```
┌─────────────────────────────────────────────────────────────────┐
│                        客户端层 (Client Layer)                   │
├─────────────────────────────────────────────────────────────────┤
│  • JayTom.Dws.Client           - 主客户端应用 (WPF)              │
│  • JayTom.Dws.ManagementStudio - 管理控制台                      │
│  • JayTom.Dws.UpdaterClient    - 自动更新客户端                  │
│  • JayTom.Dws.LicenseClient    - 许可证管理客户端                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        API服务层 (API Layer)                     │
├─────────────────────────────────────────────────────────────────┤
│  • JayTom.Dws.ManagementApi    - 管理API服务                    │
│  • JayTom.Dws.CloudApi         - 云端API服务                    │
│  • JayTom.Dws.LicenseApi       - 许可证API服务                  │
│  • JayTom.Dws.VideoApi         - 视频流API服务                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      业务逻辑层 (Business Layer)                 │
├─────────────────────────────────────────────────────────────────┤
│  • JayTom.Dws.Application      - 应用服务层                     │
│  • JayTom.Dws.Domain           - 领域模型和服务                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    基础设施层 (Infrastructure Layer)             │
├─────────────────────────────────────────────────────────────────┤
│  • JayTom.Dws.Infrastructure   - 数据访问和基础设施             │
│  • JayTom.Dws.Data             - 数据实体和上下文                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      设备和工具层 (Device & Utils)               │
├─────────────────────────────────────────────────────────────────┤
│  • JayTom.Dws.Device           - 设备驱动和管理                 │
│  • JayTom.Dws.Camera           - 相机SDK集成                    │
│  • JayTom.Dws.Ocr              - OCR识别服务                    │
│  • JayTom.Dws.Nvr              - 视频录像和管理                 │
│  • JayTom.Dws.Utils            - 工具类库                       │
│  • JayTom.Dws.License          - 许可证管理                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      插件系统 (Plugin System)                    │
├─────────────────────────────────────────────────────────────────┤
│  • JayTom.Dws.PluginInterface  - 插件接口定义                   │
│  • JayTom.Dws.Plugin           - 插件实现基类                   │
│  • Plugin Modules:                                              │
│    - DashboardPagePlugin       - 仪表板页面插件                 │
│    - DataValidationLogicPlugin - 数据验证逻辑插件               │
│    - PrinterDevicePlugin       - 打印机设备插件                 │
│    - UploadApiPlugin           - 上传API插件                    │
│    - HomeToolPlugin            - 主页工具插件                   │
└─────────────────────────────────────────────────────────────────┘
```

### 支持的硬件设备 (Supported Hardware)

- **工业相机** (Industrial Cameras)
  - 海康威视工业相机 (Hikvision Industrial Camera)
  - 海康威视智能相机 (Hikvision Smart Camera)
  - 华睿科技相机 (Huaraytech)
  - Irayple 3D相机
  - USB相机
  - Percipio 3D相机
  - Wayzim相机

- **OCR识别** (OCR Recognition)
  - 百度OCR (Baidu OCR)
  - ONNX模型推理
  - YOLO目标检测

- **NVR视频录像** (NVR Video Recording)
  - 大华NVR集成 (Dahua NVR)
  - VLC视频流支持

## 架构优化建议 (Architecture Optimization Recommendations)

### 1. 代码组织和结构 (Code Organization)

#### 当前问题 (Current Issues)
- ✗ 测试项目数量过多且分散（超过70个项目，其中很多是临时测试项目）
- ✗ 存在大量Console、WPF、WinForms测试项目混杂在主解决方案中
- ✗ 项目命名不统一（如ConsoleApp1-6、WpfApp1-3等临时命名）
- ✗ 存在重复的备份文件（如`JayTom - Backup.Dws.LicenseClient.csproj`）

#### 优化建议 (Recommendations)
- ✓ **整合测试项目**：将所有测试项目统一迁移到独立的 `tests/` 文件夹
  - 按功能模块组织：`tests/Camera/`, `tests/OCR/`, `tests/API/` 等
  - 删除或归档不再使用的临时测试项目
  - 统一测试项目命名规范：`JayTom.Dws.[Module].Tests`
  
- ✓ **分离示例和测试代码**：创建 `samples/` 文件夹存放演示项目
  
- ✓ **清理冗余文件**：删除备份文件和未使用的项目

### 2. 依赖管理 (Dependency Management)

#### 当前问题 (Current Issues)
- ✗ 多个项目可能使用不同版本的相同NuGet包
- ✗ 缺少中央依赖版本管理
- ✗ 项目间依赖关系复杂

#### 优化建议 (Recommendations)
- ✓ **实施中央包管理**：使用 `Directory.Build.props` 和 `Directory.Packages.props`
  ```xml
  <!-- Directory.Packages.props -->
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="7.0.0" />
  </ItemGroup>
  ```
  
- ✓ **依赖分析**：定期运行依赖分析工具，识别未使用的包和循环依赖

### 3. 数据库和数据访问 (Database and Data Access)

#### 当前问题 (Current Issues)
- ✗ 多个数据库上下文（SqliteContext, SqliteConfContext, SqliteLogsContext, CloudApiContext等）
- ✗ 数据访问逻辑分散在多个层
- ✗ 缺少统一的数据迁移策略

#### 优化建议 (Recommendations)
- ✓ **统一数据访问层**：实现Repository和Unit of Work模式
- ✓ **数据库迁移管理**：使用EF Core Migrations统一管理所有数据库变更
- ✓ **读写分离**：对于高频读取场景，考虑实现CQRS模式
- ✓ **连接池优化**：配置合理的数据库连接池参数

### 4. 插件系统 (Plugin System)

#### 当前问题 (Current Issues)
- ✗ 插件接口过多（14个不同的IPlugin接口）
- ✗ 插件加载和生命周期管理不够清晰
- ✗ 插件间通信机制需要改进

#### 优化建议 (Recommendations)
- ✓ **简化插件接口**：合并相似的插件接口，使用泛型和组合模式
- ✓ **插件容器化**：使用MEF或自定义插件容器管理插件生命周期
- ✓ **插件沙箱**：实现插件隔离机制，防止插件影响主程序稳定性
- ✓ **插件市场**：建立插件版本管理和分发机制

### 5. API服务架构 (API Service Architecture)

#### 当前问题 (Current Issues)
- ✗ 多个独立的API服务缺少统一的网关
- ✗ API版本管理不明确
- ✗ 缺少统一的认证和授权策略
- ✗ API文档可能不完整

#### 优化建议 (Recommendations)
- ✓ **API网关**：引入API Gateway（如Ocelot、YARP）统一管理所有API服务
- ✓ **API版本控制**：实施明确的API版本策略（URL版本化或Header版本化）
- ✓ **统一认证**：实现基于JWT的统一身份认证和授权
- ✓ **API文档**：集成Swagger/OpenAPI，自动生成API文档
- ✓ **服务发现**：引入Consul或Eureka实现服务注册和发现

### 6. 性能优化 (Performance Optimization)

#### 优化建议 (Recommendations)
- ✓ **异步编程**：全面使用async/await模式，避免阻塞调用
- ✓ **缓存策略**：实现多级缓存（内存缓存、分布式缓存）
  - 相机配置缓存
  - API响应缓存
  - OCR结果缓存（对于相同图像）
- ✓ **图像处理优化**：
  - 使用GPU加速图像处理
  - 实现图像压缩和缩略图生成
  - 异步处理大图像
- ✓ **数据库查询优化**：
  - 添加适当的索引
  - 使用查询分页
  - 实现数据预加载（Eager Loading）

### 7. 日志和监控 (Logging and Monitoring)

#### 当前问题 (Current Issues)
- ✗ 日志配置分散在多个配置文件（Nlog.config）
- ✗ 缺少统一的应用程序监控
- ✗ 错误跟踪和诊断机制不完善

#### 优化建议 (Recommendations)
- ✓ **集中式日志**：使用ELK Stack（Elasticsearch, Logstash, Kibana）或Seq
- ✓ **结构化日志**：使用Serilog实现结构化日志记录
- ✓ **分布式追踪**：引入OpenTelemetry或Jaeger进行分布式追踪
- ✓ **应用监控**：集成Application Insights或Prometheus + Grafana
- ✓ **健康检查**：为所有API服务添加健康检查端点

### 8. 安全性 (Security)

#### 优化建议 (Recommendations)
- ✓ **敏感数据加密**：加密配置文件中的敏感信息（数据库连接串、API密钥）
- ✓ **输入验证**：在所有API端点实施严格的输入验证
- ✓ **HTTPS强制**：所有API通信强制使用HTTPS
- ✓ **权限管理**：实现细粒度的基于角色的访问控制(RBAC)
- ✓ **审计日志**：记录所有关键操作的审计日志

### 9. 容器化和部署 (Containerization and Deployment)

#### 优化建议 (Recommendations)
- ✓ **Docker化**：为所有API服务创建Docker镜像
- ✓ **容器编排**：使用Docker Compose或Kubernetes进行服务编排
- ✓ **CI/CD管道**：建立自动化构建、测试和部署流程
- ✓ **配置管理**：使用环境变量和配置中心管理不同环境的配置

### 10. 文档和规范 (Documentation and Standards)

#### 优化建议 (Recommendations)
- ✓ **代码规范**：制定并执行统一的C#编码规范（使用.editorconfig）
- ✓ **架构文档**：补充详细的架构设计文档
- ✓ **开发指南**：编写新开发者快速上手指南
- ✓ **API文档**：完善所有API的使用文档
- ✓ **部署文档**：提供详细的部署和运维文档

## 现存问题 (Existing Issues)

### 高优先级 (High Priority)

1. **项目结构混乱**
   - 问题：70+个项目中包含大量临时测试项目
   - 影响：难以维护，新开发者上手困难
   - 建议：立即进行项目整理和重组

2. **缺少统一的异常处理**
   - 问题：各模块异常处理不一致
   - 影响：错误信息不统一，难以追踪问题
   - 建议：实现全局异常处理中间件

3. **数据库上下文过多**
   - 问题：多个独立的DbContext可能导致数据不一致
   - 影响：事务管理复杂，性能开销大
   - 建议：考虑合并相关的上下文或使用分布式事务

4. **缺少单元测试和集成测试**
   - 问题：虽有大量测试项目，但缺少规范的单元测试
   - 影响：代码质量难以保证，重构风险高
   - 建议：建立完善的测试框架和测试覆盖率要求

### 中优先级 (Medium Priority)

5. **配置管理分散**
   - 问题：配置分散在多个appsettings.json和config文件中
   - 影响：配置难以统一管理和更新
   - 建议：使用配置中心（如Consul、Nacos）

6. **插件系统复杂**
   - 问题：14个不同的插件接口增加了复杂度
   - 影响：插件开发门槛高，维护困难
   - 建议：简化和规范化插件接口

7. **缺少API限流和熔断**
   - 问题：API服务缺少保护机制
   - 影响：在高负载下可能导致系统崩溃
   - 建议：引入Polly库实现限流、熔断和重试策略

8. **硬件驱动版本管理**
   - 问题：多个相机SDK的版本和兼容性管理
   - 影响：升级困难，可能出现兼容性问题
   - 建议：建立硬件抽象层，统一驱动接口

### 低优先级 (Low Priority)

9. **国际化支持不完整**
   - 问题：部分界面和消息可能未国际化
   - 影响：多语言支持不完整
   - 建议：完善资源文件和多语言支持

10. **移动端支持**
    - 问题：当前主要是桌面应用
    - 影响：无法在移动设备上使用
    - 建议：考虑开发移动端管理应用（Xamarin或MAUI）

## 未完善的功能 (Incomplete Features)

### 核心功能 (Core Features)

1. **实时数据同步**
   - 状态：部分实现
   - 缺失：缺少断线重连机制和数据一致性保证
   - 计划：实现可靠的消息队列机制（RabbitMQ或Kafka）

2. **设备自动发现**
   - 状态：需要手动配置
   - 缺失：自动发现网络中的相机和其他设备
   - 计划：实现基于mDNS或UPnP的设备发现

3. **数据备份和恢复**
   - 状态：基本功能
   - 缺失：自动备份调度、增量备份、灾难恢复
   - 计划：实现完整的备份恢复策略

4. **负载均衡**
   - 状态：未实现
   - 缺失：多实例部署时的负载均衡
   - 计划：引入负载均衡器（Nginx或HAProxy）

### 增强功能 (Enhancement Features)

5. **AI智能识别**
   - 状态：基础OCR已实现
   - 缺失：更智能的包裹识别、异常检测
   - 计划：集成更先进的AI模型

6. **数据分析和报表**
   - 状态：基础功能
   - 缺失：丰富的数据可视化和统计分析
   - 计划：开发BI报表模块

7. **远程诊断和维护**
   - 状态：部分支持
   - 缺失：远程控制、远程配置更新
   - 计划：实现完整的远程管理功能

8. **第三方系统集成**
   - 状态：部分API支持
   - 缺失：与WMS、TMS等系统的标准接口
   - 计划：提供标准化的集成接口和SDK

### 运维功能 (Operations Features)

9. **自动化部署**
   - 状态：手动部署
   - 缺失：自动化CI/CD流程
   - 计划：建立完整的DevOps流程

10. **性能监控和告警**
    - 状态：基础日志
    - 缺失：实时性能监控、智能告警
    - 计划：集成APM工具

11. **配置热更新**
    - 状态：需要重启
    - 缺失：运行时配置更新能力
    - 计划：实现配置热加载机制

12. **多租户支持**
    - 状态：单租户
    - 缺失：SaaS模式的多租户架构
    - 计划：设计和实现多租户隔离机制

## 技术栈 (Technology Stack)

- **框架**: .NET (C#)
- **UI框架**: WPF, Blazor (MudBlazor)
- **数据库**: SQLite, SQL Server
- **ORM**: Entity Framework Core
- **日志**: NLog
- **通信**: SignalR, HTTP/REST API
- **图像处理**: OpenCV, ONNX Runtime
- **视频处理**: FFmpeg, VLC
- **OCR**: 百度OCR SDK

## 开发路线图 (Development Roadmap)

### 短期目标 (Q1 2024)
- [ ] 项目结构重组和清理
- [ ] 实施中央包管理
- [ ] 建立单元测试框架
- [ ] 完善API文档

### 中期目标 (Q2-Q3 2024)
- [ ] 实现API网关
- [ ] 引入分布式追踪
- [ ] 实施缓存策略
- [ ] 容器化所有服务

### 长期目标 (Q4 2024及以后)
- [ ] 实现多租户支持
- [ ] 开发移动端应用
- [ ] AI模型升级
- [ ] 实施微服务架构

## 快速开始 (Quick Start)

### 前置要求
- Visual Studio 2022 或更高版本
- .NET 6.0 SDK 或更高版本
- SQLite
- 支持的相机硬件（可选）

### 构建和运行
```bash
# 克隆仓库
git clone https://github.com/Hisoka6602/JayTom.Dws.git

# 打开解决方案
cd JayTom.Dws
start JayTom.Dws.sln

# 构建解决方案
dotnet build

# 运行主客户端
dotnet run --project JayTom.Dws.Client
```

## 贡献指南 (Contributing)

欢迎贡献代码、报告问题和提出建议。请参阅贡献指南了解详情。

## 许可证 (License)

请参阅LICENSE文件了解许可证信息。

## 联系方式 (Contact)

如有问题或建议，请通过以下方式联系：
- 创建Issue: [GitHub Issues](https://github.com/Hisoka6602/JayTom.Dws/issues)
- 项目主页: [GitHub Repository](https://github.com/Hisoka6602/JayTom.Dws)

---

**最后更新时间**: 2024年10月
**文档版本**: 1.0.0
