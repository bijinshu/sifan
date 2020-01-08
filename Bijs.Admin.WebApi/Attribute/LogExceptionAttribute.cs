using Bijs.Admin.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Bijs.Admin.WebApi
{
    public class LogExceptionAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.ExceptionHandled = true;
            var logger = NLog.LogManager.GetLogger(HttpUtility.UrlDecode(context.HttpContext.Request.Path.Value, Encoding.UTF8));
            var error = context.Exception.Message;
            logger.Error(error);
            context.Result = new JsonResult(new BaseOutput("999999", error));
        }
    }
}
