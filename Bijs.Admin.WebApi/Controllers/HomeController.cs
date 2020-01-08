using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bijs.Admin.Dto;
using Bijs.Admin.Model;
using Bijs.Admin.Orm;
using Bijs.Admin.Util;
using Bijs.Admin.WebApi.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bijs.Admin.WebApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class HomeController : BaseController
    {
        SqlSugarDbContextFactory factory;
        public HomeController(SqlSugarDbContextFactory factory)
        {
            this.factory = factory;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public BaseOutput RefreshPrice([FromBody] string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Fail("输入参数不能为空");
            }
            var dic = new Dictionary<string, string>();
            var lines = Regex.Split(value, @"(\r\n)|\n");
            foreach (var item in lines)
            {
                var currentItem = Regex.Split(item, @"\s+");
                if (currentItem.Length == 2 && !string.IsNullOrEmpty(currentItem[0]) && !string.IsNullOrEmpty(currentItem[1]))
                {
                    dic[currentItem[0]] = currentItem[1];
                }
            }
            factory.Delete<PriceInfo>(f => true);
            var list = dic.Select(s => new PriceInfo
            {
                ItemName = s.Key,
                Price = Convert.ToDecimal(s.Value),
                CreateTime = DateTime.Now,
                ModifyTime = DateTime.Now
            }).ToList();
            factory.Insert(list);
            return Success();
        }



        // POST api/values
        [HttpPost]
        public ActionResult UploadExcelFile(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return Content("未上传文件");
            }
            if (files.Count != 1)
            {
                return Content("一次只能上传一个文件");
            }
            var formFile = files[0];
            if (formFile.Length == 0)
            {
                return Content("文件不能为空");
            }
            var extension = Path.GetExtension(formFile.FileName);
            var workBook = ExcelUtil.GetWorkbook(extension, formFile.OpenReadStream());
            var mapList = ExcelUtil.GetMap<Darunfa>();
            var headerRow = ExcelUtil.FindHeaderRow(workBook, mapList); //寻找标题行
            var list = ExcelUtil.ImportExcelFile<Darunfa>(headerRow, mapList);
            var dic = ExcelUtil.ImportExcelFile(workBook, "价格");
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item.Request) && dic.ContainsKey(item.Request))
                {
                    item.Price = dic[item.Request];
                    item.Price = string.IsNullOrWhiteSpace(item.Price) ? "0" : Regex.Replace(item.Price, @"\s+", string.Empty);
                    if (!string.IsNullOrWhiteSpace(item.Size))
                    {
                        item.Size = Regex.Replace(item.Size, @"\s+", string.Empty);
                        if (Regex.IsMatch(item.Size, @"\d+(\.\d+)?\*\d+(\.\d+)?"))
                        {
                            var size = Regex.Split(item.Size, @"\*+");
                            var total = Convert.ToDecimal(size[0]) * Convert.ToDecimal(size[1]) * Convert.ToDecimal(item.Num) * Convert.ToDecimal(item.Price);
                            total = decimal.Round(total, 2);
                            item.Total = (total == Convert.ToInt32(total) ? Convert.ToInt32(total) : total).ToString();
                        }
                    }
                }
            }
            var data = ExcelUtil.CreateExcel(extension, list, mapList, headerRow);
            return File(data, "application/vnd.ms-excel", $"{DateTime.Now.ToString("yyyyMMddHHmmss")}-{formFile.FileName}");
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
