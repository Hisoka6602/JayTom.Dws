namespace JayTom.Dws.Application.Storage;

/// <summary>表示数据库可保存、但不暴露物理路径的二进制资源引用。</summary>
public readonly record struct BinaryAssetReference(string Value);
