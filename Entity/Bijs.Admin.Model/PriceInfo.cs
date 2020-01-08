using SqlSugar;
using System;

namespace Bijs.Admin.Model
{
    [SugarTable("Price")]
    public class PriceInfo
    {
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? ModifyTime { get; set; }
    }
}
