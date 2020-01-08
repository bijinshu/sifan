using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bijs.Admin.Orm
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public class SqlSugarDbConnectOption : ConnectionConfig
    {
        /// <summary>
        /// 数据库连接名称
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// 是否默认库
        /// </summary>
        public bool Default { set; get; } = true;
    }
}
