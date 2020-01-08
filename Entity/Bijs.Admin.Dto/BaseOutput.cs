using System;

namespace Bijs.Admin.Dto
{
    public class BaseOutput
    {
        public BaseOutput(string code = "000000", string msg = "操作成功")
        {
            Code = code;
            Msg = msg;
        }
        public string Code { get; set; }
        public string Msg { get; set; }
    }
    public class BaseOutput<T> : BaseOutput
    {
        public T Data { get; set; }
    }
}
