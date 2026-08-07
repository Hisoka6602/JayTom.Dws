# 代码质量编译守卫

构建 `JayTom.Dws.Client` 时会先构建并执行质量守卫。守卫扫描仓库根目录下全部 `JayTom.Dws.*` 工程，现有技术债记录在 `CodeQualityBaseline.json`；同一文件新增违规会导致编译失败。

## 强制规则

- 禁止 `DateTime.UtcNow`、`DateTimeOffset.UtcNow`、`DateTimeKind.Utc` 和 `ToUniversalTime()`。
- 源码与配置文件必须是有效 UTF-8，且不得出现常见中文乱码片段。
- 类、结构、接口、记录、枚举、委托、方法、属性、字段、事件和枚举成员必须有中文 XML 文档。
- 每个类型必须独占文件，类型名必须与文件名一致；事件载荷类型因此同样必须独占文件。
- 禁止新增 `float`、`double`、`Half` 及相关解析或转换 API，业务小数统一使用 `decimal`。
- 名称为 `id`、以 `Id` 或 `ID` 结尾的声明必须使用 `long` 或 `long?`。
- 热路径禁止直接访问数据库或文件，禁止在热路径引入高分配 LINQ 和 `Task.Run`。
- 禁止新增原始 SQL、非 EF Core 数据库客户端、低效 EF Core 查询及同步阻塞异步任务。
- 禁止 EF 实体和迁移把图片、音视频、附件或文件内容保存为 BLOB、二进制数组、流或图像对象；只允许保存路径、URL、名称、哈希和其他元数据。
- `JayTom.Dws.*` 工程必须使用 .NET 10；全仓统一使用 C# 14。
- 数据模型签名变化时必须新增 Code First 迁移，否则守卫拒绝更新基线。

## 热路径标记

守卫会自动识别名称包含 `Package`、`Sorting`、`Image`、`Frame`、`Received`、`Callback`、`Process`、`Parse`、`Encode` 或 `Decode` 的方法。其他高频方法应在 XML 文档或特性中加入 `DWS-HOT-PATH` 标记。

## 数据库调优与分表标记

每个 `DbContext` 必须提供自动迁移、索引、调优和分表策略。调优策略实现处需要添加 `DWS-DATABASE-TUNING: 中文说明`，分表策略实现处需要添加 `DWS-DATABASE-PARTITION: 中文说明`，用于让代码审查和编译守卫定位具体实现；仅添加标记而没有实际实现不符合评审要求。

## 更新基线

只有完成修复或确认新增迁移后才允许更新基线：

```powershell
dotnet build eng/JayTom.Dws.CodeQualityGuard/JayTom.Dws.CodeQualityGuard.csproj
dotnet eng/JayTom.Dws.CodeQualityGuard/bin/Debug/net10.0/JayTom.Dws.CodeQualityGuard.dll . eng/CodeQualityBaseline.json --write-baseline
```

不要手工提高违规计数。模型发生变化但没有新增迁移时，`--write-baseline` 也会失败。
