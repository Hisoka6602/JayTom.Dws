# ADR-0003：持久化所有权

- 状态：已接受
- 日期：2026-08-11

## 决策

持久化服务注册、DbContext 构造和仓储上下文生命周期由 Infrastructure 所有。Client 只调用 Infrastructure 暴露的注册扩展，不维护仓储注册清单。

## 结果

`RepositoryBase` 与 `LocalRepositoryBase` 共享 `RepositoryContextBase`，上下文工厂、缓存和键校验只有一个实现位置；内存缓存仓储不再重复持有缓存字段。
