using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Bijs.Admin.Orm
{
    public class SqlSugarDbContextFactory
    {
        private readonly IEnumerable<SqlSugarContext> clients = null;
        protected SqlSugarContext currentDb = null;
        private const string currentAsDefault = "currentAsDefault";
        protected ILogger logger = LogManager.GetCurrentClassLogger();
        public SqlSugarDbContextFactory(IEnumerable<SqlSugarContext> clients, string dbName = null)
        {
            this.clients = clients;
            ChangeDb(dbName);
        }
        /// <summary>
        /// 切换数据库，不传参数时切换为默认数据库
        /// </summary>
        /// <param name="dbName"></param>
        public void ChangeDb(string dbName = null)
        {
            var db = GetDb(dbName);
            if (currentDb == null || currentDb.DbName != db.DbName)
            {
                currentDb = db;
            }
        }
        /// <summary>
        /// 获取数据库，不传参数时返回默认数据库
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public SqlSugarContext GetDb(string dbName = null)
        {
            if (currentDb != null && (dbName == currentAsDefault || (string.IsNullOrEmpty(dbName) && currentDb.Default) || currentDb.DbName == dbName))
            {
                return currentDb;
            }
            var client = string.IsNullOrEmpty(dbName) ? clients.FirstOrDefault(f => f.Default) : clients.FirstOrDefault(f => f.DbName == dbName);
            if (client == null)
            {
                throw new Exception($"未发现该数据库：{dbName}");
            }
            WatchSql(client);
            return client;
        }
        public SqlSugarContext CurrentDb { get { return GetDb(); } }
        public ISugarQueryable<T> Queryable<T>(string dbName = currentAsDefault) where T : class, new()
        {
            return GetDb(dbName).Queryable<T>();
        }
        public bool Update<T>(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression, string dbName = currentAsDefault) where T : class, new()
        {
            return GetDb(dbName).Updateable<T>().SetColumns(columns).Where(whereExpression).ExecuteCommand() > 0;
        }
        public bool Insert<T>(T insertObj, bool returnIdentity = false, string dbName = currentAsDefault) where T : class, new()
        {
            var insertable = GetDb(dbName).Insertable(insertObj);
            return returnIdentity ? insertable.ExecuteCommandIdentityIntoEntity() : insertable.ExecuteCommand() > 0;
        }
        public bool Insert<T>(List<T> insertObj, bool returnIdentity = false, string dbName = currentAsDefault) where T : class, new()
        {
            var insertable = GetDb(dbName).Insertable(insertObj);
            return returnIdentity ? insertable.ExecuteCommandIdentityIntoEntity() : insertable.ExecuteCommand() > 0;
        }

        public T Save<T>(T saveObj, Expression<Func<T, object>> updateColumns = null, string dbName = currentAsDefault) where T : class, new()
        {
            var saveable = GetDb(dbName).Saveable(saveObj);
            if (updateColumns != null)
            {
                saveable = saveable.UpdateColumns(updateColumns);
            }
            return saveable.ExecuteReturnEntity();
        }
        public List<T> Save<T>(List<T> saveObj, Expression<Func<T, object>> updateColumns = null, string dbName = currentAsDefault) where T : class, new()
        {
            var saveable = GetDb(dbName).Saveable(saveObj);
            if (updateColumns != null)
            {
                saveable = saveable.UpdateColumns(updateColumns);
            }
            return saveable.ExecuteReturnList();
        }
        public bool Delete<T>(Expression<Func<T, bool>> whereExpression, string dbName = currentAsDefault) where T : class, new()
        {
            return GetDb(dbName).Deleteable<T>().Where(whereExpression).ExecuteCommand() > 0;
        }

        private void WatchSql(SqlSugarClient dbContext)
        {
            dbContext.Aop.OnLogExecuting = (sql, pars) =>
            {
                if (logger != null)
                {
                    string paraStr = $"SQL语句：{sql}";
                    if (pars != null && pars.Length > 0)
                    {
                        var timeFormat = new IsoDateTimeConverter();
                        timeFormat.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        var dic = new Dictionary<string, object>();
                        foreach (var item in pars)
                        {
                            dic[item.ParameterName] = item.Value;
                        }
                        paraStr += $"\nSQL参数：{JsonConvert.SerializeObject(dic, Formatting.None, timeFormat)}";
                    }
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.Debug($"******************************即将执行SQL({dbContext.Ado.Connection.ConnectionString})******************************");
                        logger.Debug(paraStr);
                    }
                    else if (logger.IsEnabled(LogLevel.Info))
                    {
                        logger.Info(paraStr);
                    }
                }
            };
        }
    }
}
