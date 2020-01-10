using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Bijs.Admin.Util
{
    public class StringUtil
    {
        public static string RemoveSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value, @"\s+", string.Empty);
        }
    }
}
