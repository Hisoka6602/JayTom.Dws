# ADR-0004：集成与设备适配

- 状态：已接受
- 日期：2026-08-11

## 决策

外部上传与网络时间接口由 `Integrations.Contracts` 编译拥有，供应商实现留在 Interface。Camera 和设备项目负责厂商 SDK、原生资产及中立类型映射；Client 不直接引用厂商程序集。

## 结果

上传契约使用 `ImageHandle`，条码格式和 NVR 查询通过中立 API 暴露。FFmpeg 文件随 Camera 项目发布，Client 项目文件不再从相邻项目复制原生资产。
