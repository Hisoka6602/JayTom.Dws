# 迁移指南 - 如何应用性能优化

本文档说明如何将新创建的优化代码集成到现有系统中。

## 1. 数据库查询优化

### 1.1 使用投影查询（高优先级）

**位置**：`JayTom.Dws.Infrastructure/Repository/CloudApi/CloudPackageRepository.cs`

**当前代码**（第 26-56 行）：
```csharp
var barCodeInfoModels = await dbSet.AsNoTracking()
    .OrderByDescending(o => o.PackageCreateTime)
    .Include(b => b.BarCodeInfo)
    .Include(b => b.WeightInfo)
    // ... 10+ Include
    .Where(where)
    .OrderByDescending(order)
    .Skip(pageIndex * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken: token);
```

**优化后的代码**（使用 OptimizedPackageQueries）：
```csharp
// 导入命名空间
using JayTom.Dws.Infrastructure.Repository.CloudApi;

// 方法 1：使用扩展方法
var packages = await dbSet.GetPackageListAsync(
    startTime: startDateTime, 
    endTime: endDateTime,
    pageIndex: pageIndex,
    pageSize: pageSize,
    cancellationToken: token);

// 方法 2：使用搜索方法
var searchResults = await dbSet.SearchPackagesAsync(
    barcode: barcode,
    startScanTime: startScanTime,
    endScanTime: endScanTime,
    cameraSerialNumber: cameraSerialNumber,
    minWeight: minWeight,
    maxWeight: maxWeight,
    requestStatus: requestStatus,
    physicalExit: physicalExit,
    logisticsName: logisticsName,
    deviceName: deviceName,
    pageIndex: pageIndex,
    pageSize: pageSize,
    cancellationToken: token);
```

**需要修改的文件**：
- `JayTom.Dws.Infrastructure/Repository/CloudApi/CloudPackageRepository.cs`
  - `SelectPackageOrderByDescending()` 方法
  - `SelectPackage()` 方法
  - `FirstOrDefaultInfo()` 方法
  
- `JayTom.Dws.Domain/Service/CloudApi/CloudService.cs`
  - 更新返回类型以使用投影 DTO

- `JayTom.Dws.Application/Service/CloudApi/CloudAppService.cs`
  - 更新方法签名以返回投影 DTO

- `JayTom.Dws.CloudApi/Controllers/PackageController.cs`
  - 更新 API 响应以使用投影 DTO

**步骤**：
1. 在 CloudPackageRepository 中添加新方法使用 OptimizedPackageQueries
2. 保留旧方法但标记为 [Obsolete]
3. 逐步迁移调用点
4. 经过充分测试后删除旧方法

### 1.2 使用规范模式（中优先级）

**示例**：创建新的查询方法
```csharp
using JayTom.Dws.Domain.Specifications.PackageSpecifications;
using JayTom.Dws.Infrastructure.Specifications;

// 在 CloudPackageRepository 中添加
public async Task<List<PackageInfoModel>> GetPackagesWithSpecificationAsync(
    ISpecification<PackageInfoModel> specification,
    CancellationToken token = default) {
    
    await using var context = _contextFactory.CreateDbContext();
    var query = SpecificationEvaluator<PackageInfoModel>.GetQuery(
        context.Set<PackageInfoModel>(), 
        specification);
    
    return await query.ToListAsync(token);
}

// 使用示例
var spec = new PackageListSpecification(
    startTime: startTime,
    endTime: endTime,
    pageIndex: 0,
    pageSize: 20);

var packages = await repository.GetPackagesWithSpecificationAsync(spec);
```

## 2. 修复启动阻塞问题

### 2.1 使用 ServiceStartupHelper（高优先级）

**位置**：`JayTom.Dws.Client/App.xaml.cs`

**当前代码**（第 579-591 行）：
```csharp
foreach (var service in hostedServices) {
    var serviceName = service.GetType().Name;
    NLog.LogManager.GetCurrentClassLogger().Error($"服务名: {serviceName}");
    await service.StartAsync(default);
}
```

**优化后的代码**：
```csharp
// 1. 在 App.xaml.cs 构造函数或 RegisterTypes 中注册服务
using JayTom.Dws.Infrastructure.Services;

protected override void RegisterTypes(IContainerRegistry containerRegistry) {
    // ... 现有代码 ...
    
    // 注册 ServiceStartupHelper
    containerRegistry.RegisterSingleton<ServiceStartupHelper>();
}

// 2. 在 OnInitialized 中使用
protected override async void OnInitialized() {
    await Task.Yield();
    base.OnInitialized();
    
    var serviceProvider = Container.Resolve<IServiceProvider>();
    var hostedServices = serviceProvider.GetServices<IHostedService>();
    
    // 使用 ServiceStartupHelper
    var startupHelper = Container.Resolve<ServiceStartupHelper>();
    try {
        await startupHelper.StartServicesAsync(hostedServices, default);
        NLog.LogManager.GetCurrentClassLogger().Info("所有服务启动成功");
    }
    catch (Exception ex) {
        NLog.LogManager.GetCurrentClassLogger().Fatal(ex, "关键服务启动失败");
        // 显示错误消息给用户
        MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", 
            MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
    }
}
```

**需要修改的文件**：
- `JayTom.Dws.Client/App.xaml.cs` - 修改 OnInitialized 方法

## 3. 修复内存泄漏 - 使用有界通道

### 3.1 替换 PackageBackgroundService 中的队列（高优先级）

**位置**：`JayTom.Dws.Client/Service/ProcessingServices/PackageBackgroundService.cs`

**当前代码**（第 76-82 行）：
```csharp
private ConcurrentQueue<CameraImageInfo> _panoramaImageItems = new();
private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();
private ConcurrentDictionary<string, BarCodeFrameInfo> _barCodeFrameInfoItem = new();
private ConcurrentQueue<InstructionsAttach> _instructionsAttachItems = new();
```

**优化后的代码**：
```csharp
using System.Threading.Channels;
using JayTom.Dws.Infrastructure.Channels;

// 替换为有界通道
private readonly Channel<CameraImageInfo> _panoramaImageChannel = 
    BoundedChannelFactory.CreateImageChannel<CameraImageInfo>(capacity: 100);

private readonly Channel<CameraImageInfo> _volumeCameraImageChannel = 
    BoundedChannelFactory.CreateImageChannel<CameraImageInfo>(capacity: 100);

private readonly Channel<InstructionsAttach> _instructionsAttachChannel = 
    BoundedChannelFactory.CreateDataChannel<InstructionsAttach>(capacity: 500);

// BarCodeFrameInfo 字典可以保留，但添加大小限制
private readonly ConcurrentDictionary<string, BarCodeFrameInfo> _barCodeFrameInfoItem = new();
private const int MaxBarCodeFrames = 1000;

// 写入示例
public async Task OnImageReceivedAsync(CameraImageInfo image) {
    // 旧方式：_panoramaImageItems.Enqueue(image);
    
    // 新方式（如果满了会等待）：
    await _panoramaImageChannel.Writer.WriteAsync(image);
    
    // 或者使用 TryWrite（如果满了就丢弃）：
    if (!_panoramaImageChannel.Writer.TryWrite(image)) {
        _logger.Warn("Panorama image channel is full, dropping image");
    }
}

// 读取示例
private async Task ProcessPanoramaImagesAsync(CancellationToken cancellationToken) {
    try {
        await foreach (var image in _panoramaImageChannel.Reader.ReadAllAsync(cancellationToken)) {
            try {
                // 处理图像
                await ProcessImageAsync(image, cancellationToken);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to process panorama image");
            }
        }
    }
    catch (OperationCanceledException) {
        // 正常取消
    }
}

// 添加大小限制的字典操作
private void AddBarCodeFrame(string key, BarCodeFrameInfo frame) {
    if (_barCodeFrameInfoItem.Count >= MaxBarCodeFrames) {
        // 移除最旧的项
        var oldestKey = _barCodeFrameInfoItem.Keys.FirstOrDefault();
        if (oldestKey != null) {
            _barCodeFrameInfoItem.TryRemove(oldestKey, out _);
        }
    }
    _barCodeFrameInfoItem.TryAdd(key, frame);
}

// 清理资源
public override async Task StopAsync(CancellationToken cancellationToken) {
    // 完成写入
    _panoramaImageChannel.Writer.Complete();
    _volumeCameraImageChannel.Writer.Complete();
    _instructionsAttachChannel.Writer.Complete();
    
    // 等待处理完成
    await Task.WhenAll(
        _panoramaImageChannel.Reader.Completion,
        _volumeCameraImageChannel.Reader.Completion,
        _instructionsAttachChannel.Reader.Completion);
    
    await base.StopAsync(cancellationToken);
}
```

**需要修改的文件**：
- `JayTom.Dws.Client/Service/ProcessingServices/PackageBackgroundService.cs`
- `JayTom.Dws.Client/Service/BackgroundService/DataProcessingBackgroundService.cs`
- 其他使用 ConcurrentQueue 的后台服务

### 3.2 DataProcessingBackgroundService 优化

**位置**：`JayTom.Dws.Client/Service/BackgroundService/DataProcessingBackgroundService.cs`

**当前代码**（第 50-55 行）：
```csharp
private ConcurrentQueue<PackageInfoModel> _insertItems = new();
private ConcurrentQueue<ApiResponseReceived> _updateResponseItems = new();
private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
private ConcurrentQueue<InstructionReceived> _instructionItems = new();
private ConcurrentQueue<ExceptionSortingReceived> _exceptionSortingItems = new();
private ConcurrentQueue<PackageExitUpdateEvent> _packageExitUpdateItems = new();
```

**优化后的代码**：
```csharp
using System.Threading.Channels;
using JayTom.Dws.Infrastructure.Channels;

private readonly Channel<PackageInfoModel> _insertChannel = 
    BoundedChannelFactory.CreateDataChannel<PackageInfoModel>(capacity: 1000);

private readonly Channel<ApiResponseReceived> _updateResponseChannel = 
    BoundedChannelFactory.CreateDataChannel<ApiResponseReceived>(capacity: 1000);

private readonly Channel<SavedImageInfo> _savedImageChannel = 
    BoundedChannelFactory.CreateDataChannel<SavedImageInfo>(capacity: 500);

private readonly Channel<InstructionReceived> _instructionChannel = 
    BoundedChannelFactory.CreateDataChannel<InstructionReceived>(capacity: 1000);

private readonly Channel<ExceptionSortingReceived> _exceptionSortingChannel = 
    BoundedChannelFactory.CreateDataChannel<ExceptionSortingReceived>(capacity: 500);

private readonly Channel<PackageExitUpdateEvent> _packageExitUpdateChannel = 
    BoundedChannelFactory.CreateDataChannel<PackageExitUpdateEvent>(capacity: 1000);

// 修改事件订阅
EventAggregator.Instance.Subscribe<PackageInfo>(async item => {
    if (item is { } model) {
        var package = new PackageInfoModel() {
            BarCodeInfo = model.BarCodeInfo,
            WeightInfo = model.WeightInfo,
            VolumeInfo = model.VolumeInfo,
            PackageCreateTime = model.CreateTime,
            PackageTimestamped = new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds(),
        };
        
        // 旧方式：_insertItems.Enqueue(package);
        // 新方式：
        await _insertChannel.Writer.WriteAsync(package);
    }
});

// 修改处理循环（在 ExecuteAsync 中）
protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    // 启动多个处理任务
    var tasks = new List<Task> {
        ProcessInsertItemsAsync(stoppingToken),
        ProcessUpdateResponseItemsAsync(stoppingToken),
        ProcessSavedImageItemsAsync(stoppingToken),
        ProcessInstructionItemsAsync(stoppingToken),
        ProcessExceptionSortingItemsAsync(stoppingToken),
        ProcessPackageExitUpdateItemsAsync(stoppingToken),
    };
    
    await Task.WhenAll(tasks);
}

private async Task ProcessInsertItemsAsync(CancellationToken cancellationToken) {
    await foreach (var package in _insertChannel.Reader.ReadAllAsync(cancellationToken)) {
        try {
            // 处理包裹
            await _packageRepository.AddAsync(package, cancellationToken);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Failed to insert package");
        }
    }
}

// 类似地实现其他处理方法...
```

## 4. 优化图像处理

### 4.1 异步图像保存

**位置**：`JayTom.Dws.Application/Service/CloudApi/CloudAppService.cs`

**当前代码**（第 28-58 行）：
```csharp
barcodeImageInfo.Image?.Save(barcodeImagePath, ImageFormat.Jpeg);
barcodeImageInfo.Image?.Dispose();
```

**优化后的代码**：
```csharp
// 使用 Task.Run 进行异步 I/O
await Task.Run(() => {
    using (barcodeImageInfo.Image) {
        barcodeImageInfo.Image.Save(barcodeImagePath, ImageFormat.Jpeg);
    }
}, cancellationToken);
```

**完整优化方法**已在 `REFACTORING_RECOMMENDATIONS.md` 的"阶段 2.3"中提供。

## 5. 连接池优化

### 5.1 优化数据库连接字符串

**位置**：`JayTom.Dws.CloudApi/appsettings.json`

**当前配置**：
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=127.0.0.1;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;"
}
```

**优化后的配置**：
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=127.0.0.1;Port=3306;Password=f6vQDiiWpXLDUCxR;Database=CloudApi;User=root;Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionLifeTime=300;ConnectionTimeout=30;ConnectionIdleTimeout=300;AllowUserVariables=true;UseAffectedRows=false;"
}
```

### 5.2 优化 DbContext 配置

**位置**：`JayTom.Dws.CloudApi/Program.cs`

**当前代码**（第 52-58 行）：
```csharp
builder.Services.AddPooledDbContextFactory<CloudApiContext>(options => {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
        .EnableServiceProviderCaching();
}, 300);
```

**优化后的代码**：
```csharp
builder.Services.AddPooledDbContextFactory<CloudApiContext>(options => {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions => {
        // 启用重试机制
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        
        // 命令超时
        mySqlOptions.CommandTimeout(30);
        
        // 启用字符串比较转换
        mySqlOptions.EnableStringComparisonTranslations();
    })
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
    .EnableServiceProviderCaching()
    .EnableSensitiveDataLogging(false) // 生产环境关闭
    .ConfigureWarnings(warnings => {
        warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
    });
}, poolSize: 512); // 增加连接池大小
```

## 6. 实施时间表

### 第 1 周：紧急修复
- [x] Day 1-2: 创建优化代码（已完成）
- [ ] Day 3: 集成 ServiceStartupHelper 到 App.xaml.cs
- [ ] Day 4: 在 PackageController 中使用投影查询
- [ ] Day 5: 测试和验证

### 第 2 周：核心优化
- [ ] Day 1-2: 替换 DataProcessingBackgroundService 中的队列
- [ ] Day 3-4: 替换 PackageBackgroundService 中的队列
- [ ] Day 5: 测试和性能基准测试

### 第 3 周：完善和测试
- [ ] Day 1-2: 优化其他后台服务
- [ ] Day 3-4: 负载测试和压力测试
- [ ] Day 5: 文档更新和代码审查

## 7. 测试清单

### 单元测试
- [ ] Specification Pattern 测试
- [ ] Projection 查询测试
- [ ] Channel 读写测试
- [ ] ServiceStartupHelper 测试

### 集成测试
- [ ] 数据库查询性能测试（before/after）
- [ ] 启动时间测试（before/after）
- [ ] 内存使用监控（长时间运行）
- [ ] 并发性能测试

### 性能基准
- [ ] 记录当前性能指标
- [ ] 每次优化后测量改进
- [ ] 验证达到目标（-70% 查询时间，-80% 启动时间等）

## 8. 回滚计划

如果优化导致问题：

1. **立即回滚**：使用 git revert 回退更改
2. **部分回滚**：只回退有问题的特定更改
3. **保留 [Obsolete] 标记的旧方法**：确保可以快速切换回旧实现

## 9. 监控指标

实施后需要监控：

- 数据库查询平均响应时间
- 应用启动时间
- 内存使用量（峰值和平均值）
- CPU 使用率
- GC 频率（Gen 0/1/2）
- 错误率和崩溃率

## 10. 支持和帮助

如有问题，请参考：
- `REFACTORING_RECOMMENDATIONS.md` - 详细的技术说明
- `PERFORMANCE_ISSUES_SUMMARY.md` - 问题总结
- `JayTom.Dws.Infrastructure/Examples/OptimizedBackgroundServiceExample.cs` - 代码示例
