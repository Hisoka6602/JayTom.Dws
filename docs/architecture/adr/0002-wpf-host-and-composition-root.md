# ADR-0002：Client 作为 WPF 进程入口与组合根

- 状态：已修订
- 日期：2026-08-11

## 决策

`JayTom.Dws.Client` 是产品主程序和唯一 WPF 可执行入口，负责 `App.xaml`、宿主生命周期、顶层组合、视图与 ViewModel。`JayTom.Dws.Host.Wpf` 仅转发启动、没有独立部署边界，因此已经从解决方案移除。

## 结果

启动职责仍通过 `ApplicationComposition` 和 `IHostedServiceSupervisor` 保持内部模块化，并显式协调异步启动/关闭；同时避免双 WPF 入口、跨程序集资源 URI 和重复发布配置。架构测试要求 Client 保持唯一入口，并禁止重新引入空转 Host。
