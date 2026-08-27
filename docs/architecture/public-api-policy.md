# 稳定公共 API 策略

以下跨模块契约属于稳定公共 API，并由 `StablePublicApiTests` 持续验证：

- `JayTom.Dws.Abstractions.Results.Result` 与 `Result<T>`：统一成功/失败语义。
- `JayTom.Dws.Application.Configuration.ISettingsStore`：配置读写和原子快照边界。
- `JayTom.Dws.Camera.ICamera`：相机生命周期、参数与采集边界。
- `JayTom.Dws.Ocr.IOcr`：OCR 平台中立图像处理边界。
- `JayTom.Dws.Plugin.Contracts.IPlugin`：插件元数据与生命周期事件边界。

兼容规则：

1. 新增成员允许向后兼容；删除或改变现有签名必须提升主版本。
2. 旧成员先标记 `[Obsolete]`，信息必须给出替代成员和计划删除版本。
3. 兼容期内新旧成员映射到同一实现，并由测试验证行为一致。
4. 修改上述契约必须同步本策略、ADR、调用方迁移与公共 API 测试。
