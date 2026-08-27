# 模块所有权

本清单定义代码层面的能力所有者。所有者表示负责维护公共契约、依赖方向、迁移策略与回归测试的模块；跨模块变更必须由调用方适配所有者公开的 API。

| 项目 | 业务/技术能力所有者 | 稳定边界 |
| --- | --- | --- |
| JayTom.Dws.Abstractions | 跨平台基础契约 | Results、Devices、Persistence、Workflows |
| JayTom.Dws.Models | 持久化数据模型 | 数据库记录模型，不承载领域行为 |
| JayTom.Dws.Domain | 纯领域基础 | 值对象、领域算法与平台无关契约 |
| JayTom.Dws.Legacy.Contracts | 历史兼容契约 | 隔离仍绑定持久化模型的 DTO、仓储与运行时聚合，供渐进迁移 |
| JayTom.Dws.Application | 应用用例 | Command/Query、配置、消息、工作流 |
| JayTom.Dws.Infrastructure | 基础设施适配 | EF Core、文件、JWT、视频服务 |
| JayTom.Dws.Integrations.Contracts | 外部集成契约 | HTTP/上传/网络时间契约 |
| JayTom.Dws.Interface | 外部系统适配 | 快递、云端及第三方 API 实现 |
| JayTom.Dws.Camera | 相机与 NVR 适配 | ICamera、厂商 SDK、原生资产 |
| JayTom.Dws.Ocr | OCR 引擎适配 | IOcr、平台中立图像载荷 |
| JayTom.Dws.Excel | 表格导入导出 | Excel 文件适配 |
| JayTom.Dws.Utils | 平台工具适配 | Windows 与通用工具实现 |
| JayTom.Dws.License | 授权验证 | 授权运行时契约与验证 |
| JayTom.Dws.LicenseTool | 授权生成工具 | 离线授权文件生成入口 |
| JayTom.Dws.Plugin.Abstractions | 插件运行时契约 | 清单、兼容性、加载与卸载 |
| JayTom.Dws.PluginInterface | 插件展示契约 | WPF 插件视图扩展点 |
| JayTom.Dws.Plugin | 设备插件适配 | 磅秤、键盘等设备实现 |
| JayTom.Dws.Client | 桌面组合与展示 | WPF 组合根、ViewModel、宿主服务 |
| JayTom.Dws.Tests | 自动化验收 | 单元、契约、架构与安全回归 |
| JayTom.Dws.Benchmarks | 性能基线 | 热路径基准与预算 |
| JayTom.Dws.CodeQualityGuard | 代码质量策略 | 可重复执行的静态质量门禁 |

## 变更规则

1. 新能力首先选择唯一所有者，不允许通过链接编译复制源文件。
2. 跨模块只依赖稳定契约；实现默认保持内部可见。
3. 修改稳定边界必须同步 ADR、公共 API 策略与自动化测试。
4. 新增项目必须同时登记到 `eng/ArchitecturePolicy.json` 和本清单。
| JayTom.Dws.Camera.Contracts | 相机适配器稳定契约 | 平台无关帧租约、能力、背压和工厂契约 |
| JayTom.Dws.Plugin.Runtime | 插件运行时与安全边界 | 签名验证、权限、撤销、加载和卸载 |
