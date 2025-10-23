# JayTom.Dws - 动态称重系统

## 概述
JayTom.Dws 是一个综合性的动态称重系统，集成了相机、OCR 和设备管理功能。该系统使用 WPF 构建，遵循模块化架构。

## 快速开始

### 前置要求
- .NET 7.0 SDK 或更高版本
- Windows 10/11（用于 WPF 应用程序）
- Visual Studio 2022 或更高版本（推荐）

### 构建解决方案

#### 精简解决方案（推荐用于客户端开发）
```bash
# 构建客户端精简解决方案
dotnet build JayTom.Dws.Client.sln

# 运行客户端应用程序
dotnet run --project JayTom.Dws.Client/JayTom.Dws.Client.csproj
```

#### 完整解决方案（所有项目）
```bash
# 构建完整解决方案
dotnet build JayTom.Dws.sln
```

## 项目结构

### 核心项目
- **JayTom.Dws.Client**: 主 WPF 客户端应用程序
- **JayTom.Dws.Data**: 数据模型和实体
- **JayTom.Dws.Domain**: 领域逻辑和业务规则
- **JayTom.Dws.Infrastructure**: 核心基础设施服务
- **JayTom.Dws.Interface**: 服务接口和契约
- **JayTom.Dws.Utils**: 实用工具类和帮助程序

### 设备集成
- **JayTom.Dws.Camera**: 相机集成（海康威视、大华、USB 相机）
- **JayTom.Dws.Nvr**: 用于视频录制的 NVR 集成
- **JayTom.Dws.Ocr**: 图像处理的 OCR 功能

### 插件系统
- **JayTom.Dws.PluginInterface**: 插件接口
- **JayTom.Dws.Plugin**: 插件实现

### 安全性
- **JayTom.Dws.License**: 许可证管理和验证

## 包管理

### 集中式包版本管理
本项目通过 `Directory.Packages.props` 使用集中式包管理。所有包版本都在此文件中**统一定义**，确保：
- ✅ 项目间版本一致性
- ✅ 避免版本冲突
- ✅ 简化依赖管理
- ✅ 便于升级维护

### 正确使用方法
**✅ 正确做法**：
```xml
<!-- 1. 在 Directory.Packages.props 中定义版本 -->
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

<!-- 2. 在项目 .csproj 中引用（不指定版本） -->
<PackageReference Include="Newtonsoft.Json" />
```

**❌ 错误做法**：
```xml
<!-- 不要在 .csproj 中指定版本，这会违反集中式管理原则 -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### 添加新包的步骤
1. 在 `Directory.Packages.props` 中添加 `<PackageVersion>` 条目并指定版本
2. 在需要的项目 `.csproj` 文件中添加 `<PackageReference>`（无需 Version 属性）
3. 运行 `dotnet restore` 恢复包

## 文档

### 核心文档
- **[架构文档](ARCHITECTURE.md)**: 完整的系统架构设计、分层说明和技术栈（必读）
- **[事件驱动架构计划](EVENT_DRIVEN_ARCHITECTURE_PLAN.md)**: 迁移到事件驱动架构的计划
- **[项目清理文档](PROJECT_CLEANUP.md)**: 解决方案清理和重构的详细信息

### 历史文档
- **[重构建议](REFACTORING_RECOMMENDATIONS.md)**: 通用重构建议
- **[客户端优化完成](CLIENT_OPTIMIZATION_COMPLETE.md)**: 客户端优化历史
- **[性能问题总结](PERFORMANCE_ISSUES_SUMMARY.md)**: 性能问题和解决方案
- **[迁移指南](MIGRATION_GUIDE.md)**: 迁移指南
- **[实现总结](IMPLEMENTATION_SUMMARY.md)**: 实现总结
- **[优化说明](README_OPTIMIZATION.md)**: 优化说明

## 主要特性
- 实时重量测量
- 多相机支持（工业相机、IP 相机、USB 相机）
- 条形码扫描集成
- 图像文字识别 OCR
- 可扩展的插件系统
- 数据导出功能（Excel、PDF）
- 云同步
- 多语言支持

## 架构
项目采用清晰的分层架构设计，遵循依赖倒置原则和关注点分离：

### 核心分层
- **表现层** (`JayTom.Dws.Client`): WPF 客户端应用程序
- **应用层** (`JayTom.Dws.Application`): 应用服务和业务用例编排
- **领域层** (`JayTom.Dws.Domain`): 核心业务逻辑和领域模型
- **基础设施层** (`JayTom.Dws.Infrastructure`): 数据访问、外部服务集成
- **数据层** (`JayTom.Dws.Data`): 数据模型和实体定义

### 专用层
- **设备集成层**: 硬件设备集成（相机、NVR、OCR、通用设备）
- **插件系统层**: 可扩展的插件架构
- **横切关注点层**: 日志、工具、许可证、接口服务

### 架构特点
✅ **零边界入侵**: 核心层完全独立，无基础设施依赖  
✅ **清晰命名**: 统一命名规范，职责明确  
✅ **依赖倒置**: 高层不依赖低层具体实现  
✅ **关注点分离**: 每个项目职责单一清晰  

**详细架构文档**: 查看 [ARCHITECTURE.md](ARCHITECTURE.md) 了解完整的架构设计、依赖关系和技术栈。

### 未来：事件驱动架构
项目计划迁移到事件驱动架构以进一步改善可扩展性、可维护性和松耦合特性。详见 [事件驱动架构计划](EVENT_DRIVEN_ARCHITECTURE_PLAN.md)。

## 开发工作流

### 分支策略
- `main`: 生产就绪代码
- `develop`: 开发分支
- `feature/*`: 功能分支
- `bugfix/*`: 错误修复分支

### 代码标准
- 遵循 C# 编码规范
- 对 I/O 操作使用 async/await
- 实现适当的错误处理和日志记录
- 为业务逻辑编写单元测试

## 配置
应用程序使用多个配置文件：
- `appsettings.json`: 应用程序设置
- `App.config`: 旧版配置
- `Nlog.config`: 日志配置

## 日志记录
应用程序使用 NLog 进行日志记录。日志写入到：
- 控制台（开发期间）
- 文件（生产环境）
- 数据库（关键错误）

## 测试
```bash
# 运行所有测试
dotnet test

# 运行测试并生成覆盖率
dotnet test /p:CollectCoverage=true
```

## 部署
（待添加）

## 故障排除

### 常见问题
1. **相机无法连接**: 检查设备驱动程序和权限
2. **许可证验证失败**: 验证许可证文件位置
3. **数据库连接错误**: 检查配置中的连接字符串

### 日志
在 `Logs/` 目录中查看日志以获取详细的错误信息。

## 贡献
（待添加）

## 许可证
（待添加）

## 支持
如有问题和疑问，请联系开发团队。

## 当前项目状态

### 项目结构
截至 2025-10-23，项目已完成重大清理和重组：

#### 活跃的核心项目（17个）
1. **JayTom.Dws.Client** - 主 WPF 客户端应用程序
2. **JayTom.Dws.Application** - 应用层服务
3. **JayTom.Dws.Camera** - 相机集成模块
4. **JayTom.Dws.CrossCutting** - 横切关注点
5. **JayTom.Dws.Data** - 数据模型和实体
6. **JayTom.Dws.Device** - 设备管理
7. **JayTom.Dws.Domain** - 领域逻辑和业务规则
8. **JayTom.Dws.Infrastructure** - 基础设施服务
9. **JayTom.Dws.Interface** - 服务接口和契约
10. **JayTom.Dws.License** - 许可证管理
11. **JayTom.Dws.Nvr** - NVR 视频录制集成
12. **JayTom.Dws.Ocr** - OCR 图像识别
13. **JayTom.Dws.Plugin** - 插件实现
14. **JayTom.Dws.PluginInterface** - 插件接口定义
15. **JayTom.Dws.Sunnen** - Sunnen 特定功能
16. **JayTom.Dws.SunnenPlugin** - Sunnen 插件
17. **JayTom.Dws.Utils** - 实用工具类

#### 已移除的项目
- **测试项目**：所有测试和演示项目（包括 WpfApp1、ConsoleApp1-6、各种相机测试项目等）
- **API 项目**：所有独立的 API 服务项目（包括 ManagementApi、LicenseApi、VideoApi、CloudApi 等）
- **临时项目**：临时客户端和实验性项目

### 解决方案文件
- **JayTom.Dws.sln** - 完整解决方案（仅包含 17 个核心项目）
- **JayTom.Dws.Client.sln** - 客户端精简解决方案（用于客户端开发）

## 优化计划

### 短期目标（1-3个月）

#### 1. 代码质量改进
- [ ] 为核心业务逻辑添加单元测试
- [ ] 实施代码覆盖率监控（目标：>70%）
- [ ] 统一代码风格和命名约定
- [ ] 消除代码重复和代码异味

#### 2. 性能优化
- [ ] 优化数据库查询和索引
- [ ] 实现响应式数据加载和分页
- [ ] 优化图像处理管道
- [ ] 改进内存管理和资源释放

#### 3. 架构改进
- [ ] 完成向事件驱动架构的迁移（详见 [事件驱动架构计划](EVENT_DRIVEN_ARCHITECTURE_PLAN.md)）
- [ ] 实现依赖注入容器的统一配置
- [ ] 标准化日志记录和错误处理
- [ ] 改进模块间的解耦

### 中期目标（3-6个月）

#### 1. 可测试性
- [ ] 重构紧耦合组件
- [ ] 实现集成测试框架
- [ ] 添加 UI 自动化测试
- [ ] 建立持续集成/持续部署（CI/CD）管道

#### 2. 用户体验
- [ ] 优化 UI 响应性能
- [ ] 改进错误消息和用户反馈
- [ ] 实现更好的配置管理界面
- [ ] 添加用户使用指南和帮助系统

#### 3. 功能扩展
- [ ] 增强插件系统的灵活性
- [ ] 支持更多设备类型
- [ ] 实现高级数据分析功能
- [ ] 添加云同步和备份功能

### 长期目标（6-12个月）

#### 1. 平台现代化
- [ ] 升级到 .NET 8.0 LTS
- [ ] 评估并迁移到现代 UI 框架（WinUI 3 或 Avalonia）
- [ ] 实现跨平台支持（Windows、Linux）
- [ ] 容器化部署支持

#### 2. 数据和分析
- [ ] 实现实时数据分析仪表板
- [ ] 集成机器学习模型用于预测性分析
- [ ] 大数据处理能力
- [ ] 高级报表和可视化功能

#### 3. 企业级功能
- [ ] 多租户支持
- [ ] 高可用性和故障转移
- [ ] 增强的安全性和审计
- [ ] API 网关和微服务架构

### 持续改进措施

1. **代码审查**：所有代码变更必须经过同行评审
2. **文档**：保持技术文档和用户文档的更新
3. **监控**：实施应用性能监控（APM）
4. **反馈循环**：定期收集和处理用户反馈
5. **技术债务**：每个迭代分配 20% 的时间用于技术债务清理

## 版本历史
- **2025-10-23**: 
  - ✅ **修复集中式包管理违规**: 移除所有 `.csproj` 文件中的 PackageReference Version 属性，确保版本由 `Directory.Packages.props` 统一管理
  - ✅ **完善架构文档**: 新增 `ARCHITECTURE.md`，详细说明分层架构、依赖关系和设计原则
  - ✅ **架构优化**: 验证零边界入侵原则，确保核心层独立性
  - 项目清理：移除所有测试项目和 API 项目
  - 解决方案精简：从 100+ 个项目减少到 17 个核心项目
  - 添加项目状态和优化计划文档
- 以前的版本：参见 git 历史记录
