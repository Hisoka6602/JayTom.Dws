# 查询让位于分拣：修改与验证

验证日期：2026-10-04。已将数据管理查询、日志查询和历史导出隔离到独立工作通道；设备触发、组包、接口响应和 PLC 回复不经过查询等待。业务写入积压时历史读取自动等待，页面继续处理输入，离开页面或再次查询会取消旧请求。

## 原因与最终策略

原数据管理页面在 WPF 线程上查询 SQLite 并映射 500 条完整包裹记录。Microsoft.Data.Sqlite 的异步读取实际同步执行，因此仅使用 `await` 无法避免界面被占用。列表还读取了每个包裹的完整接口请求、响应和参数；导出累计全部列表及工作簿，容易在内存不足时影响整个进程。

最终采用以下约束：

- 历史读取使用进程内独立线程，每次执行一个完整操作，在途请求上限为 8。新查询取消旧查询，排队请求立即释放名额；SQLite 原生进度回调可中断正在执行的旧 SQL。
- 待写业务项达到 128 时，历史读取以 25ms 间隔异步等待，尚未打开查询连接；每次查询或映射开始前再主动等待 10ms。业务队列长度通过原子计数读取，不扫描队列，不阻塞设备生产者。服务释放时移除积压指标，避免停机后的残留队列阻止查询。
- 工作线程保留 `Normal` 系统优先级，用准入和主动等待实现业务上的低优先级。早期 `Lowest` / `BelowNormal` 方案在单 CPU 实测中仍出现秒级停顿；线程转储同时捕获到导出初始化字体、界面及查询线程的运行时方法初始化，因此最终避免让共享运行时资源的持有者因低优先级饥饿而拖住其他线程。转储说明的是当时的调用状态，不能据此断言所有停顿都由同一个运行时锁引起。
- 每页 100 条，后台完成查询和模型构建，WPF 只一次性发布当前页面；启用行列虚拟化。列表只读取接口状态、时间与耗时，点击详情再按原包裹标识读取完整报文。查询失败保留原页面并提示错误，异步命令异常被观察，页面卸载取消读取和导出。
- 本地 SQLite 使用 WAL 与私有连接缓存，避免读写连接共享表锁；每连接缓存从 32MiB 调整为 8MiB，限制私有缓存的总内存负担。取消回调在连接归还前撤销，避免取消信号影响后续业务写入。
- Excel 按页读取，SXSSF 只保留最近 100 行，直接写临时文件并在成功后替换目标文件。不会积累完整数据列表或内存工作簿；中途取消保留原文件并清理临时文件。数据导出进度只在本页面显示。
- 诊断日志使用有界非阻塞队列，慢磁盘且队列满时允许丢弃诊断日志，避免无限增长。包裹、上传、分拣指令和回执的业务持久化队列继续保留工作并重试。

## 单 CPU 真实链路压力结果

两轮使用同一客户端 DLL，SHA-256：`EE64EDF596ECFB76EB1ED3430B41820B6328F5DEB2B63E335CED79CCB752BDA4`。最终客户端回归构建的 DLL 与模拟快照哈希一致。

每轮预置 50,000 条历史数据，其中 5000 条各含 8KiB 请求与 8KiB 响应；以 10ms 目标间隔触发 5000 次，比现场约 300ms 的触发密度高 30 倍。相机最终结果保序且响应延迟变化，HTTP 并发乱序、预期接口超时及重复落格回执持续启用。

| 场景 | 触发数 | 完成包裹 / 正确 PLC 回复 | 查询请求 / 实际发布页面 | 导出行数 | UI 心跳最大间隔 | UI 心跳 P99 | 总耗时 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| NoRead 开启 | 5000 | 4865 / 4865 | 722 / 150 | 5000 | 159.35ms | 59.25ms | 53.80s |
| NoRead 关闭 | 5000 | 3845 / 3845 | 722 / 150 | 5000 | 145.45ms | 56.79ms | 53.68s |

合计 **10,000 次触发、8710 个完成包裹、8710 条正确 PLC 回复**，全部完成上传、存图、创建/下发/回执指令及理论/物理格口记录。开启 NoRead 保留的 1020 个空读继续按正常规则走格口 8；关闭时消费并丢弃这些位置。没有条码与小车错绑、重复下发、额外接口超时兜底、业务丢失、运行时异常或正常关闭前 Error/Fatal 日志。

两轮最多各有 2 个历史查询在途，页面最多 100 条。查询开启轮采样到 256 项待写业务积压并触发查询让位；积压及时排空。采样进程工作集峰值分别为 365MiB / 363MiB，包含真实后台服务、历史夹具、WPF 绑定与导出，不是内存上限或真实相机大图的吞吐承诺。

独立 Python 再次只读核对 SQLite 完整性、原始相机时间、小车/条码/格口关联、每包创建/下发/回执各一条及图片文件，并核对两份 Excel 中 5000 个条码的顺序和身份：**差异为 0**。

- [原始两轮报告与数据库](../../output/simulation/runs/20261004-065445)
- [独立核对汇总](../../output/simulation/query-priority-final-audit.json)
- [NoRead 开启原始报告](../../output/simulation/runs/20261004-065445/query-stress-on/report.json)
- [NoRead 关闭原始报告](../../output/simulation/runs/20261004-065445/query-stress-off/report.json)

本次运行使用 Windows 项目真实后台服务、SQLite、WPF STA 调度器及 DataGrid 绑定/布局，设备由 Docker Python TCP/HTTP 夹具模拟。未打开完整生产主窗口，也未执行厂商识别 SDK 或真实 PLC 电气触发。最终测试未开启线程转储采集，以免诊断暂停干扰性能结果。已停止并移除本任务 Docker 容器及网络；未修改现场数据库或运行程序。

## 自动化回归与失败说明

客户端 **113/113 通过**，包括慢查询下 UI 输入、最新查询替代、卸载取消、查询异常恢复、空页面重置、正常条码及 NoRead 的完整详情恢复、5000 行 Excel 往返和中途取消保留原文件；既有 NoRead、PLC 兜底与原包裹身份回归全部通过。

基础工程 **465/467 通过**，本次新增的调度准入、积压等待/取消/停机恢复、并发业务队列计数、SQLite 原生取消、私有缓存 WAL 并发写入及轻量列表/完整详情回归均通过。另 2 项仍是既有静态架构检查失败：

- `Public_implementation_surface_does_not_grow`：Application 公共实现类数量原为 62，超过审计预算；本次没有增加 Application 公共实现类。
- `Production_projects_must_not_link_source_files_from_other_projects`：检查仍将链接客户端源文件的基础测试工程判为生产工程。

这两项未记为通过，也未通过放宽预算或排除测试处理。原有日志有界背压检查已随本次日志策略修复通过。Release 构建成功，代码质量检查通过。

证据：[客户端 TRX](../../output/simulation/tests/query-priority-client-final.trx)、[基础工程 TRX](../../output/simulation/tests/query-priority-core-final.trx)、[代码质量结果](../../output/simulation/tests/query-priority-code-quality.log)。

## 复现与技术依据

```powershell
./eng/simulation/Run-Simulation.ps1 -Count 5000 -Scenario query-stress-on,query-stress-off
```

SQL 异步语义和 WAL 缓存选择参照微软的 [SQLite 异步限制](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async) 与 [连接字符串说明](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings)。SQL 取消使用 SQLite 的 [原生进度回调](https://www.sqlite.org/c3ref/progress_handler.html)。系统线程优先级的取舍参照微软 [调度优先级](https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities)，并以本机单 CPU 联调结果为准。
