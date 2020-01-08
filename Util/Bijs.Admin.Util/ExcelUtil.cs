using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bijs.Admin.Util
{
    public class ExcelUtil
    {
        /// <summary>
        /// 创建Excel文件
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="fileExtension">指定文件类型（.xls或者.xlsx）</param>
        /// <param name="list">实体列表</param>
        /// <param name="mapList">导出文件标题与实体类属性名称映射</param>
        /// <returns></returns>
        public static byte[] CreateExcel<T>(string fileExtension, List<T> list)
        {
            var mapList = GetMap<T>();
            //Create new Excel Workbook
            int maxRowCount = fileExtension == ".xls" ? 65535 : int.MaxValue;
            int maxSheetCount = list.Count / maxRowCount + 1;
            var workbook = GetWorkbook(fileExtension);
            var insType = typeof(T);
            for (int i = 0; i < maxSheetCount; i++)
            {
                var sheet = workbook.CreateSheet(string.Format("第{0}页", i + 1)); //创建表单
                var headerRow = sheet.CreateRow(0); //创建标题行
                for (int ii = 0; ii < mapList.Count; ii++)
                {
                    headerRow.CreateCell(ii).SetCellValue(mapList[ii].ExcelColumnTitle);
                }
                sheet.CreateFreezePane(0, 1, 0, 1); //冻结标题行
                int startIndex = i * maxRowCount;
                for (int j = 0; j < maxRowCount; j++) //每次表单中最多只创建maxRowCount行数据
                {
                    if (startIndex + j < list.Count)
                    {
                        var row = sheet.CreateRow(j + 1);
                        var item = list[startIndex + j];
                        for (int k = 0; k < mapList.Count; k++)//为每行的列赋值
                        {
                            var prop = insType.GetProperty(mapList[k].EntityPropName, BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null)
                            {
                                throw new Exception($"属性{mapList[k].EntityPropName}不存在");
                            }
                            var value = prop.GetValue(item, null);
                            row.CreateCell(k).SetCellValue((value ?? string.Empty).ToString());
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //Write the Workbook to a memory stream
            using (MemoryStream output = new MemoryStream())
            {
                workbook.Write(output);
                return output.ToArray();
            }
        }

        /// <summary>
        /// 获取文件行数
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static int GetRowCount(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath);
            var workbook = GetWorkbook(fileExtension, new FileStream(filePath, FileMode.Open, FileAccess.Read));
            var rowCount = 0;
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                rowCount += (sheet.LastRowNum - sheet.FirstRowNum);//排除标题行
            }
            return rowCount;
        }

        /// <summary>
        /// 导入Excel文件
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="fileExtension">文件扩展名</param>
        /// <param name="mapList">导入文件标题与实体类属性名称映射</param>
        /// <returns></returns>
        public static List<T> ImportExcelFile<T>(IWorkbook workbook, string sheetName) where T : class, new()
        {
            var mapList = GetMap<T>();
            var result = new List<T>();
            ISheet sheet = GetSheet(workbook, sheetName);
            IRow headerRow = sheet.GetRow(0); //第一行为标题行
            int cellCount = headerRow.LastCellNum; //LastCellNum = PhysicalNumberOfCells
            int rowCount = sheet.LastRowNum; //LastRowNum = PhysicalNumberOfRows - 1
            var properties = typeof(T).GetProperties();
            var dic = new Dictionary<int, PropertyInfo>();
            //handling header.
            for (int i = headerRow.FirstCellNum; i < cellCount; i++)
            {
                var excelHeadName = headerRow.GetCell(i).StringCellValue;
                var entity = mapList.FirstOrDefault(f => f.ExcelColumnTitle == excelHeadName);
                if (entity != null && properties.Any(a => a.Name == entity.EntityPropName))
                {
                    dic[i] = properties.Single(f => f.Name == entity.EntityPropName);
                }
            }
            if (dic.Count != mapList.Count)
            {
                throw new Exception($"Excel中，仅允许如下标题：[{string.Join(',', mapList.Select(t => t.ExcelColumnTitle))}]");
            }
            for (int i = (sheet.FirstRowNum + 1); i <= rowCount; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row != null)
                {
                    var entity = new T();
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        if (row.GetCell(j) != null && dic.ContainsKey(j))
                        {
                            var property = dic[j];
                            property.SetValue(entity, GetCellValue(row.GetCell(j)));
                        }
                    }
                    result.Add(entity);
                }
            }
            return result;
        }

        public static Dictionary<string, string> ImportExcelFile(IWorkbook workbook, string sheetName)
        {
            ISheet sheet = GetSheet(workbook, sheetName);
            var dic = new Dictionary<string, string>();
            for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row != null)
                {
                    var column0 = GetCellValue(row.GetCell(0));
                    var column1 = GetCellValue(row.GetCell(1));
                    if (!string.IsNullOrEmpty(column0) && !string.IsNullOrEmpty(column1))
                    {
                        dic[column0] = column1;
                    }
                }
            }
            return dic;
        }

        public static List<ExcelEntityMap> GetMap<T>()
        {
            var list = new List<ExcelEntityMap>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                var attribute = prop.GetCustomAttribute<DescAttribute>();
                if (attribute == null)
                {
                    throw new Exception($"属性{prop.Name}未定义DescAttribute");
                }
                else if (string.IsNullOrEmpty(attribute.Value))
                {
                    throw new Exception($"属性{prop.Name}的Desc注解值不能为空");
                }
                else
                {
                    list.Add(new ExcelEntityMap(attribute.Value, prop.Name));
                }
            }
            return list;
        }
        /// <summary>
        /// 查询表单
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public static ISheet GetSheet(IWorkbook workbook, string sheetName = null)
        {
            ISheet sheet = string.IsNullOrEmpty(sheetName) ? workbook.GetSheetAt(0) : workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                throw new Exception($"表单{sheetName}不存在");
            }
            return sheet;
        }

        /// <summary>
        /// 获取工作表单对象
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <param name="s">输入流</param>
        /// <returns></returns>
        public static IWorkbook GetWorkbook(string fileExtension, Stream s = null)
        {
            IWorkbook workBook = null;
            switch (fileExtension)
            {
                case ".xls":
                    workBook = s == null ? new HSSFWorkbook() : new HSSFWorkbook(s);
                    break;
                case ".xlsx":
                    workBook = s == null ? new XSSFWorkbook() : new XSSFWorkbook(s);
                    break;
            }
            if (workBook == null)
            {
                throw new Exception("不支持的Excel类型");
            }
            return workBook;
        }

        /// <summary>
        /// 根据Excel列类型获取列的值
        /// </summary>
        /// <param name="cell">Excel列</param>
        /// <returns></returns>
        private static string GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return string.Empty;
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Error:
                    return cell.ErrorCellValue.ToString();
                case CellType.Numeric:
                    return DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue.ToString() : cell.NumericCellValue.ToString();
                case CellType.Unknown:
                default:
                    return cell.ToString();
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Formula:
                    try
                    {
                        HSSFFormulaEvaluator e = new HSSFFormulaEvaluator(cell.Sheet.Workbook);
                        e.EvaluateInCell(cell);
                        return cell.ToString();
                    }
                    catch
                    {
                        return cell.NumericCellValue.ToString();
                    }
            }
        }

    }
}
