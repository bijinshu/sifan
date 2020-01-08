using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bijs.Admin.Util;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace Bijs.Admin.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var workboot = ExcelUtil.GetWorkbook(".xls", new FileStream(@"C:\Users\Administrator\Desktop\2001档期费用-1.xls", FileMode.Open));
            //var dic = ExcelUtil.ImportExcelFile(workboot, "价格");

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       //logging.SetMinimumLevel(LogLevel.Trace);
                   })
                   .UseNLog()
                   .UseStartup<Startup>();
    }
}
