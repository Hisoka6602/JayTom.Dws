# 既有 SQLite 数据库兼容策略

本次整改采用“CLR 语义升级、物理结构不变”的方式，现有 `Data.db`、`Configuration.db` 和 `ClientLogs.db` 可原位继续使用。

## 存储位置与自动迁移

- 三个 SQLite 数据库固定保存在可执行程序所在目录（`AppContext.BaseDirectory`）。
- 数据库目录是必填部署配置，禁止缺省回退到 `%LOCALAPPDATA%` 等系统用户目录。
- 程序目录已有同名数据库时直接使用，自动迁移绝不覆盖现有文件。
- 程序目录没有数据库时，按显式 `DWS_LEGACY_DATA_DIRECTORY`、上一版本 `%LOCALAPPDATA%\JayTom\Dws\data`、当前工作目录的顺序查找历史库。
- 发现历史库后通过 SQLite 在线备份生成完整快照，合并已提交的 WAL 数据，经 `PRAGMA quick_check` 校验后原子落盘；源数据库始终保留。
- 部署目录必须授予运行账户创建和写入数据库、WAL 与 SHM 文件的权限。

## 不变项

- 不重命名既有业务表、列、索引或数据库文件。
- 主键和外键继续使用 SQLite `INTEGER`；CLR 统一映射为 `long`。
- 既有小数列继续保留 SQLite `REAL` 声明类型；CLR 和业务计算统一使用 `decimal`。
- 既有本地 `DateTime` 值不批量换算、不改写为 UTC。
- 旧 JSON 字段、供应商字段和 `{TimestampedGuid}` 宏通过显式映射继续兼容。

## 升级过程

1. 上下文从实际连接解析数据库文件路径。
2. 对历史上由 `EnsureCreated` 创建、没有迁移历史表的文件先执行兼容检查。
3. 执行 `202608110001_FixedPointCompatibility` 和 `202608110002_ModelSemanticsCompatibility` 空迁移，仅登记版本，不执行建表、改列或搬迁数据。
4. 保留现有读取索引并以 `CREATE INDEX IF NOT EXISTS` 补齐缺失索引。

## 自动验证

`SqliteCompatibilityTests` 先创建旧式 `INTEGER/REAL` 表，写入超过 32 位范围的主键和历史 REAL 小数，再通过当前 EF 模型读取、更新并验证：

- `Id` 和 `PackageId` 的声明类型仍为 `INTEGER`；
- `FormattedWeight` 的声明类型仍为 `REAL`；
- 原始 `CREATE TABLE` SQL 完全不变；
- `long` 和 `decimal` 可正常读写；
- 兼容迁移只写入迁移历史。
