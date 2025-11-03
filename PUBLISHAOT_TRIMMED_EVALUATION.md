# PublishAot 和 PublishTrimmed 适用性评估报告

## 执行摘要

本文档评估 JayTom.Dws 项目是否适合使用 .NET 的 PublishAot（提前编译）和 PublishTrimmed（代码裁剪）功能。

**结论：**
- ❌ **PublishAot (Native AOT)**: 当前项目**不适合**使用 PublishAot
- ⚠️ **PublishTrimmed (IL Trimming)**: 部分项目**可以尝试**使用 PublishTrimmed，但需要仔细测试

## 项目结构分析

### 主要可执行项目

1. **JayTom.Dws.Client** - WPF 桌面应用程序 (.NET 7.0)
2. **JayTom.Dws.ManagementStudio** - WPF 桌面应用程序 (.NET 6.0)
3. **JayTom.Dws.CloudApi** - ASP.NET Core Web API (.NET 7.0)
4. **JayTom.Dws.LicenseApi** - ASP.NET Core Web API (.NET 7.0)
5. **JayTom.Dws.ManagementApi** - ASP.NET Core Web API (.NET 6.0)
6. **JayTom.Dws.UploadCloudService** - Worker Service (.NET 7.0)
7. **MyApplication** - Blazor WebAssembly (.NET 6.0)

## PublishAot (Native AOT) 评估

### 不兼容的原因

#### 1. WPF 应用程序不受支持
- `JayTom.Dws.Client` 和 `JayTom.Dws.ManagementStudio` 都是 WPF 应用程序
- **WPF 框架不支持 Native AOT**
- WPF 依赖于大量的反射和动态类型加载

#### 2. Entity Framework Core 限制
项目使用了以下 EF Core 提供程序：
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Pomelo.EntityFrameworkCore.MySql.Json.Microsoft`

**问题：**
- EF Core 5.x 版本不支持 Native AOT
- 即使升级到 EF Core 7.0+，也需要使用编译时生成的 DbContext 和查询编译
- 当前代码使用了动态查询和表达式树

#### 3. 反射和动态代码使用

发现以下反射使用模式：
```csharp
// JayTom.Dws.Plugin/Excel/NpoiExport.cs
System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public

// JayTom.Dws.Client/ViewModels/Pages/Preferences/AppSettings/OtherSettingsViewModel.cs
System.Reflection.Assembly.GetExecutingAssembly().Location
```

#### 4. 动态代码编译
- `Microsoft.CodeAnalysis.Scripting` 在 CloudApi 项目中被使用
- 运行时代码编译和执行不支持 Native AOT

#### 5. Newtonsoft.Json 使用
- 多个项目使用 `Newtonsoft.Json`
- 虽然部分功能可以在 AOT 下工作，但需要大量的源生成器支持
- 建议迁移到 `System.Text.Json` 以获得更好的 AOT 支持

#### 6. 第三方库兼容性问题
项目使用的库可能不支持 Native AOT：
- `MaterialDesignThemes` (WPF UI 库)
- `LottieSharp` (动画库)
- `Prism.DryIoc` (MVVM 框架)
- `Vlc.DotNet.Wpf` (视频播放)
- `gong-wpf-dragdrop`

### Native AOT 兼容性矩阵

| 项目 | Native AOT 兼容性 | 原因 |
|------|------------------|------|
| JayTom.Dws.Client | ❌ 不兼容 | WPF 应用，使用反射，第三方库不支持 |
| JayTom.Dws.ManagementStudio | ❌ 不兼容 | WPF 应用 |
| JayTom.Dws.CloudApi | ❌ 不兼容 | 使用 CodeAnalysis.Scripting，EF Core 5.x |
| JayTom.Dws.LicenseApi | ⚠️ 可能兼容 | 需要重大修改（升级 EF Core，移除动态代码） |
| JayTom.Dws.ManagementApi | ⚠️ 可能兼容 | 需要重大修改 |
| JayTom.Dws.UploadCloudService | ⚠️ 可能兼容 | 取决于依赖项 |
| MyApplication | ❌ 不兼容 | Blazor WebAssembly 不需要 AOT（已经是客户端运行） |

## PublishTrimmed (IL Trimming) 评估

IL 裁剪比 Native AOT 的限制要少，但仍然需要注意：

### 可以尝试裁剪的项目

#### ✅ Web API 项目（需要测试）
- JayTom.Dws.CloudApi
- JayTom.Dws.LicenseApi
- JayTom.Dws.ManagementApi

**推荐配置：**
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  <!-- 或使用 link 模式以获得更好的压缩 -->
  <!-- <TrimMode>link</TrimMode> -->
</PropertyGroup>
```

#### ✅ Worker Service
- JayTom.Dws.UploadCloudService

### 需要注意的问题

#### 1. Entity Framework Core
- EF Core 5.x 对裁剪的支持有限
- 可能需要添加 `<TrimmerRootAssembly>` 来保留必要的程序集
- 建议升级到 EF Core 7.0+ 以获得更好的裁剪支持

#### 2. 反射和动态类型
需要使用 `DynamicallyAccessedMembers` 属性标记：
```csharp
public void ProcessType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
```

#### 3. Newtonsoft.Json
- 不完全支持裁剪
- 建议迁移到 `System.Text.Json`
- 或添加裁剪警告抑制（不推荐）

#### 4. WPF 应用程序
- ⚠️ WPF 应用程序可以尝试裁剪，但**风险很高**
- 大量的 XAML 和数据绑定依赖于反射
- **不推荐**对 WPF 项目启用裁剪

### IL Trimming 兼容性矩阵

| 项目 | 裁剪兼容性 | 推荐 TrimMode | 备注 |
|------|----------|--------------|------|
| JayTom.Dws.Client | ⚠️ 不推荐 | - | WPF 应用，XAML 绑定问题 |
| JayTom.Dws.ManagementStudio | ⚠️ 不推荐 | - | WPF 应用 |
| JayTom.Dws.CloudApi | ✅ 可以尝试 | partial | 需要测试，可能需要排除某些程序集 |
| JayTom.Dws.LicenseApi | ✅ 可以尝试 | partial | 需要测试 |
| JayTom.Dws.ManagementApi | ✅ 可以尝试 | partial | 需要测试 |
| JayTom.Dws.UploadCloudService | ✅ 可以尝试 | partial | Worker Service 通常兼容性较好 |
| MyApplication | ⚠️ 特殊情况 | - | Blazor WASM 有自己的裁剪机制 |

## 建议和行动计划

### 短期建议（可立即实施）

#### 1. 为 Web API 项目启用 PublishTrimmed（试验性）

可以在以下项目中尝试启用 IL 裁剪：
- JayTom.Dws.LicenseApi
- JayTom.Dws.ManagementApi

**步骤：**
1. 更新项目文件以启用部分裁剪
2. 运行完整的集成测试
3. 检查应用程序日志是否有裁剪警告
4. 测试所有 API 端点和功能

#### 2. 测试配置示例

创建发布配置文件 (`Properties/PublishProfiles/TrimmedRelease.pubxml`)：
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
  </PropertyGroup>
</Project>
```

### 中期建议（需要一定重构）

#### 1. 迁移到 System.Text.Json
- 将 Newtonsoft.Json 替换为 System.Text.Json
- 更好的裁剪和 AOT 支持
- 需要更新序列化/反序列化代码

#### 2. 升级 Entity Framework Core
- 从 EF Core 5.x 升级到 EF Core 7.0 或 8.0
- 利用编译时模型和查询编译
- 改善裁剪支持

#### 3. 减少反射使用
- 使用源生成器替代反射
- 使用编译时已知的类型而不是动态类型

### 长期建议（需要重大架构更改）

#### 1. 拆分动态功能
- 将使用 `Microsoft.CodeAnalysis.Scripting` 的功能隔离到单独的服务
- 核心 API 可以支持 AOT，动态脚本功能作为可选扩展

#### 2. 考虑最小 API（Minimal APIs）
- 对于简单的 API 服务，考虑使用最小 API
- 原生支持 Native AOT

#### 3. 插件系统重构
- 当前的插件系统依赖于动态加载
- 考虑使用编译时插件注册

### 不推荐的做法

❌ **不要对 WPF 应用程序启用 PublishAot**
- WPF 不支持 Native AOT
- 会导致编译失败

❌ **不要对 WPF 应用程序启用激进的 PublishTrimmed**
- XAML 绑定依赖于反射
- 很可能导致运行时错误

❌ **不要在没有充分测试的情况下启用裁剪**
- 裁剪可能导致运行时错误
- 必须有完整的测试覆盖

## 性能和大小优化的其他选项

如果 PublishAot 和 PublishTrimmed 不适用，可以考虑以下替代方案：

### 1. ReadyToRun (R2R)
```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```
- 提供提前编译的部分优势
- 兼容性好
- 启动时间更快
- 但发布包会更大

### 2. 单文件发布
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```
- 所有文件打包成一个可执行文件
- 便于分发
- 但不减少总大小

### 3. 框架依赖部署
```xml
<PropertyGroup>
  <SelfContained>false</SelfContained>
</PropertyGroup>
```
- 需要目标机器安装 .NET 运行时
- 大大减少部署大小

## 测试检查清单

如果决定启用裁剪，请执行以下测试：

- [ ] 所有 API 端点响应正确
- [ ] 数据库操作正常（CRUD）
- [ ] Entity Framework 查询工作正常
- [ ] JSON 序列化/反序列化正确
- [ ] 依赖注入容器正常工作
- [ ] SignalR 连接和消息传递正常
- [ ] 身份验证和授权工作正常
- [ ] 所有第三方库功能正常
- [ ] 日志记录正常
- [ ] 配置加载正确
- [ ] 插件系统（如果适用）正常工作

## 参考资料

- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [Trim self-contained deployments and executables](https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained)
- [Introduction to AOT warnings](https://learn.microsoft.com/dotnet/core/deploying/native-aot/fixing-warnings)
- [EF Core and trimming](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#compiled-queries)
- [ASP.NET Core Native AOT](https://learn.microsoft.com/aspnet/core/fundamentals/native-aot)

## 总结

**PublishAot**: 由于大量使用 WPF、Entity Framework Core 5.x、反射和动态代码编译，当前项目**不适合**使用 Native AOT。

**PublishTrimmed**: Web API 项目（JayTom.Dws.CloudApi、JayTom.Dws.LicenseApi、JayTom.Dws.ManagementApi）可以**谨慎尝试**使用部分裁剪模式，但必须进行全面测试。WPF 项目不建议启用裁剪。

最务实的优化方案是使用 **ReadyToRun** 编译来改善启动性能，同时保持最大兼容性。
