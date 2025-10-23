# JayTom.Dws 项目清理文档

## 概述
本文档描述了 JayTom.Dws 解决方案的清理和重构，以专注于核心客户端应用程序。

## 已完成的变更

### 1. 创建集中式包管理
在解决方案根目录创建了 `Directory.Packages.props` 文件，用于集中管理所有 NuGet 包版本。这确保了所有项目之间的一致性。

**优点：**
- 包版本的单一事实来源
- 更容易跨所有项目更新包
- 防止版本冲突
- 减少维护开销

**位置：** `/Directory.Packages.props`

### 2. 创建精简解决方案
创建了新的解决方案文件 `JayTom.Dws.Client.sln`，仅包含客户端应用程序所需的核心项目。

**包含的项目：**
- **核心**: JayTom.Dws.Client（主 WPF 应用程序）
- **领域**: JayTom.Dws.Data, JayTom.Dws.Domain
- **基础设施**: JayTom.Dws.Infrastructure, JayTom.Dws.Interface, JayTom.Dws.Utils
- **设备**: JayTom.Dws.Camera, JayTom.Dws.Nvr, JayTom.Dws.Ocr
- **插件**: JayTom.Dws.Plugin, JayTom.Dws.PluginInterface
- **许可证**: JayTom.Dws.License

**排除的项目：**
以下项目已从精简解决方案中移除，因为它们是：
- 测试/演示应用程序
- 客户端不需要的 API 服务
- 临时/实验性项目
- 管理/管理工具

排除项目的类别：
- 测试项目（WpfApp1, WinFormsApp1, ConsoleApp1-7 等）
- API 项目（ManagementApi, VideoApi, CloudApi, LicenseApi, PostSoapApi）
- 管理工具（ManagementStudio, UpdaterClient）
- 服务项目（Service, DataInteractionService, SystemStatusMonitorService, UploadCloudService）
- 测试/演示项目（所有相机测试、HID 测试、OCR 测试等）
- 数据库测试项目（LicenseDBTest, VideoApiDbTest, CloudApiDbTest, LicenseApiDbTest）
- 临时项目（TemporaryClient, ForTestPr）

### 3. 项目结构

```
JayTom.Dws/
├── Directory.Packages.props          # 集中式包管理
├── JayTom.Dws.sln                    # 原始完整解决方案（已保留）
├── JayTom.Dws.Client.sln             # 新的精简解决方案
├── EVENT_DRIVEN_ARCHITECTURE_PLAN.md # 迁移计划
├── PROJECT_CLEANUP.md                # 本文件
│
├── Core/
│   └── JayTom.Dws.Client/           # 主 WPF 应用程序
│
├── Domain/
│   ├── JayTom.Dws.Data/             # 数据模型
│   └── JayTom.Dws.Domain/           # 领域逻辑
│
├── Infrastructure/
│   ├── JayTom.Dws.Infrastructure/   # 核心基础设施
│   ├── JayTom.Dws.Interface/        # 服务接口
│   └── JayTom.Dws.Utils/            # 实用工具类
│
├── Device/
│   ├── JayTom.Dws.Camera/           # 相机集成
│   ├── JayTom.Dws.Nvr/              # NVR 集成
│   └── JayTom.Dws.Ocr/              # OCR 功能
│
├── Plugin/
│   ├── JayTom.Dws.Plugin/           # 插件实现
│   └── JayTom.Dws.PluginInterface/  # 插件接口
│
└── License/
    └── JayTom.Dws.License/          # 许可证管理
```

## 使用精简解决方案

### 构建项目
```bash
# 构建精简解决方案
dotnet build JayTom.Dws.Client.sln

# 在 Release 模式下构建
dotnet build JayTom.Dws.Client.sln -c Release
```

### 运行应用程序
```bash
# 运行客户端应用程序
dotnet run --project JayTom.Dws.Client/JayTom.Dws.Client.csproj
```

### 添加/更新包
要添加或更新包：

1. 在 `Directory.Packages.props` 中更新版本
2. 在项目文件中添加不带版本的包引用：
   ```xml
   <PackageReference Include="PackageName" />
   ```

## 迁移到事件驱动架构

有关计划迁移到事件驱动架构的详细信息，请参见 `EVENT_DRIVEN_ARCHITECTURE_PLAN.md`。

## 依赖关系图

```
JayTom.Dws.Client
├── JayTom.Dws.Camera
│   └── JayTom.Dws.Ocr
├── JayTom.Dws.Infrastructure
│   ├── JayTom.Dws.Data
│   ├── JayTom.Dws.Domain
│   │   ├── JayTom.Dws.Data
│   │   ├── JayTom.Dws.Interface
│   │   │   ├── JayTom.Dws.Plugin
│   │   │   └── JayTom.Dws.Utils
│   │   └── JayTom.Dws.Plugin
│   └── JayTom.Dws.Plugin
├── JayTom.Dws.Interface
│   ├── JayTom.Dws.Plugin
│   └── JayTom.Dws.Utils
├── JayTom.Dws.License
├── JayTom.Dws.Nvr
├── JayTom.Dws.PluginInterface
└── JayTom.Dws.Plugin
```

## 清理的好处

1. **简化的解决方案**：更易于导航和理解
2. **更快的构建时间**：仅构建必要的项目
3. **更好的专注**：开发团队可以专注于核心功能
4. **更容易入门**：新开发人员可以快速理解项目结构
5. **降低复杂性**：减少认知负担
6. **一致的包**：集中式包管理可防止版本冲突

## 原始解决方案

原始的 `JayTom.Dws.sln` 文件已被保留，仍可用于：
- 构建 API 服务
- 运行测试
- 使用管理工具
- 向后兼容

## 后续步骤

1. 审查精简解决方案并确保所有必需的功能都存在
2. 开始实施事件驱动架构（参见 EVENT_DRIVEN_ARCHITECTURE_PLAN.md）
3. 更新 CI/CD 管道以使用新的精简解决方案
4. 更新开发者文档
5. 考虑归档或删除未使用的测试项目

## 问题或疑问

如果您对此清理有疑问或需要访问排除的项目，请：
1. 检查核心项目中是否存在该功能
2. 查看原始 `JayTom.Dws.sln` 以获取完整项目列表
3. 联系开发团队

## 更新日志

### 2025-10-23
- 创建了用于集中式包管理的 `Directory.Packages.props`
- 创建了 `JayTom.Dws.Client.sln` 精简解决方案
- 记录了清理过程
- 创建了事件驱动架构迁移计划
