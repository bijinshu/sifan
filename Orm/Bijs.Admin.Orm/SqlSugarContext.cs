using SqlSugar;
using System;

namespace Bijs.Admin.Orm
{
    public class SqlSugarContext : SqlSugarClient
    {
        public SqlSugarContext(SqlSugarDbConnectOption config) : base(config) { DbName = config.Name; Default = config.Default; }
        public string DbName { set; get; }
        public bool Default { set; get; }
    }
}
