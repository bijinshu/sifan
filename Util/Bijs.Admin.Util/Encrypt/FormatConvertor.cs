using System;
using System.Collections.Generic;
using System.Text;

namespace Bijs.Admin.Util.Encrypt
{
    public class FormatConvertor
    {
        public static string ToBase64String(byte[] text)
        {
            return Convert.ToBase64String(text);
        }

        public static string ToBase64String(string text)
        {
            return ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static byte[] FromBase64String(string base64String)
        {
            return Convert.FromBase64String(base64String);
        }

        public static string ToHexString(byte[] text)
        {
            StringBuilder ret = new StringBuilder();
            foreach (byte b in text)
            {
                ret.AppendFormat("{0:x2}", b);
            }
            return ret.ToString();
        }

        public static string ToHexString(string text)
        {
            return ToHexString(Encoding.UTF8.GetBytes(text));
        }

        public static byte[] FromHexString(string hexString)
        {
            byte[] data = null;
            if (!string.IsNullOrEmpty(hexString))
            {
                int length = hexString.Length / 2;
                data = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    data[i] = Convert.ToByte(hexString.Substring(2 * i, 2), 16);
                }
            }
            return data;
        }
    }
}
