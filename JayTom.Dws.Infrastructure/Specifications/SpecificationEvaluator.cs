using System.Linq;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Domain.Specifications;

namespace JayTom.Dws.Infrastructure.Specifications {
    /// <summary>
    /// 规范评估器 - 将规范转换为 EF Core 查询
    /// </summary>
    public static class SpecificationEvaluator<T> where T : class {
        /// <summary>
        /// 获取应用了规范的查询
        /// </summary>
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification) {
            var query = inputQuery;

            // 应用跟踪设置
            if (!specification.IsTrackingEnabled) {
                query = query.AsNoTracking();
            }

            // 应用查询条件
            if (specification.Criteria != null) {
                query = query.Where(specification.Criteria);
            }

            // 应用 Include
            query = specification.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            // 应用字符串 Include
            query = specification.IncludeStrings.Aggregate(query,
                (current, include) => current.Include(include));

            // 应用排序
            if (specification.OrderBy != null) {
                query = query.OrderBy(specification.OrderBy);
            } else if (specification.OrderByDescending != null) {
                query = query.OrderByDescending(specification.OrderByDescending);
            }

            // 应用分页
            if (specification.IsPagingEnabled) {
                query = query.Skip(specification.Skip).Take(specification.Take);
            }

            return query;
        }
    }
}
