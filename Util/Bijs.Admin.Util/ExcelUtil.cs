﻿using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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
        public static byte[] CreateExcel<T>(string fileExtension, List<T> list, List<ExcelEntityMap> mapList, IRow oldHeadRow = null)
        {
            //Create new Excel Workbook
            int maxRowCount = fileExtension == ".xls" ? 65535 : int.MaxValue;
            int maxSheetCount = list.Count / maxRowCount + 1;
            var workbook = GetWorkbook(fileExtension);
            var insType = typeof(T);
            for (int i = 0; i < maxSheetCount; i++)
            {
                var sheet = workbook.CreateSheet(string.Format("第{0}页", i + 1)); //创建表单
                var headerRow = sheet.CreateRow(0); //创建标题行
                if (oldHeadRow != null)
                {
                    headerRow.HeightInPoints = oldHeadRow.HeightInPoints;
                }
                for (int ii = 0; ii < mapList.Count; ii++)
                {
                    var cell = headerRow.CreateCell(ii);
                    cell.SetCellValue(mapList[ii].ExcelColumnTitle);
                    if (oldHeadRow != null)
                    {
                        var oldCell = oldHeadRow.FirstOrDefault(f => f != null && f.CellType == CellType.String && StringUtil.RemoveSpace(f.StringCellValue) == cell.StringCellValue);
                        if (oldCell != null)
                        {
                            var cellStyle = workbook.CreateCellStyle();
                            cellStyle.CloneStyleFrom(oldCell.CellStyle);
                            cell.CellStyle = cellStyle;
                            sheet.SetColumnWidth(cell.ColumnIndex, oldCell.Sheet.GetColumnWidth(oldCell.ColumnIndex));
                        }
                    }
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
                            var cell = row.CreateCell(k);
                            cell.SetCellValue((value ?? string.Empty).ToString());
                            cell.CellStyle.Alignment = HorizontalAlignment.Center;
                            cell.CellStyle.VerticalAlignment = VerticalAlignment.Center;
                            cell.CellStyle.SetFont(headerRow.GetCell(cell.ColumnIndex).CellStyle.GetFont(workbook));
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
        public static List<T> ImportExcelFile<T>(IRow srcHeaderRow, IList<ExcelEntityMap> mapList) where T : class, new()
        {
            var result = new List<T>();
            ISheet srcSheet = srcHeaderRow.Sheet;
            int cellCount = srcHeaderRow.LastCellNum; //LastCellNum = PhysicalNumberOfCells
            int rowCount = srcSheet.LastRowNum; //LastRowNum = PhysicalNumberOfRows - 1
            var properties = typeof(T).GetProperties();
            var dic = new Dictionary<int, PropertyInfo>();
            //handling header.
            for (int i = srcHeaderRow.FirstCellNum; i < cellCount; i++)
            {
                var cell = srcHeaderRow.GetCell(i);
                if (cell != null && cell.CellType == CellType.String && !string.IsNullOrEmpty(cell.StringCellValue))
                {
                    var entity = mapList.SingleOrDefault(f => f.ExcelColumnTitle == StringUtil.RemoveSpace(cell.StringCellValue));
                    if (entity != null && properties.Any(a => a.Name == entity.EntityPropName))
                    {
                        dic[i] = properties.Single(f => f.Name == entity.EntityPropName);
                    }
                }
            }
            if (dic.Count != mapList.Count)
            {
                throw new Exception($"Excel中的标题应为：[{string.Join(',', mapList.Select(t => t.ExcelColumnTitle))}]");
            }
            for (int i = srcHeaderRow.RowNum + 1; i <= rowCount; i++)
            {
                IRow srcRow = srcSheet.GetRow(i);
                if (srcRow != null)
                {
                    var entity = new T();
                    for (int j = srcRow.FirstCellNum; j < cellCount; j++)
                    {
                        if (dic.ContainsKey(j) && srcRow.GetCell(j) != null)
                        {
                            var property = dic[j];
                            property.SetValue(entity, GetCellValue(srcRow.GetCell(j)));
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
                    if (!string.IsNullOrWhiteSpace(column0) && !string.IsNullOrWhiteSpace(column1))
                    {
                        dic[column0.Trim()] = column1.Trim();
                    }
                }
            }
            return dic;
        }

        public static IRow FindHeaderRow(IWorkbook workBook, IList<ExcelEntityMap> list, string sheetName = null)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                for (int i = 0; i < workBook.NumberOfSheets; i++)
                {
                    ISheet sheet = workBook.GetSheetAt(i);
                    IRow row = FindHeaderRow(list, sheet);
                    if (row != null) return row;
                }
            }
            else
            {
                ISheet sheet = workBook.GetSheet(sheetName);
                if (sheet != null)
                {
                    IRow row = FindHeaderRow(list, sheet);
                    if (row != null) return row;
                }
            }
            throw new Exception("未找到标题行");
        }

        public static IRow FindHeaderRow(IList<ExcelEntityMap> list, ISheet sheet)
        {
            if (sheet.FirstRowNum < sheet.LastRowNum)
            {
                for (int j = sheet.FirstRowNum; j <= sheet.LastRowNum; j++)
                {
                    var row = sheet.GetRow(j);
                    var headers = row.Select(s => GetCellValue(s)).Where(f => !string.IsNullOrEmpty(f.Trim())).Select(s => StringUtil.RemoveSpace(s)).ToList();
                    if (list.All(a => headers.Contains(a.ExcelColumnTitle)))
                    {
                        return row;
                    }
                }
            }
            return null;
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
                    list.Add(new ExcelEntityMap(StringUtil.RemoveSpace(attribute.Value), prop.Name));
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
                case CellType.String:
                    return cell.StringCellValue.Trim();
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
                case CellType.Unknown:
                default:
                    return cell.ToString().Trim();
            }
        }
    }
}
