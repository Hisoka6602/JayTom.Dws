using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace JayTom.Dws.Domain.Specifications {
    /// <summary>
    /// 查询规范接口，用于封装查询逻辑
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface ISpecification<T> {
        /// <summary>
        /// 查询条件
        /// </summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// Include 表达式列表（用于急切加载）
        /// </summary>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Include 字符串列表（用于字符串形式的 Include）
        /// </summary>
        List<string> IncludeStrings { get; }

        /// <summary>
        /// 排序表达式（升序）
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// 排序表达式（降序）
        /// </summary>
        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// 分页起始位置
        /// </summary>
        int Skip { get; }

        /// <summary>
        /// 分页大小
        /// </summary>
        int Take { get; }

        /// <summary>
        /// 是否启用分页
        /// </summary>
        bool IsPagingEnabled { get; }

        /// <summary>
        /// 是否启用跟踪
        /// </summary>
        bool IsTrackingEnabled { get; }
    }
}
