using System;
using System.Collections.Generic;
using System.Text;

namespace Bijs.Admin.Util
{
    public class ExcelEntityMap
    {
        public ExcelEntityMap(string excelColumnTitle, string entityPropName)
        {
            ExcelColumnTitle = excelColumnTitle;
            EntityPropName = entityPropName;
        }
        /// <summary>
        /// excel列标题
        /// </summary>
        public string ExcelColumnTitle { get; set; }
        /// <summary>
        /// 实体属性名称
        /// </summary>
        public string EntityPropName { get; set; }
    }
}
