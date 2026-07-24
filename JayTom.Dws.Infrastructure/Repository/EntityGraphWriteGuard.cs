using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JayTom.Dws.Infrastructure.Repository {

    /// <summary>
    /// 实体图写入守卫。
    /// </summary>
    internal static class EntityGraphWriteGuard {

        /// <summary>
        /// 清理实体图中指向父实体的反向导航，避免 EF 将已存在父实体当成新增实体插入。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entity">根实体。</param>
        public static void ClearDependentReferenceNavigations(DbContext context, object? entity) {
            ClearDependentReferenceNavigations(context, entity, new HashSet<object>(ReferenceComparer.Instance));
        }

        /// <summary>
        /// 清理实体集合中指向父实体的反向导航，避免批量写入时重复插入父实体。
        /// </summary>
        /// <typeparam name="TEntity">实体类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entities">实体集合。</param>
        public static void ClearDependentReferenceNavigations<TEntity>(
            DbContext context,
            IEnumerable<TEntity> entities) where TEntity : class {
            var visited = new HashSet<object>(ReferenceComparer.Instance);
            foreach (var entity in entities) {
                ClearDependentReferenceNavigations(context, entity, visited);
            }
        }

        /// <summary>
        /// 获取单列主键属性。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entityType">实体运行时类型。</param>
        /// <returns>单列主键属性；复合主键或无主键时返回空。</returns>
        public static IProperty? GetSinglePrimaryKeyProperty(DbContext context, Type entityType) {
            var primaryKey = FindEntityType(context, entityType)?.FindPrimaryKey();
            return primaryKey?.Properties.Count == 1
                ? primaryKey.Properties[0]
                : null;
        }

        /// <summary>
        /// 读取实体主键值。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="primaryKeyProperty">主键属性元数据。</param>
        /// <returns>主键值。</returns>
        public static object? GetPrimaryKeyValue(object entity, IProperty primaryKeyProperty) {
            var propertyInfo = primaryKeyProperty.PropertyInfo ??
                               entity.GetType().GetProperty(primaryKeyProperty.Name);
            return propertyInfo?.GetValue(entity);
        }

        /// <summary>
        /// 判断主键是否仍为默认值。
        /// </summary>
        /// <param name="value">主键值。</param>
        /// <param name="primaryKeyProperty">主键属性元数据。</param>
        /// <returns>是否默认值。</returns>
        public static bool IsDefaultPrimaryKeyValue(object? value, IProperty primaryKeyProperty) {
            if (value is null) {
                return true;
            }

            var clrType = Nullable.GetUnderlyingType(primaryKeyProperty.ClrType) ??
                          primaryKeyProperty.ClrType;
            var defaultValue = clrType.IsValueType ? Activator.CreateInstance(clrType) : null;
            return Equals(value, defaultValue);
        }

        /// <summary>
        /// 递归清理反向导航。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entity">当前实体。</param>
        /// <param name="visited">已访问实体集合。</param>
        private static void ClearDependentReferenceNavigations(
            DbContext context,
            object? entity,
            ISet<object> visited) {
            if (entity is null || !visited.Add(entity)) {
                return;
            }

            var entityType = FindEntityType(context, entity.GetType());
            if (entityType is null) {
                return;
            }

            foreach (var navigation in entityType.GetNavigations()) {
                var propertyInfo = navigation.PropertyInfo;
                if (propertyInfo is null) {
                    continue;
                }

                var value = propertyInfo.GetValue(entity);
                if (value is null) {
                    continue;
                }

                if (navigation is { IsOnDependent: true, IsCollection: false }) {
                    propertyInfo.SetValue(entity, null);
                    continue;
                }

                if (navigation.IsCollection && value is IEnumerable items) {
                    foreach (var item in items) {
                        ClearDependentReferenceNavigations(context, item, visited);
                    }
                    continue;
                }

                ClearDependentReferenceNavigations(context, value, visited);
            }
        }

        /// <summary>
        /// 查找实体元数据。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entityType">实体运行时类型。</param>
        /// <returns>实体元数据。</returns>
        private static IEntityType? FindEntityType(DbContext context, Type entityType) {
            return context.Model.FindEntityType(entityType) ??
                   context.Model.GetEntityTypes()
                       .FirstOrDefault(modelType => modelType.ClrType.IsAssignableFrom(entityType));
        }

        /// <summary>
        /// 按引用比较对象，避免实体重写相等性后影响循环检测。
        /// </summary>
        private sealed class ReferenceComparer : IEqualityComparer<object> {

            /// <summary>
            /// 单例比较器。
            /// </summary>
            public static readonly ReferenceComparer Instance = new();

            /// <summary>
            /// 判断两个对象是否为同一引用。
            /// </summary>
            /// <param name="x">左侧对象。</param>
            /// <param name="y">右侧对象。</param>
            /// <returns>是否同一引用。</returns>
            bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

            /// <summary>
            /// 获取对象引用哈希。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <returns>引用哈希。</returns>
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
