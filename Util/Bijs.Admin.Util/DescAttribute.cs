using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bijs.Admin.Util
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DescAttribute:Attribute
    {
        public string Value { get; set; }
        public DescAttribute()
        {

        }
        public DescAttribute(string value)
        {
            Value = value;
        }
    }
}
