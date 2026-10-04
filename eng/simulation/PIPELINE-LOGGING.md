# 分拣、组包与绑定的落盘证据

日志位于实际运行的客户端目录下，与数据库写入成功与否无关；实际模拟与回归结果见 [PIPELINE-LOGGING-VALIDATION.md](PIPELINE-LOGGING-VALIDATION.md)。

- `logs/Pipeline/Pipeline-日期-进程号.jsonl`：完整链路，JSON 每行一条。
- `logs/Anomalies/Anomalies-日期-进程号.jsonl`：独立保存警告和异常。
- 文件达到 100 MiB 后滚动，保留最多 30 个归档；排查跨日期问题时同时查看归档文件。

先用 `packageTimestamp` 定位一次包裹，再核对 `cartId`、`createdAt` 和 `creationTimeTicks`。小车 Id 会循环复用，不能单独作为一次包裹的标识。`runId` 区分记录器所在的进程运行，`processId`、`threadId` 标明执行上下文。`eventSequence` 是日志提交序号，文件行次序和它都不应单独用来推断异步业务执行次序；结合阶段和观测、处理时间判断。

| 问题 | 主要阶段 | 可定位的证据 |
| --- | --- | --- |
| 组包缺失、没有齐套 | `package.created`、`package.creation.rejected`、`package.fields.changed`、`package.completed`、`package.removed` | 创建方法、原始创建指令、原车身份、条码/重量/体积是否齐备、移除说明、是否业务丢弃 |
| 条码错绑、结果迟到 | `trigger.slot.queued`、`camera.result.ordered`、`camera.paired`、`barcode.assignment.selected`、`camera.pair.rejected`、`barcode.assignment.rejected` | 本机触发消费次序、每台相机的结果次序、SDK 帧号、条码、原车创建时间、窗口上下限、数值时间间隔、队列积压 |
| NoRead 或过滤后的丢弃 | `package.discarded` | `NoReadDisabled`、`NoReadFiltered`、`BarcodeFiltered` 等原因代码，以及 `resultConsumed=true`；丢弃不再生成完成事件 |
| 多相机组包异常 | `camera.fusion.deadline`、`camera.fusion.completed`、`camera.late`、`camera.duplicate`、`camera.fusion.error` | 应到/实到相机数、各相机条码和时间、冲突或缺帧、已经关闭的组、异常堆栈 |
| 分拣规则错误或进入异常口 | `api.query.completed`、`sorting.api.rule`、`sorting.fallback`、`sorting.route.rejected`、`sorting.reply.queued` | 查询成功状态、超时/解析等错误类型、有限长度的业务响应、规则 Id、目标口、异常口、指令绑定、配置延时 |
| PLC 没收到指令 | `plc.send.requested`、`plc.send.started`、`plc.write.attempt`、`plc.send.failed`、`plc.queue.rejected`、`sorting.reply.sent` | 连接名称、发送排队时间、写入尝试次数、是否已开始写入、堆栈；原有 `transport.Send` 保留原始发送内容 |
| 错车保护拒绝回复 | `sorting.api.response.rejected`、`sorting.reply.rejected`、`plc.identity.rejected` | 原响应/指令的完整身份、创建时间、扫码时间、拒绝说明；重复响应另记 `sorting.api.response.duplicate` |
| PLC 落格异常 | `plc.fall.received`、`plc.fall.duplicate`、`plc.fall.timeout`、`sorting.PhysicalExit` | 实际回执原文、实际格口、异常类型；超时补记明确标为 `isSynthetic=true`，与真实回执区分 |
| 数据库落库异常 | `persistence.package.saved`、`persistence.failed`、`persistence.retry` | 主记录数据库 Id、受影响包裹标识、异常堆栈、待写工作数、重试类型和次数 |
| 链路积压或证据缺口 | `package.queue.pressure`、`diagnostics.loss` | 各队列等待水位、累计普通/异常日志丢弃数、格式化/提交失败数、文件目标错误报告和最近错误 |

相机 `observedAt` 和下位机创建时间可能来自不同设备时钟。`observedAgeMilliseconds` 是现有窗口实际使用的时间差，不是纯相机处理耗时。`receivedAt` 记录程序收到相机事件的时间，`processingAt` 记录消费/绑定时刻；SDK `triggerIndex` 缺失时不要把本机 `cameraOrdinal` 当作厂商提供的可靠映射。

NoRead 开启后仍走正常分拣规则；日志会保留 NoRead 的绑定和完成证据。关闭后同样消费自己的触发位置，记录丢弃原因，后续小车不顺延。异常口未配置时记录 `AbnormalExitNotConfigured`；已配置但无法发送时记录 `ConfiguredAbnormalExitUnavailable`，不记录虚假的发送成功。`sorting.reply.sent` 表示连接完成指令写入，不表示已收到物理落格确认。

两个 JSON 目标分别使用 20,000 和 4,096 条容量的异步队列；正常洪峰不会挤占异常队列。写盘在后台批处理，同一进程保持文件句柄，不在设备回调中开关文件。日志格式化、脱敏和目标故障隔离于业务流程。单个外部文本超过 16,384 字符时省略，保留原长度和省略标记；正文中的接口凭据隐藏。严重慢盘或磁盘故障时诊断日志可能丢失，业务队列继续处理；恢复后累计缺口由已有后台周期报告，不能把缺少一条日志直接当作业务没有执行。

普通链路目标按 10 ms 调度、每批至多 256 条写出；异常目标按 10 ms 调度、每批至多 128 条写出，实际延迟仍受设备负载影响。数据库单次实体图更新使用 EF 自带的原子提交，省去外层事务和保存点；故障注入测试验证多条指令失败时整组回滚。

可直接用下面的工具生成一个包裹的时间线，同时补入同一相机次序的关联事件：

```powershell
python eng/simulation/inspect-pipeline.py --log-root '客户端运行目录/logs' --package-timestamp 1791071765938
```

输出按包裹标识关联，保留来源文件和行号，去除完整链路与异常文件中的重复事件。也支持 `--cart-id`、`--barcode`、`--run-id` 和 `--output`；车号或条码筛选会扩展同一次包裹仅携带 Timestamp 的后续证据。`package.pipeline.reset` 用于区分同一进程停机前后的相机次序，不能跨停止记录拼接第 N 次结果；诊断日志已丢失时不能保证补入事件完整。这些查询只是离线读文件，不调用生产程序或数据库。
