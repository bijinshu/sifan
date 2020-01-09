using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Bijs.Admin.Util.Encrypt
{
    public class MD5
    {
        public static string Encrypt(string text)
        {
            return Encrypt(Encoding.UTF8.GetBytes(text));
        }
        public static string Encrypt(System.IO.Stream inputStream)
        {

            using (var md5 = new MD5CryptoServiceProvider())
            {
                byte[] outputData = md5.ComputeHash(inputStream);
                string result = FormatConvertor.ToHexString(outputData);
                return result;
            }
        }

        public static string Encrypt(byte[] data)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                byte[] outputData = md5.ComputeHash(data);
                string result = FormatConvertor.ToHexString(outputData);
                return result;
            }
        }
    }
}
