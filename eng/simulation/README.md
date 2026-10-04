# Windows 客户端 + Docker Python 设备联调

这套夹具在 Docker 中模拟下位机、相机最终结果和 HTTP 分拣接口，Windows 上运行当前项目实际的设备事件转发、云山组包、上传、格口规则、TCP 协议、存图及 SQLite 持久化服务。它不是重新实现一套组包算法，也不连接现场设备或使用现场数据库。

运行要求：Docker Desktop 使用 Linux 容器；Windows x64；.NET 10 SDK 与 Windows Desktop Runtime；本机 Python 3.10+ 用于只读独立审计。Python 夹具只有标准库，不安装第三方包。所有应用构建、发布、运行均在本机 Windows 执行，Docker 仅运行 Python 设备夹具。

先在本机生成独立发布包，校验原生资产、符号与重复文件，并逐个核对联调宿主使用的生产程序集 SHA-256：

```powershell
./eng/simulation/Publish-Simulation.ps1
# 使用命令打印的 RuntimeDirectory，运行已发布的 Windows exe，而不是重新编译的开发副本。
./eng/simulation/Run-Simulation.ps1 -RuntimeDirectory <打印的runtime目录> -Scenario query-stress-on,query-stress-off,reply-faults-on,reply-faults-off,lower-removal-on,dual-window-on,dual-window-off,realtime-on
```

本机发布证据保存在 `output/simulation/local-publish/<时间>/local-publish.json`。原始发布暂存与运行副本分开，脚本不会清空常用的 `PublishClean` 或覆盖已部署程序。

```powershell
# 六种场景各 5000 次触发；最后的 300ms 长测约 25 分钟。
./eng/simulation/Run-Simulation.ps1

# 快速复测 NoRead 开、关的真实 TCP/HTTP/数据库链路。
./eng/simulation/Run-Simulation.ps1 -Scenario dense-off,dense-on

# 双相机与时间窗口。
./eng/simulation/Run-Simulation.ps1 -Scenario dual-window-off,dual-window-on

# 低性能设备上的分拣与历史查询竞争：NoRead 开、关，每轮 5000 次触发。
./eng/simulation/Run-Simulation.ps1 -Scenario query-stress-on,query-stress-off
```

脚本遇到失败会返回错误并保留报告。它检查是否有正在运行的夹具，避免覆盖长测；每轮使用新的独立配置库、业务库和图片目录。`-SkipBuild` 使用已有编译结果，`-RuntimeDirectory` 直接运行冻结的本机发布副本，`-KeepContainers` 保留本任务容器。默认只启动 `devices` 服务，`realtime` 提供第二套端口以便独立并行长测。本机审计 Python 优先从 PATH 查找，其次使用 Codex 已有依赖运行时；可用 `-PythonExecutable` 显式指定，解释器在开始测试前验证。

| 服务 | devices 端口 | realtime 端口 | 实际接入位置 |
| --- | ---: | ---: | --- |
| 下位机 | 19001 | 19011 | 生产 TouchSocket TCP 与 Wxkc 8 字节协议 |
| 相机结果 | 19002 | 19012 | 真实 DefaultDeviceService 的 SDK 回调入口 |
| HTTP 接口/夹具控制 | 18086 | 18096 | 生产 DefaultApi 上传与返回格口判定 |

端口仅绑定到本机 127.0.0.1。`RuntimeHost` 不启动 WPF 窗口，不调用厂商拍照/识别 SDK；窗口显示通过真实显示事件验证。图片是夹具生成的 PNG，正常长测为 1280×720，高密度压力测试为 96×64。这里的压力结果不能直接换算成真实相机大图的最大吞吐量。

## 输入与故障注入

- 每次触发创建一辆有编号的小车，同时产生拍照结果。每台相机严格按触发顺序返回，无可靠帧号、触发计数或小车 Id 可供组包代码使用。
- 相机响应延迟在 40～925ms 间变化，并保留后续结果等待前序结果的集中回传情况。
- 每 5 次触发一次 NoRead，另有连续 60 次 NoRead；每 37 次有一次条码过滤。
- 双相机中一台 NoRead、另一台有效码时融合成有效结果；每 113 次注入一次条码冲突并拒绝原车。
- 窗口场景设置 50～500ms，每 97 次注入一次超出上限的观察时间，检查后续原车恢复正常，不连续偏移。
- TCP 创建帧成批粘包及拆包，异步 HTTP 乱序返回，个别 HTTP 返回缺少格口或超时；原车进入异常格口 99。
- HTTP 1.1 使用持久连接，并处理分块正文及尾部头。原夹具逐请求强制断开会产生大量短连接；新报告的 `api_tcp_connections` 记录接口实际连接数，避免将连接创建压力与生产算法吞吐混在一起。预设的 700ms 超时故障继续注入。
- `reply-faults-off` / `reply-faults-on` 额外注入接口逻辑失败、成功正文的 HTTP 503、无效 JSON、无效十六进制指令、空指令、无绑定、停用目标口及不可用连接。Docker 提供锁格传感器状态，Windows 在真实监控服务的硬件回调入口注入锁格事件；`IsLockExit` 是运行时状态，不能通过数据库列模拟。
- 落格回执延迟 120～390ms，每 101 个接收分拣指令的小车再返回一次相同回执。

时间窗口依据相机事件提供的 `ScanTime` 与接收创建指令时刻，而不是仅用 TCP 回调返回时刻。设备时钟差异通过关闭窗口的有序消费场景及客户端回归测试覆盖；配置窗口仍需对应现场 SDK 的时间语义。

## 判定与保留证据

Docker Python 根据原始触发独立生成期望条码和格口，并直接检验 Windows 发出的实际分拣字节及 HTTP 请求。Windows 同时校验完成/显示事件、数据库主记录、上传、理论/物理格口、创建/下发/回执各一条以及图片文件存在。

`report.json` 必须同时满足：Python oracle 通过、运行时异常为空、数据库差异为空、正常关闭前 Error/Fatal 日志为空、结构化日志没有丢弃或写入失败。预期 HTTP 超时与缺少格口必须正确进入异常格口，不能作为遗漏跳过。每轮还自动执行 SQLite/事件/图片独立审计和落盘日志逐包关联审计，全部通过后汇总才标记成功。

模拟器分别记录分拣完成时刻和后台收尾耗时；最多允许 60 秒后台收尾，并将该时间纳入总截止时间。结束判定要求每个完成包裹的创建、下发、回执三条指令全部落库（`counts.Instructions` 为完成包裹数的三倍），随后仍逐包核对，防止主记录数量齐全却提前停止。允许后台收尾不表示现场持续吞吐量已经提高。

每次测试目录位于 `output/simulation/`，包括 `report.json`、`completed.json`、`physical.json`、`Data.db`、独立 `Configuration.db`、真实图片和 `runtime.log`。原始期望数据在 `output/simulation/fixture/*-truth.json`。新版本报告包含客户端 DLL SHA-256，便于区分运行快照。

可使用本机 Python 对保留证据再次做只读核对，包括数据库中完整相机时间、小车/条码/格口、回执事件、各一次的创建/下发/回执指令和图片存在：

```powershell
python ./eng/simulation/Audit-Reports.py <测试目录>/report.json --output <测试目录>/offline-audit.json
```

原图待保存队列保留 256MiB 解码内存预算与 4096 条数量上限。超出这个预算仍执行既有的最旧图片释放策略；测试通过不表示在任意图片大小、任意磁盘速度和无界输入下都能无损存图。

最终测试结果及修复说明见 [VALIDATION.md](VALIDATION.md)。落盘阶段、字段、容量与离线定位命令见 [PIPELINE-LOGGING.md](PIPELINE-LOGGING.md)。

查询专项会给隔离数据库预置 50,000 条历史记录，限制模拟客户端到一个逻辑 CPU，同时以 10ms 间隔触发真实分拣服务，反复执行真实数据管理查询命令、20 次连续替代查询及 5000 行流式导出。使用真实 WPF STA 调度器、DataGrid 绑定与布局检测输入心跳；未打开完整生产主窗口。查询会在待写业务项达到 128 时异步等待，排队至多 8 个请求，列表每页 100 条。详细结果见 [QUERY-PRIORITY-VALIDATION.md](QUERY-PRIORITY-VALIDATION.md)。

验证完成分发后的 PLC 回复与 NoRead 正常规则，可运行：

```powershell
./eng/simulation/Run-Simulation.ps1 -Count 5000 -Scenario dense-off,dense-on,reply-faults-off,reply-faults-on
```

此策略只作用于进入完成分发的包裹：正常结果使用正常规则目标口，异常结果使用已配置的异常口。关闭 NoRead 后在组包阶段丢弃的空读没有完成事件，仍不显示、上传、落库或下发。开启 NoRead 时保留的空读正常匹配规则，不因条码为 NoRead 而强制进入异常口。

格口查询提供商抛异常或返回空结果时发布一个携带原包裹身份的失败响应；该查询不再无限重新排队。其他既有上传、图片与业务推送任务的重试策略保持原实现。每包最终回复使用原创建时间领取，发送前再次检查原车身份和目标口当前状态；明确的备用口重发和通信应答重试仍遵守既有协议配置。

未设置异常口时允许省略异常回复并释放等待。已设置但停用、缺指令或缺通信绑定的异常口会在启动时明确报错；运行中变成不可用同样报告故障。连接层只在确认尚未开始写入时允许切换异常口，已经开始写入的指令不会被盲目改发另一个格口。
