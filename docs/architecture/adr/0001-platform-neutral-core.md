# ADR-0001：平台无关核心

- 状态：已接受
- 日期：2026-08-11

## 决策

Abstractions、Domain 和 Application 统一使用 `net10.0`。核心契约不得引用 WPF、`System.Drawing`、`System.IO.Ports`、NLog、Prism 或 Entity Framework。坐标、矩形、颜色、串口设置和图像所有权使用本仓库的平台无关值对象表达。

## 结果

核心业务可在无 Windows 桌面运行时的测试和服务环境中编译。具体平台对象只在 Client、Camera、Infrastructure、Interface 和设备适配项目中转换。
