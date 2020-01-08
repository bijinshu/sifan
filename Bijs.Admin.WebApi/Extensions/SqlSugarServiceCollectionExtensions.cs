using Bijs.Admin.Orm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bijs.Admin.WebApi.Extensions
{
    public static class SqlSugarServiceCollectionExtensions
    {
        /// <summary>
        /// 添加sqlSugar多数据库支持
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="contextLifetime"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public static IServiceCollection AddSqlSugarDbContext(this IServiceCollection services, IConfiguration configuration, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, string section = "DbConfig")
        {
            var connectOptions = configuration.GetSection(section).Get<List<SqlSugarDbConnectOption>>();
            if (connectOptions != null)
            {
                foreach (var option in connectOptions)
                {
                    if (contextLifetime == ServiceLifetime.Scoped)
                        services.AddScoped(s => new SqlSugarContext(option));
                    if (contextLifetime == ServiceLifetime.Singleton)
                        services.AddSingleton(s => new SqlSugarContext(option));
                    if (contextLifetime == ServiceLifetime.Transient)
                        services.AddTransient(s => new SqlSugarContext(option));
                }
                if (contextLifetime == ServiceLifetime.Singleton)
                    services.AddSingleton<SqlSugarDbContextFactory>();
                if (contextLifetime == ServiceLifetime.Scoped)
                    services.AddScoped<SqlSugarDbContextFactory>();
                if (contextLifetime == ServiceLifetime.Transient)
                    services.AddTransient<SqlSugarDbContextFactory>();
            }
            return services;
        }
    }
}
