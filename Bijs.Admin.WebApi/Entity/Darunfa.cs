using Bijs.Admin.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bijs.Admin.WebApi.Entity
{
    public class Darunfa
    {
        [Desc("主题")]
        public string Title { get; set; }
        [Desc("要   求")]
        public string Request { get; set; }
        [Desc("尺寸规格W*H")]
        public string Size { get; set; }
        [Desc("数量")]
        public string Num { get; set; }
        [Desc("单位")]
        public string Unit { get; set; }
        [Desc("单价")]
        public string Price { get; set; }
        [Desc("单价单位")]
        public string PriceUnit { get; set; }
        [Desc("小计")]
        public string Total { get; set; }
    }
}
