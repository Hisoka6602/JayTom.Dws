namespace JayTom.Dws.Application.UseCases;

/// <summary>要求在一个原子持久化边界内完成的应用写用例。</summary>
public interface ITransactionalApplicationCommand<TResult> : IApplicationCommand<TResult> { }
