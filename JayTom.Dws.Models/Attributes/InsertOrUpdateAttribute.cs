namespace JayTom.Dws.Models.Attributes;

/// <summary>
/// 标记批量插入或更新操作中允许更新的实体属性。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InsertOrUpdateAttribute : Attribute;
