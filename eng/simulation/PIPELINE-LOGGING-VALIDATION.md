# 分拣链路落盘日志验证

2026-10-04，当前工作区 Release x64。阶段、关联字段与离线定位命令见 [PIPELINE-LOGGING.md](PIPELINE-LOGGING.md)。

最终配置下完成六组真实 Windows 客户端服务与 Docker Python 设备夹具模拟，每组 5000 次触发，共 30,000 次触发、25,962 个业务完成包裹。原小车配对、HTTP 请求、实际 PLC 指令字节、真实落格回执、主记录、上传、图片及创建/发送/回执指令均与独立期望一致。NoRead 开启时正常规则命中格口 8；关闭时消费原车后丢弃业务输出。

| 场景 | 触发 / 完成 | 分拣完成秒数 | 全链路收尾秒数 | 报告目录 |
| --- | --- | --- | --- | --- |
| 密集单相机，NoRead 开启 | 5000 / 4865 | 6.74 | 12.67 | `output/simulation/runs/20261004-092234/dense-on` |
| 密集单相机，NoRead 关闭 | 5000 / 3845 | 6.68 | 11.02 | `output/simulation/runs/20261004-092234/dense-off` |
| 单核并发数据查询，NoRead 开启 | 5000 / 4865 | 51.80 | 79.18 | `output/simulation/runs/20261004-092653/query-stress-on` |
| 单核并发数据查询，NoRead 关闭 | 5000 / 3845 | 51.36 | 53.35 | `output/simulation/runs/20261004-092653/query-stress-off` |
| 双相机 50-500 ms 窗口，NoRead 开启 | 5000 / 4772 | 51.49 | 57.91 | `output/simulation/runs/20261004-092653/dual-window-on` |
| 双相机 50-500 ms 窗口，NoRead 关闭 | 5000 / 3770 | 51.66 | 53.42 | `output/simulation/runs/20261004-092653/dual-window-off` |

六组的 `pipeline_logging` 均为：普通日志丢弃 0、异常日志丢弃 0、提交失败 0、文件目标错误报告 0。离线 `pipeline-audit.json` 六组全部通过，逐一核对 30,000 个固定消费位置及保留包裹的条码绑定、完成、API 回流、格口选择、PLC 写入和真实回执证据；不能仅凭模拟业务通过推断文件证据齐全。

最终客户端 DLL SHA-256：`E1FABD99129048F3F1EC9A05AEF4A27BC99933977DCE8DD8345D548BCCFEEC99`。报告保留原始配置和计数，文件均位于独立模拟目录，不接触生产库或真实设备。

## 性能与故障修复

普通日志曾采用 100 ms / 1000 条批处理；1 ms 密集突发时超过写出速率，业务正确但普通诊断记录发生丢弃。改为 10 ms / 256 条后，保留 20,000 条非阻塞容量，最终两组密集模拟及四组其他模拟的文件证据和实际丢弃计数全部通过。异常文件保持独立 4096 条容量、10 ms / 128 条批处理。

单个外部文本超过 16,384 字符时省略正文并记录原长度，凭据脱敏使用有时间边界的正则；普通不含分隔符的条码和固定说明直接跳过正则。包裹字段先在锁内快照标量，脱敏和日志提交在释放锁后执行。两个 JSON 文件保持每进程独立写句柄，减少逐条开关文件。

数据库单次实体图更新省去外层事务，由 EF `SaveChanges` 按需原子提交，减少额外事务和保存点。真实 SQLite 故障注入验证：单格口更新无额外显式事务；后续指令写入失败时整组分拣信息与指令回滚，恢复后可以再次提交。该事务语义参见 [EF Core 官方说明](https://learn.microsoft.com/en-us/ef/core/saving/transactions)。

查询压力场景预置 50,000 条历史，限制一个逻辑 CPU，以 10 ms 间隔触发分拣，同时使用真实数据管理命令、WPF STA/DataGrid 心跳及 5000 行流式导出。开启/关闭 NoRead 两组的 UI 最大间隔为 173.25 / 231.19 ms，P99 为 51.83 / 80.90 ms，导出均完成 5000 行，查询及运行时异常为空。

此强度约为 300 ms 现场触发间隔的 30 倍。NoRead 开启的单核场景在分拣约 51.80 秒完成后，后台持久化另需收尾，累计收尾等待约 27.90 秒。模拟器将单核压力场景收尾上限从 20 秒改为 60 秒，并分别记录 `sorting_finished_elapsed_seconds`、`persistence_drain_seconds` 和上限；其他场景仍为 20 秒。完整落库计数和逐条关联仍必须通过，不能把延长等待当作吞吐量提高或现场无限速率保证。

## 回归与边界

- 客户端功能及日志关联测试：119 / 119 通过，包含 NoRead 开关和原车超窗拒绝后下一车正确恢复。
- 生产配置实际文件日志测试：10 / 10 通过，覆盖凭据隐藏、超长响应、故障堆栈、包裹快照、慢盘洪峰、独立异常容量及错误恢复计数。
- 真实 SQLite 更新与失败回滚测试：2 / 2 通过。
- 离线定位脚本测试：2 / 2 通过，覆盖仅携带 Timestamp 的后续记录、停机后相机次序重用及重复目标去重。
- 核心工程总测试：471 / 473 通过；两项既有架构检查仍失败：`Public_implementation_surface_does_not_grow`、`Production_projects_must_not_link_source_files_from_other_projects`，与本轮新增日志功能测试分开保留。
- 代码质量守卫通过，未放宽基线；`git diff --check` 通过。

设备采用 TCP PLC、相机 SDK 边界回调和 HTTP 夹具；测试图片为 96×64，未调用厂商原生识别 SDK、未打开完整生产主窗口，也未模拟真实机械误动作。实际磁盘长期故障或超出设备处理能力的持续输入仍可能产生诊断缺口；有界诊断队列不得阻塞分拣，恢复后的 `diagnostics.loss` 用于报告累计缺口。

可重复离线核查最终证据：

```powershell
python eng/simulation/audit-pipeline.py output/simulation/runs/20261004-092234
python eng/simulation/audit-pipeline.py output/simulation/runs/20261004-092653
```

密集 A/B/C 垂直泳道图位于 `output/pdf/JayTomDWS_密集包裹延迟相机响应垂直泳道链路.pdf`；已完成 PDF 布局校验、Poppler 渲染逐段检查及中文可搜索文本核对。
