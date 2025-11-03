# 发布优化配置指南 (Publish Optimization Guide)

本指南提供了针对 JayTom.Dws 项目的发布优化配置说明。

## 快速决策指南

### 我应该使用哪种优化？

```
需要最小部署大小 + 愿意承担风险？
│
├─ 是 → 尝试 PublishTrimmed（仅限 Web API 项目）
│         ⚠️ 需要大量测试
│
└─ 否 → 使用 ReadyToRun
          ✅ 安全、兼容、性能提升
```

### 按项目类型的推荐

| 项目类型 | PublishAot | PublishTrimmed | ReadyToRun | 推荐 |
|---------|-----------|---------------|-----------|------|
| WPF 应用 | ❌ 不支持 | ⚠️ 高风险 | ✅ 推荐 | **ReadyToRun** |
| Web API | ❌ 不兼容* | ⚠️ 可尝试 | ✅ 推荐 | **ReadyToRun** 或谨慎尝试 Trimmed |
| Worker Service | ❌ 不兼容* | ✅ 可尝试 | ✅ 推荐 | **ReadyToRun** 或 Trimmed |
| Blazor WASM | N/A | ⚠️ 内置 | N/A | 使用默认配置 |

*由于当前使用 EF Core 5.x、反射和动态代码

## 选项 1: ReadyToRun (推荐 ⭐)

### 优点
✅ 与所有现有代码 100% 兼容  
✅ 提升启动性能（平均提升 20-40%）  
✅ 无需代码更改  
✅ 无运行时风险  

### 缺点
❌ 部署包会更大（约增加 30-50%）  
❌ 编译时间稍长  

### 使用方法

#### 方法 1: 使用提供的发布配置文件

```bash
# 对于 ManagementApi（已提供配置文件）
cd JayTom.Dws.ManagementApi
dotnet publish -p:PublishProfile=ReadyToRunRelease

# 输出位置: bin\Release\net6.0\publish\r2r\
```

#### 方法 2: 命令行参数

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true
```

#### 方法 3: 修改项目文件

在 `.csproj` 文件中添加：

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

### 适用项目
- ✅ JayTom.Dws.Client (WPF)
- ✅ JayTom.Dws.ManagementStudio (WPF)
- ✅ JayTom.Dws.CloudApi
- ✅ JayTom.Dws.LicenseApi
- ✅ JayTom.Dws.ManagementApi
- ✅ JayTom.Dws.UploadCloudService

## 选项 2: PublishTrimmed (实验性 ⚠️)

### 优点
✅ 显著减少部署大小（可减少 30-70%）  
✅ 减少内存占用  

### 缺点
❌ 可能在运行时失败  
❌ 需要大量测试  
❌ 某些功能可能需要特殊标记  
❌ WPF 项目不推荐  

### 使用方法

#### 步骤 1: 分析裁剪警告

在启用裁剪之前，先分析警告：

```bash
cd JayTom.Dws.ManagementApi
dotnet build /p:PublishTrimmed=true /p:EnableTrimAnalyzer=true
```

检查输出中的警告：
- `IL2026`: 需要动态访问的成员
- `IL2060`: 不支持的动态类型
- `IL2070`: 无法确定的目标类型

#### 步骤 2: 使用部分裁剪模式

使用提供的发布配置文件：

```bash
cd JayTom.Dws.ManagementApi
dotnet publish -p:PublishProfile=TrimmedRelease
```

#### 步骤 3: 完整功能测试

**必须测试所有功能！**

使用测试检查清单（见下文）。

### 仅推荐用于以下项目

- ⚠️ JayTom.Dws.ManagementApi（配置文件已提供）
- ⚠️ JayTom.Dws.LicenseApi
- ⚠️ JayTom.Dws.UploadCloudService

### 不推荐用于

- ❌ JayTom.Dws.Client (WPF)
- ❌ JayTom.Dws.ManagementStudio (WPF)
- ❌ JayTom.Dws.CloudApi (使用动态脚本)

## 选项 3: 单文件发布

将所有依赖项打包成单个 .exe 文件。

### 使用方法

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 可与其他选项组合

```bash
# 单文件 + ReadyToRun
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true

# 单文件 + Trimmed (需谨慎)
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial
```

### 注意事项

- 首次启动时会解压文件到临时目录
- 包含本机依赖项时可能会有问题
- 某些资源文件可能需要特殊处理

## 选项 4: 框架依赖部署

最小的部署大小，但需要目标机器安装 .NET 运行时。

### 使用方法

```bash
dotnet publish -c Release --self-contained false
```

### 优缺点

✅ 部署包极小（通常 < 10 MB）  
✅ 自动获得运行时更新  
❌ 需要用户安装 .NET 运行时  
❌ 需要匹配的运行时版本  

## 测试检查清单

如果使用 PublishTrimmed，必须测试以下内容：

### API 项目测试清单

```
基本功能：
□ 应用程序启动且无异常
□ Swagger/OpenAPI 页面加载
□ 健康检查端点响应

数据访问：
□ 数据库连接成功
□ 所有 CRUD 操作正常
□ 复杂查询（JOIN、聚合）工作
□ 事务处理正常

序列化：
□ JSON 序列化/反序列化正确
□ 所有 DTO 正确映射
□ 日期时间格式正确
□ 枚举值正确处理

依赖注入：
□ 所有服务正确注入
□ 作用域服务工作正常
□ 单例服务状态保持

身份验证/授权：
□ JWT 令牌验证工作
□ 授权策略执行正确
□ 角色和声明工作

其他：
□ SignalR 连接和消息
□ 文件上传/下载
□ 后台服务/任务
□ 缓存功能
□ 日志记录
□ 配置加载
```

### WPF 应用测试清单（如果尝试裁剪）

```
UI 和数据绑定：
□ 所有窗口和页面正确显示
□ 数据绑定工作（双向、单向、OneTime）
□ 命令绑定执行
□ 值转换器工作
□ 验证规则应用

资源和样式：
□ 主题和样式正确应用
□ 资源字典加载
□ 动态资源更新
□ 图像和图标显示

控件和功能：
□ 自定义控件渲染
□ 第三方控件工作
□ 对话框显示
□ 导航功能
```

## 性能比较参考

基于典型的 .NET 7.0 Web API 项目：

| 配置 | 部署大小 | 启动时间 | 内存占用 | 兼容性风险 |
|------|---------|---------|---------|-----------|
| 标准发布 | 100 MB | 100% | 100% | 无 |
| ReadyToRun | 140 MB | 60-80% | 100% | 极低 |
| Trimmed (partial) | 40-60 MB | 90-95% | 80-90% | 中等 |
| Trimmed (link) | 30-40 MB | 90-95% | 70-80% | 高 |
| Native AOT | 20-30 MB | 30-50% | 60-70% | 极高* |

*当前项目不支持 Native AOT

## 故障排查

### PublishTrimmed 问题

#### 问题: 运行时 MissingMethodException

```
解决方案:
1. 添加 TrimmerRootAssembly 保留整个程序集

<ItemGroup>
  <TrimmerRootAssembly Include="AssemblyName" />
</ItemGroup>

2. 或标记特定类型

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class MyClass { }
```

#### 问题: JSON 序列化失败

```
解决方案:
迁移到 System.Text.Json 并使用源生成器

[JsonSerializable(typeof(MyType))]
internal partial class MyJsonContext : JsonSerializerContext { }
```

#### 问题: Entity Framework 查询失败

```
解决方案:
1. 使用 TrimMode=partial 而不是 link
2. 升级到 EF Core 7.0+
3. 避免使用动态 LINQ 查询
```

### ReadyToRun 问题

#### 问题: 发布时间过长

```
解决方案:
1. 仅在 Release 配置中启用
2. 使用增量发布
3. 在 CI/CD 中使用发布缓存
```

## 建议的实施路径

### 第 1 阶段: 低风险优化（立即可做）

1. ✅ 为所有项目启用 ReadyToRun
   - 修改主要可执行项目的 Release 配置
   - 测试启动性能改进
   - 测量部署大小增加

### 第 2 阶段: 评估裁剪（1-2 周）

1. ⚠️ 在测试环境中试用 PublishTrimmed
   - 从 ManagementApi 开始（已提供配置）
   - 运行完整测试套件
   - 记录所有警告和问题

2. 📊 收集指标
   - 部署大小减少
   - 性能影响
   - 发现的问题数量

### 第 3 阶段: 生产部署（如果第 2 阶段成功）

1. ✅ 逐步推出
   - 先在非关键服务上启用
   - 监控错误率和性能
   - 扩展到其他服务

2. 📝 记录配置
   - 为每个项目创建发布配置文件
   - 更新部署文档
   - 培训团队

## 总结建议

### 保守方案（推荐 ⭐）
- 所有项目使用 **ReadyToRun**
- 安全、可靠、性能提升
- 部署大小略有增加可以接受

### 激进方案（需要资源投入）
- Web API 和 Worker Service 尝试 **PublishTrimmed (partial mode)**
- 需要 2-4 周的测试和验证
- 可能需要代码修改
- WPF 应用仍使用 ReadyToRun

### 现实方案
1. 立即为所有项目启用 ReadyToRun
2. 为 1-2 个简单的 API 服务试验 PublishTrimmed
3. 根据结果决定是否扩大范围
4. 持续监控和优化

## 相关文件

本仓库中的相关文件：
- `PUBLISHAOT_TRIMMED_EVALUATION.md` - 详细评估报告（中文）
- `PUBLISHAOT_TRIMMED_EVALUATION_EN.md` - 详细评估报告（英文）
- `JayTom.Dws.ManagementApi/Properties/PublishProfiles/TrimmedRelease.pubxml` - 裁剪配置示例
- `JayTom.Dws.ManagementApi/Properties/PublishProfiles/ReadyToRunRelease.pubxml` - R2R 配置示例

## 需要帮助？

如果在实施过程中遇到问题：

1. 检查裁剪分析器警告
2. 查看 .NET 官方文档
3. 在项目中搜索类似问题
4. 考虑回退到更保守的配置
