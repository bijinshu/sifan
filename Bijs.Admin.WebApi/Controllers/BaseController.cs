using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bijs.Admin.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Bijs.Admin.WebApi.Controllers
{
    public abstract class BaseController : Controller
    {
        protected BaseOutput Output(string code, string msg)
        {
            return new BaseOutput(code, msg);
        }
        protected BaseOutput Success()
        {
            return new BaseOutput();
        }
        protected BaseOutput Fail(string msg = "操作失败")
        {
            return new BaseOutput("999999", msg);
        }
    }
}