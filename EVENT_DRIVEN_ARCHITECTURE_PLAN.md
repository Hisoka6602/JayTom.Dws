# 事件驱动架构迁移计划

## 概述
本文档概述了将 JayTom.Dws.Client 重构为采用事件驱动架构（EDA）模式的计划。目标是提高应用程序的模块化、可扩展性和可维护性。

## 当前架构
当前架构遵循传统的分层方法：
- **表现层**: JayTom.Dws.Client (WPF UI)
- **领域层**: JayTom.Dws.Domain, JayTom.Dws.Data
- **基础设施层**: JayTom.Dws.Infrastructure, JayTom.Dws.Interface, JayTom.Dws.Utils
- **设备层**: JayTom.Dws.Camera, JayTom.Dws.Nvr, JayTom.Dws.Ocr
- **插件层**: JayTom.Dws.Plugin, JayTom.Dws.PluginInterface
- **许可证层**: JayTom.Dws.License

## 目标事件驱动架构

### 核心原则
1. **松耦合**: 组件通过事件通信，而不是直接方法调用
2. **异步处理**: 事件异步处理
3. **可扩展性**: 可以通过订阅现有事件添加新功能
4. **可测试性**: 组件可以独立测试

### 建议的架构组件

#### 1. 事件总线（中央消息代理）
- **技术**: MediatR 或自定义实现
- **用途**: 所有领域事件的中央枢纽
- **位置**: JayTom.Dws.Infrastructure

#### 2. 领域事件
为关键领域操作定义事件：
- **设备事件**
  - `DeviceConnectedEvent` - 设备已连接事件
  - `DeviceDisconnectedEvent` - 设备已断开事件
  - `MeasurementCompletedEvent` - 测量完成事件
  - `CameraImageCapturedEvent` - 相机图像已捕获事件
  
- **工作流事件**
  - `WorkflowStartedEvent` - 工作流已启动事件
  - `WorkflowStepCompletedEvent` - 工作流步骤完成事件
  - `WorkflowCompletedEvent` - 工作流完成事件
  - `WorkflowFailedEvent` - 工作流失败事件

- **数据事件**
  - `DataValidatedEvent` - 数据已验证事件
  - `DataSavedEvent` - 数据已保存事件
  - `DataSyncedEvent` - 数据已同步事件

- **UI 事件**
  - `NotificationRequiredEvent` - 需要通知事件
  - `ViewNavigationRequestedEvent` - 视图导航请求事件

#### 3. 事件处理程序
为每种事件类型创建专用处理程序：
- 位于适当的层（领域层、基础设施层或表现层）
- 实现单一职责原则
- 支持 async/await 模式

#### 4. 事件存储（可选）
- 存储事件历史记录用于审计和重放
- 如需要，实现事件溯源模式

### 迁移阶段

#### 阶段 1：基础设施（第 1-2 周）
- [ ] 使用 MediatR 设置中央事件总线
- [ ] 定义核心领域事件接口和基类
- [ ] 创建事件处理程序基础设施
- [ ] 更新依赖注入配置

#### 阶段 2：设备集成（第 3-4 周）
- [ ] 将相机操作迁移到事件驱动模型
- [ ] 将重量测量迁移到事件驱动模型
- [ ] 将条码扫描器迁移到事件驱动模型
- [ ] 实现设备连接/断开连接事件

#### 阶段 3：业务逻辑（第 5-6 周）
- [ ] 将工作流引擎转换为事件驱动
- [ ] 实现数据验证事件
- [ ] 转换数据持久化操作
- [ ] 添加基于事件的日志记录和监控

#### 阶段 4：UI 集成（第 7-8 周）
- [ ] 更新 UI 以订阅领域事件
- [ ] 通过事件实现 UI 通知系统
- [ ] 将视图导航转换为事件驱动
- [ ] 更新插件系统以使用事件

#### 阶段 5：测试与优化（第 9-10 周）
- [ ] 为所有事件处理程序添加单元测试
- [ ] 为事件流添加集成测试
- [ ] 性能测试和优化
- [ ] 文档编写和培训

### 技术实现细节

#### 事件总线配置
```csharp
// 在 Startup 或 App.xaml.cs 中
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblies(
        typeof(Client).Assembly,
        typeof(Domain).Assembly,
        typeof(Infrastructure).Assembly
    );
});
```

#### 事件定义示例
```csharp
public class MeasurementCompletedEvent : INotification
{
    public string DeviceId { get; set; }       // 设备 ID
    public double Weight { get; set; }         // 重量
    public DateTime Timestamp { get; set; }    // 时间戳
    public byte[] Image { get; set; }          // 图像
}
```

#### 事件处理程序示例
```csharp
public class MeasurementCompletedEventHandler : INotificationHandler<MeasurementCompletedEvent>
{
    private readonly IDataRepository _repository;
    private readonly INotificationService _notificationService;
    
    public async Task Handle(MeasurementCompletedEvent notification, CancellationToken cancellationToken)
    {
        // 保存测量数据
        await _repository.SaveMeasurementAsync(notification);
        
        // 通知 UI
        await _notificationService.NotifyAsync("测量完成");
    }
}
```

### 优势

1. **提高可维护性**: 清晰的关注点分离
2. **增强可测试性**: 组件可以独立测试
3. **更好的可扩展性**: 可以在不修改现有代码的情况下添加新功能
4. **增加灵活性**: 易于添加/删除事件处理程序
5. **审计跟踪**: 所有事件都可以记录，用于调试和合规性

### 风险和缓解措施

1. **学习曲线**
   - 缓解措施：提供培训课程和文档
   
2. **性能开销**
   - 缓解措施：使用异步处理，优化关键路径
   
3. **调试复杂性**
   - 缓解措施：实现全面的日志记录和事件跟踪

4. **事件版本控制**
   - 缓解措施：设计考虑向后兼容性的事件

### 后续步骤

1. 审查并批准此计划
2. 在基础设施层中设置 MediatR
3. 开始阶段 1 实施
4. 安排定期审查会议

## 参考资料
- [MediatR 文档](https://github.com/jbogard/MediatR)
- [事件驱动架构模式](https://martinfowler.com/articles/201701-event-driven.html)
- [领域事件模式](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
