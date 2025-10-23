using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace JayTom.Dws.Infrastructure {

    /// <summary>
    /// 数据库连接池优化配置扩展
    /// Database connection pool optimization configuration extensions
    /// </summary>
    public static class DbContextPoolingExtensions {

        /// <summary>
        /// 配置优化的数据库连接池
        /// Configure optimized database connection pool
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="poolSize">连接池大小（默认128）</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddOptimizedCloudApiDbContextFactory(
            this IServiceCollection services,
            string connectionString,
            int poolSize = 128) {

            services.AddPooledDbContextFactory<CloudApiContext>(options => {
                ConfigureDbContextOptions(options, connectionString);
            }, poolSize: poolSize);

            return services;
        }

        /// <summary>
        /// 配置 DbContext 选项以实现最佳性能
        /// Configure DbContext options for optimal performance
        /// </summary>
        private static void ConfigureDbContextOptions(
            DbContextOptionsBuilder options,
            string connectionString) {

            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySqlOptions => {
                    // 启用连接重试机制
                    // Enable connection retry mechanism
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    // 设置命令超时时间（30秒）
                    // Set command timeout (30 seconds)
                    mySqlOptions.CommandTimeout(30);

                    // 启用字符串比较转换
                    // Enable string comparison translations
                    mySqlOptions.EnableStringComparisonTranslations();

                    // 启用索引特性支持
                    // Enable index attribute support
                    mySqlOptions.EnableIndexOptimizedBooleanColumns();

                    // 配置字符集
                    // Configure character set
                    mySqlOptions.CharSet(CharSet.Utf8Mb4);
                })
                // 使用无跟踪查询提升性能
                // Use no-tracking queries for better performance
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
                
                // 在生产环境中关闭敏感数据日志记录
                // Disable sensitive data logging in production
                .EnableSensitiveDataLogging(false)
                
                // 启用服务提供程序缓存
                // Enable service provider caching
                .EnableServiceProviderCaching()
                
                // 配置警告忽略
                // Configure warning suppressions
                .ConfigureWarnings(warnings => {
                    // 忽略多个集合 Include 警告（因为我们使用投影查询）
                    // Ignore multiple collection include warnings (since we use projection queries)
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
                });
        }

        /// <summary>
        /// 获取优化的连接字符串建议
        /// Get optimized connection string recommendations
        /// </summary>
        /// <param name="server">服务器地址</param>
        /// <param name="port">端口</param>
        /// <param name="database">数据库名</param>
        /// <param name="user">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>优化的连接字符串</returns>
        public static string GetOptimizedConnectionString(
            string server,
            int port,
            string database,
            string user,
            string password) {

            // 优化的连接字符串参数说明：
            // Optimized connection string parameter descriptions:
            // 
            // Pooling=true              - 启用连接池 (Enable connection pooling)
            // MinPoolSize=5             - 最小连接池大小 (Minimum pool size)
            // MaxPoolSize=100           - 最大连接池大小 (Maximum pool size)
            // ConnectionLifeTime=300    - 连接生命周期（秒）(Connection lifetime in seconds)
            // ConnectionTimeout=30      - 连接超时时间（秒）(Connection timeout in seconds)
            // ConnectionIdleTimeout=180 - 空闲连接超时（秒）(Idle connection timeout in seconds)
            // AllowUserVariables=true   - 允许用户变量 (Allow user variables)
            // UseAffectedRows=false     - 使用匹配行数而非影响行数 (Use matched rows instead of affected rows)

            return $"Server={server};" +
                   $"Port={port};" +
                   $"Database={database};" +
                   $"User={user};" +
                   $"Password={password};" +
                   "Pooling=true;" +
                   "MinPoolSize=5;" +
                   "MaxPoolSize=100;" +
                   "ConnectionLifeTime=300;" +
                   "ConnectionTimeout=30;" +
                   "ConnectionIdleTimeout=180;" +
                   "AllowUserVariables=true;" +
                   "UseAffectedRows=false;";
        }

        /// <summary>
        /// 数据库连接池性能监控建议
        /// Database connection pool performance monitoring recommendations
        /// </summary>
        public static class PerformanceRecommendations {
            
            /// <summary>
            /// 建议的连接池大小配置
            /// Recommended connection pool size configuration
            /// </summary>
            public static class PoolSize {
                public const int Small = 32;     // 适用于小型应用 (For small applications)
                public const int Medium = 64;    // 适用于中型应用 (For medium applications)
                public const int Large = 128;    // 适用于大型应用 (For large applications)
                public const int ExtraLarge = 256; // 适用于超大型应用 (For extra-large applications)
            }

            /// <summary>
            /// 建议的最大连接池大小
            /// Recommended maximum pool size
            /// </summary>
            public static class MaxPoolSize {
                public const int Small = 50;
                public const int Medium = 100;
                public const int Large = 200;
                public const int ExtraLarge = 500;
            }

            /// <summary>
            /// 建议的命令超时时间（秒）
            /// Recommended command timeout (seconds)
            /// </summary>
            public static class CommandTimeout {
                public const int Fast = 15;      // 快速查询 (Fast queries)
                public const int Normal = 30;    // 正常查询 (Normal queries)
                public const int Slow = 60;      // 慢查询 (Slow queries)
                public const int VeryLong = 120; // 长时间运行的查询 (Long-running queries)
            }
        }
    }
}
