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
using Bijs.Admin.Util.Encrypt;
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

        [HttpGet, HttpGet("/")]
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

        [HttpPost]
        public BaseOutput UploadExcelFile(List<IFormFile> files, string priceSheetName)
        {
            if (files == null || files.Count == 0) return Fail("未上传文件");
            if (files.Count != 1) return Fail("一次只能上传一个文件");
            var formFile = files[0];
            if (formFile.Length == 0) return Fail("文件不能为空");
            var now = DateTime.Now;
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "files", now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var fileName = $"{MD5.Encrypt(formFile.OpenReadStream())}-{formFile.FileName}";
            if (!System.IO.File.Exists(Path.Combine(directory, fileName)))
            {
                var extension = Path.GetExtension(formFile.FileName);
                var workBook = ExcelUtil.GetWorkbook(extension, formFile.OpenReadStream());
                var dic = ExcelUtil.ImportExcelFile(workBook, priceSheetName);
                if (dic.Count == 0) return Fail("价格列表为空");
                if (dic.Values.Any(f => !Regex.IsMatch(StringUtil.RemoveSpace(f), @"\d+(\.\d+)?"))) return Fail("价格不格式不正确");
                var mapList = ExcelUtil.GetMap<Darunfa>();
                var headerRow = ExcelUtil.FindHeaderRow(workBook, mapList); //寻找标题行
                var list = ExcelUtil.ImportExcelFile<Darunfa>(headerRow, mapList);
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
                if (!System.IO.File.Exists(Path.Combine(directory, fileName))) System.IO.File.WriteAllBytes(Path.Combine(directory, fileName), data);
            }
            var output = new BaseOutput<string>();
            output.Data = $"/files/{now.ToString("yyyy")}/{now.ToString("MM")}/{now.ToString("dd")}/{fileName}";
            return output;
        }
    }
}
