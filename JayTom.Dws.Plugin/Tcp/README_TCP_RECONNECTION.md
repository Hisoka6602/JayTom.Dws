# TCP客户端无限重连功能说明

## 概述

`TouchSocketTcpClient` 类实现了完整的TCP客户端功能，并支持两种重连机制：
1. **自动重连**：由TouchSocket框架的重连插件自动处理
2. **手动重连**：通过调用 `Reconnect` 方法手动控制重连逻辑

## 功能特性

### ✅ 支持无限重连
TCP客户端完全支持无限重连功能，确保在网络中断或服务器临时不可用时能够持续尝试重新建立连接。

### 1. 自动重连（推荐）

在 `SetParameter` 方法中配置TouchSocket的重连插件：

```csharp
var touchSocketConfig = new TouchSocketConfig()
    .SetRemoteIPHost(new IPHost($"{tcpConnect.Address}:{tcpConnect.Port}"))
    .UsePlugin()
    .SetBufferLength(tcpConnect.DataLength)
    .ConfigurePlugins(a => {
        // 参数说明：
        // -1: 无限重连次数
        // true: 启用重连
        // 1000: 每次重连间隔1000毫秒（1秒）
        a.UseReconnection(-1, true, 1000);
    });
```

**优点：**
- 框架级别的自动处理，无需手动干预
- 在连接断开后自动触发重连
- 性能开销小

### 2. 手动重连

通过 `Reconnect` 方法实现应用层面的重连控制：

```csharp
// 创建TCP客户端
var tcpClient = new TouchSocketTcpClient();

// 配置连接参数
var param = new TcpConnectParam {
    Address = "192.168.1.100",
    Port = 8080,
    DataFormatType = FormatType.Ascii,
    DataLength = 1024
};

tcpClient.SetParameter(param);

// 创建取消令牌以便随时停止重连
var cts = new CancellationTokenSource();

// 无限重连：count <= 0
// 有限重连：count > 0
await tcpClient.Reconnect(count: -1, token: cts.Token);

// 取消重连
cts.Cancel();
```

**参数说明：**
- `count <= 0`: 无限次重连，直到连接成功或被取消
- `count > 0`: 重连指定次数后停止
- `token`: CancellationToken用于取消重连操作

**优点：**
- 完全控制重连时机
- 支持通过CancellationToken优雅取消
- 可以在重连过程中执行自定义逻辑

## 使用示例

### 示例1：基本的无限重连

```csharp
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Plugin.Tcp.TcpClient;

var client = new TouchSocketTcpClient();

// 订阅事件
client.Connected += (s, msg) => Console.WriteLine($"已连接: {msg}");
client.Disconnected += (s, msg) => Console.WriteLine($"已断开: {msg}");

// 配置参数（自动启用无限重连）
client.SetParameter(new TcpConnectParam {
    Address = "127.0.0.1",
    Port = 9999
});

// 尝试连接
var cts = new CancellationTokenSource();
await client.Connect(token: cts.Token);
```

### 示例2：带取消功能的手动无限重连

```csharp
var client = new TouchSocketTcpClient();
var cts = new CancellationTokenSource();

// 5分钟后自动取消重连
cts.CancelAfter(TimeSpan.FromMinutes(5));

// 无限重连直到成功或超时
bool result = await client.Reconnect(-1, cts.Token);

if (result) {
    Console.WriteLine("重连成功！");
} else {
    Console.WriteLine("重连已取消");
}

client.Close();
```

### 示例3：有限次数重连

```csharp
var client = new TouchSocketTcpClient();

// 重连5次
bool result = await client.Reconnect(count: 5);

if (!result) {
    Console.WriteLine("重连5次后仍然失败");
}
```

## 重连机制详解

### 自动重连流程
1. TCP连接断开时，TouchSocket框架检测到断开事件
2. 根据配置的重连次数（-1表示无限）和间隔时间自动发起重连
3. 重连成功后触发 `Connected` 事件
4. 重连失败继续等待下一次重连

### 手动重连流程
1. 调用 `Reconnect(count, token)` 方法
2. 根据count参数决定重连模式：
   - `count > 0`: 循环重连count次
   - `count <= 0`: 无限循环重连
3. 每次重连间隔500毫秒
4. 检查CancellationToken状态，如果被取消则立即停止
5. 连接成功后返回true，全部失败或取消后返回false

### 取消重连
```csharp
var cts = new CancellationTokenSource();
var reconnectTask = client.Reconnect(-1, cts.Token);

// 在其他地方取消重连
cts.Cancel();

// 等待重连任务完成
await reconnectTask;
```

## 注意事项

1. **资源管理**：
   - 使用完毕后记得调用 `Close()` 方法释放资源
   - 长时间无限重连可能会占用系统资源

2. **取消令牌**：
   - 无限重连时强烈建议传入CancellationToken
   - 可以设置超时时间避免永久重连

3. **事件处理**：
   - 订阅 `Connected`、`Disconnected`、`Exception` 事件以监控连接状态
   - 在事件处理程序中避免长时间阻塞操作

4. **重连间隔**：
   - 自动重连间隔：1000毫秒（由UseReconnection配置）
   - 手动重连间隔：500毫秒（Reconnect方法内部固定）

5. **线程安全**：
   - 内部使用SemaphoreSlim确保发送操作的线程安全
   - 多线程环境下可安全使用

## 配置建议

### 生产环境推荐配置

```csharp
var client = new TouchSocketTcpClient();

// 使用自动重连（框架处理）
client.SetParameter(new TcpConnectParam {
    Address = "production-server.com",
    Port = 8080,
    DataFormatType = FormatType.Ascii,
    DataLength = 1024
});

// 订阅关键事件
client.Disconnected += async (s, msg) => {
    // 记录断开日志
    Logger.Error($"TCP连接断开: {msg}");
    // 框架会自动重连，无需手动处理
};

client.Connected += (s, msg) => {
    Logger.Info($"TCP连接已建立: {msg}");
};
```

### 开发/测试环境配置

```csharp
var client = new TouchSocketTcpClient();

// 使用有限次重连，避免测试时无限等待
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
await client.Reconnect(count: 10, token: cts.Token);
```

## 常见问题

**Q: 自动重连和手动重连可以同时使用吗？**
A: 可以。自动重连由框架层处理连接中断，手动重连用于应用层主动发起重连。

**Q: 如何判断当前是否处于重连状态？**
A: 检查 `ConnectionStatus` 属性：
```csharp
if (client.ConnectionStatus == ConnectionStatus.Disconnected) {
    // 未连接状态，可能正在重连
}
```

**Q: 无限重连会导致CPU占用过高吗？**
A: 不会。每次重连尝试之间有500-1000毫秒的延迟，避免了频繁重连导致的CPU占用。

**Q: 如何设置重连超时？**
A: 使用CancellationTokenSource的CancelAfter方法：
```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5)); // 5分钟后超时
await client.Reconnect(-1, cts.Token);
```

## 版本历史

- **v1.0**: 支持无限重连功能
  - 自动重连配置支持-1参数（无限重连）
  - 手动重连支持count<=0参数（无限重连）
  - 改进CancellationToken处理逻辑

## 相关文件

- `TouchSocketTcpClient.cs`: TCP客户端实现
- `ITcpBase.cs`: TCP基础接口定义
- `ITcpCommClient.cs`: TCP客户端接口定义
