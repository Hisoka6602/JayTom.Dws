using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace JayTom.Dws.Domain.Specifications {
    /// <summary>
    /// 查询规范基类
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseSpecification<T> : ISpecification<T> {
        protected BaseSpecification() {
        }

        protected BaseSpecification(Expression<Func<T, bool>> criteria) {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>>? Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public List<string> IncludeStrings { get; } = new();
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }
        public int Skip { get; private set; }
        public int Take { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        public bool IsTrackingEnabled { get; private set; } = false;

        /// <summary>
        /// 添加 Include 表达式
        /// </summary>
        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) {
            Includes.Add(includeExpression);
        }

        /// <summary>
        /// 添加 Include 字符串
        /// </summary>
        protected virtual void AddInclude(string includeString) {
            IncludeStrings.Add(includeString);
        }

        /// <summary>
        /// 添加排序（升序）
        /// </summary>
        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) {
            OrderBy = orderByExpression;
        }

        /// <summary>
        /// 添加排序（降序）
        /// </summary>
        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression) {
            OrderByDescending = orderByDescendingExpression;
        }

        /// <summary>
        /// 应用分页
        /// </summary>
        protected virtual void ApplyPaging(int skip, int take) {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        /// <summary>
        /// 启用实体跟踪
        /// </summary>
        protected virtual void EnableTracking() {
            IsTrackingEnabled = true;
        }
    }
}
