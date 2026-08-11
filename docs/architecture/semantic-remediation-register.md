# 语义问题整改台账（60 项）

状态说明：`已修复` 表示新语义已作为主契约使用；`已修复（兼容别名）` 表示保留旧序列化名称、宏或只读别名以兼容既有数据和配置。

| 编号 | 原语义问题 | 状态 | 实施证据 |
|---:|---|---|---|
| 1 | 实体标识泛型类型不统一 | 已修复 | `IEntity.Id` 固定为 `long` |
| 2 | 基础实体主键类型可能漂移 | 已修复 | `BaseModel.Id` 固定为 `long` |
| 3 | 包裹长整型标识误称 `Guid` | 已修复（兼容别名） | `PackageInfo.Id` 为主语义，保留旧别名 |
| 4 | SDK 登录值误称业务 ID | 已修复 | `LoginHandle`、`PlaybackHandle` 等句柄命名 |
| 5 | 相机通道序号误称 ID | 已修复 | `ChannelNumber`、`PlayChannelNumber` |
| 6 | 供应商字符串编码混同实体 ID | 已修复（兼容序列化） | `ApplicationCode`、`DeviceCode`、`NetworkIdentifier` |
| 7 | SignalR 字符串连接值混同 long 主键 | 已修复 | `ConnectionIdentifier`，框架边界仍读取 `ConnectionId` |
| 8 | `TimestampedGuid` 实为毫秒时间戳 | 已修复（兼容序列化） | `TimestampMilliseconds` 与旧 JSON/宏名称映射 |
| 9 | 时间戳字段缺少单位 | 已修复 | `PackageTimestampMilliseconds`、`RecognitionDurationMilliseconds` |
| 10 | 项目内可能重新使用 UTC | 已修复 | 质量守卫禁止 UTC API，业务统一本地时间 |
| 11 | 网络时间返回值未明确时区 | 已修复 | `GetLocalTimeAsync` 返回本地 `DateTimeOffset` |
| 12 | 网络时间失败时静默伪造本机时间 | 已修复 | 解析或网络失败直接向调用方传播 |
| 13 | 令牌时间用 `DateTime.MinValue` 表示缺失 | 已修复 | 聚水潭令牌时间改为 `DateTime?` |
| 14 | 业务测量值使用浮点数 | 已修复 | 重量、体积、尺寸、阈值统一 `decimal` |
| 15 | 系统监控百分比使用浮点数 | 已修复 | CPU、内存、磁盘指标统一 `decimal` |
| 16 | 小数默认值先构造浮点再转换 | 已修复 | `0.002m` 等定点数字面量 |
| 17 | SQLite 小数 CLR 类型与旧列不兼容 | 已修复（兼容存储） | `decimal` 继续映射既有 `REAL` 列 |
| 18 | SQLite 初始化使用硬编码文件路径 | 已修复 | 从实际连接解析数据库路径 |
| 19 | 旧 `EnsureCreated` 数据库没有迁移历史 | 已修复 | 先 `EnsureCreated`，再登记兼容迁移 |
| 20 | 类型升级可能触发表重建 | 已修复 | 空迁移不修改任何表、列和类型 |
| 21 | 旧 INTEGER 大主键缺少回归证明 | 已修复 | 50 亿主键兼容测试 |
| 22 | 旧 REAL 小数缺少读写回归证明 | 已修复 | 原位 decimal 读写兼容测试 |
| 23 | 操作结果用 `KeyValuePair<bool,string>` 表意 | 已修复 | `OperationResult<T>` 提供状态、代码和消息 |
| 24 | 授权响应字段以无类型字符串承载 | 已修复 | 授权 API 使用强类型结果属性 |
| 25 | 上传响应可被任意位置部分修改 | 已修复 | `UploadResponse` 改为不可变 record |
| 26 | 上传耗时没有单位 | 已修复 | `DurationSeconds` |
| 27 | API 超时没有单位 | 已修复（兼容序列化） | `TimeoutMilliseconds` 映射旧 `TimeOut` 名称 |
| 28 | 异步方法名未体现异步语义 | 已修复 | 相机、证书、网络时间方法统一 `Async` 后缀 |
| 29 | 网络和证书验证不能取消 | 已修复 | 契约和实现接受 `CancellationToken` |
| 30 | 相机初始化不能取消且结果含糊 | 已修复 | `InitializeAsync` 返回明确结果并支持取消 |
| 31 | 设备多个布尔状态可能互相矛盾 | 已修复 | `DeviceRuntimeState` 为权威状态机 |
| 32 | 枚举默认值可能误表示真实设备类型 | 已修复 | `ScaleType.None=0`、`CameraBindingType.None=0` |
| 33 | 相机可用性默认即为真 | 已修复 | 必须由发现/连接流程明确设置 |
| 34 | 相机相等性依赖可变序列号 | 已修复 | 移除可变标识相等性实现 |
| 35 | 相机集合可被外部直接修改 | 已修复 | 契约公开 `IReadOnlyList` |
| 36 | 异常事件参数不遵循事件模式 | 已修复 | `DeviceExceptionEventArgs : EventArgs` |
| 37 | 异常属性名称不明确 | 已修复 | 统一使用 `Exception` |
| 38 | 条码设置使用枚举与 `object` 字典 | 已修复 | `BarcodeReaderSettings` 强类型 record |
| 39 | 条码设置在两个读码器中契约不一致 | 已修复 | 统一类型安全设置并仅在适配器内转换 |
| 40 | 条码识别耗时字段名像时间点 | 已修复 | `RecognitionDurationMilliseconds` |
| 41 | 条码拍照参数未说明属于包裹且无单位 | 已修复 | `packageTimestampMilliseconds` |
| 42 | OCR 裁剪图所有权不明确 | 已修复 | `TakeCropImage` 显式转移所有权 |
| 43 | 图片存储是否释放输入不明确 | 已修复 | `SaveAndDisposeImageAsync` 明确所有权 |
| 44 | 图片保存失败仍表现为成功任务 | 已修复 | 异常向上传播，资源在 `finally` 释放 |
| 45 | 仓储插入无成功语义 | 已修复 | `InsertAsync` 返回 `bool` |
| 46 | 仓储异常被空集合、0 或 null 吞掉 | 已修复 | 查询和写入保留真实失败 |
| 47 | 原始 SQL 空字符串被当作有效调用 | 已修复 | 空 SQL 直接参数异常 |
| 48 | 重复包裹创建静默覆盖 | 已修复 | 重复键明确抛出异常 |
| 49 | 删除包裹先释放后发布事件 | 已修复 | 先发布删除事件，再清理资源 |
| 50 | 清空包裹不发布逐项删除语义 | 已修复 | `ClearAll` 发布每个移除事件 |
| 51 | 事件 sender 可能为 null | 已修复 | 包裹事件使用非空发布者 |
| 52 | 缺失重量/体积时伪造默认完成数据 | 已修复 | 完成前要求真实条码、重量和体积 |
| 53 | 已完成包裹可能退回未完成 | 已修复 | 完成状态只允许单向迁移 |
| 54 | 图片标记名称混淆“已保存”和“请求保存” | 已修复 | `IsImageSaveRequested` 与显式标记方法 |
| 55 | 定时器回调返回值没有语义 | 已修复 | 返回 `false` 时停止并释放定时器 |
| 56 | 包裹定时器可被外部任意替换 | 已修复 | 定时器封装为私有/内部状态 |
| 57 | 事件总线同步/异步执行语义不清 | 已修复 | 文档化同步线程、顺序并增加 `SubscribeAsync` |
| 58 | 异步事件尾链无界增长 | 已修复 | 单订阅者有界队列，容量 256 |
| 59 | 一个异步订阅异常会破坏后续队列 | 已修复 | 每个处理器独立捕获、记录并继续排空 |
| 60 | 相机变更以枚举加 `object` 表达联合类型 | 已修复 | 三种强类型 `CameraParametersModifiedEventArgs` 子类 |

## 边界约定

业务模型、业务计算、数据库实体和跨层契约不再使用 `float`/`double`。WPF 几何、GDI 绘制、Excel 版式以及厂商原生 SDK 函数签名仍必须在最外层适配器使用框架规定的浮点类型，并在进入业务层前立即转换为 `decimal`；这些边界由质量基线冻结，禁止扩散。
