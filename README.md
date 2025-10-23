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
本项目通过 `Directory.Packages.props` 使用集中式包管理。所有包版本都在此文件中定义，以确保项目间的一致性。

添加新包的步骤：
1. 在 `Directory.Packages.props` 中添加版本号
2. 在项目文件中引用包（无需指定版本）

示例：
```xml
<!-- 在 Directory.Packages.props 中 -->
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

<!-- 在你的 .csproj 中 -->
<PackageReference Include="Newtonsoft.Json" />
```

## 文档

### 最新变更
- **[项目清理文档](PROJECT_CLEANUP.md)**: 解决方案清理和重构的详细信息
- **[事件驱动架构计划](EVENT_DRIVEN_ARCHITECTURE_PLAN.md)**: 迁移到事件驱动架构的计划

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
项目遵循分层架构模式：
- **表现层**: WPF 客户端应用程序
- **领域层**: 业务逻辑和领域模型
- **基础设施层**: 数据访问、外部服务
- **设备层**: 硬件集成
- **插件层**: 可扩展性支持

### 未来：事件驱动架构
项目计划迁移到事件驱动架构以改善：
- 可扩展性
- 可维护性
- 可测试性
- 松耦合

详见 [事件驱动架构计划](EVENT_DRIVEN_ARCHITECTURE_PLAN.md)。

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

## 版本历史
- **2025-10-23**: 项目清理和集中式包管理
- 以前的版本：参见 git 历史记录
