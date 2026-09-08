using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PerillaTable
{
    public class SheetInfo
    {
        public string SheetName;
        public string ClassName;
        public int RowCount;
        public int ColumnCount;
        public int SheetIndex;
        public DateTime ModifyTime;
    }

    //导出严重错误：定位到Excel行/列(从1开始)；Row=0表示整表/文件级错误
    public class ExportError
    {
        public string FilePath = "";
        public string SheetName = "";
        public int Row;
        public int Column;
        public string Reason = "";

        public ExportError(string filePath, string sheetName, int row, int column, string reason)
        {
            FilePath = filePath;
            SheetName = sheetName;
            Row = row;
            Column = column;
            Reason = reason;
        }

        public override string ToString()
        {
            string loc = Row > 0
                ? $"行{Row}" + (Column > 0 ? $" 列{Column}" : "")
                : "整表";
            return $"{loc}: {Reason}";
        }
    }

    class ExcelTool
    {
        public ExcelTool()
        {
            InitClassStr();
        }

        private string classStr;
        private string propertyStr;
        private string parseBytes;

        //本次导出累计的严重错误(含被跳过的表)与成功导出的表数
        public readonly List<ExportError> ExportErrors = new();
        public int ExportedSheetCount;

        #region enum支持

        //enum定义：列名(小写) → (成员名(小写) → 序号)；来自 Enum.xlsx，每列一个枚举
        private Dictionary<string, Dictionary<string, int>>? enumTypesDic;
        private string? enumTypeCsContent;
        private bool enumDataLoaded;

        //加载 Enum.xlsx（每列一个枚举：第1行=枚举名，其后每行=成员名，序号从0递增）
        private void LoadEnumData()
        {
            if (enumDataLoaded)
                return;
            enumDataLoaded = true;
            enumTypesDic = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

            string enumPath = Config.I.excelPath + "Enum.xlsx";
            if (!File.Exists(enumPath))
            {
                Logger.Error($"LoadEnumData: 未找到 {enumPath}，enum 类型列将无法导出");
                return;
            }

            List<DataTable>? result = ExcelToDataTable(enumPath);
            if (result == null || result.Count == 0)
            {
                Logger.Error("LoadEnumData: Enum.xlsx 解析失败或没有可用sheet");
                return;
            }

            DataTable curSheet = result[0];
            var fileStr = new StringBuilder();

            for (int j = 0; j < curSheet.Columns.Count; ++j)
            {
                string enumName = curSheet.Rows[0][j].ToString().Trim();
                if (string.IsNullOrEmpty(enumName))
                    continue;

                var members = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var memberNames = new List<string>();
                for (int i = 1; i < curSheet.Rows.Count; ++i)
                {
                    string member = curSheet.Rows[i][j].ToString().Trim();
                    if (string.IsNullOrEmpty(member))
                        continue;
                    if (members.ContainsKey(member))
                        continue;

                    members.Add(member, members.Count);
                    memberNames.Add(UpperFirstLetter(member));
                }

                if (members.Count == 0)
                    continue;

                enumTypesDic.Add(enumName.ToLower(), members);

                fileStr.AppendLine($"public enum Enum{UpperFirstLetter(enumName)}");
                fileStr.AppendLine("{");
                for (int m = 0; m < memberNames.Count; ++m)
                {
                    if (m < memberNames.Count - 1)
                        fileStr.AppendLine($"    {memberNames[m]},");
                    else
                        fileStr.AppendLine($"    {memberNames[m]}");
                }
                fileStr.AppendLine("}");
                fileStr.AppendLine();
            }

            enumTypeCsContent = fileStr.Length > 0 ? fileStr.ToString() : null;
            Logger.Log($"LoadEnumData: 已加载 {enumTypesDic.Count} 个枚举定义 ({Path.GetFileName(enumPath)})");
        }

        //生成 EnumType.cs 到 classPath（导出流程调用；校验流程只加载映射不写文件）
        private void GenerateEnumTypeFile()
        {
            if (enumTypeCsContent == null)
                return;
            FileTool.WriteString($"{Config.I.classPath}EnumType.cs", enumTypeCsContent);
            Logger.Log("EnumType.cs Created");
        }

        //查表：enum列的成员名 → 序号；缺失时报错返回0（严格校验通过时不会走到这里）
        private int GetEnumValue(string enumName, string memberValue)
        {
            if (string.IsNullOrEmpty(memberValue))
            {
                Logger.Error($"enum '{enumName}' 出现空值，按 0 导出");
                return 0;
            }

            if (enumTypesDic != null &&
                enumTypesDic.TryGetValue(enumName.ToLower(), out var members) &&
                members.TryGetValue(memberValue.ToLower(), out int index))
            {
                return index;
            }

            Logger.Error($"enum '{enumName}' 未定义成员 '{memberValue}'（检查 Enum.xlsx），按 0 导出");
            return 0;
        }

        #endregion

        public void CreateDataTable(string path)
        {
            //Enum.xlsx 是枚举定义表：只加载枚举映射并生成 EnumType.cs，不作为数据表导出
            if (Path.GetFileName(path).Equals("Enum.xlsx", StringComparison.OrdinalIgnoreCase))
            {
                LoadEnumData();
                GenerateEnumTypeFile();
                return;
            }

            LoadEnumData();

            string classPath = Config.I.classPath;
            List<DataTable> result = ExcelToDataTable(path, ExportErrors);
            if (result == null)
            {
                Logger.Error($"ExcelToDataTable:{path} NULL");
                return;
            }

            int tableNum = result.Count;
            for (int index = 0; index < tableNum; index++)
            {
                ProcessSheet(result[index], path, classPath);
            }
        }

        public void CreateDataTable(string path, int sheetIndex)
        {
            LoadEnumData();

            string classPath = Config.I.classPath;
            List<DataTable> result = ExcelToDataTable(path, ExportErrors);
            if (result == null)
            {
                Logger.Error($"ExcelToDataTable:{path} NULL");
                return;
            }

            int tableNum = result.Count;
            if (sheetIndex < 0 || sheetIndex >= tableNum)
            {
                Logger.Error($"CreateDataTable: sheetIndex {sheetIndex} out of range (0..{tableNum - 1})");
                return;
            }

            ProcessSheet(result[sheetIndex], path, classPath);
        }

        private void ProcessSheet(DataTable curSheet, string path, string classPath)
        {
            //类名优先取备注行首格的 #ClassName 标记（多表文件约定），无标记时用sheet名
            string className = curSheet.TableName;
            Match markerMatch = Regex.Match((curSheet.Rows[0][0].ToString() ?? "").Trim(), @"^#(\w+)$");
            if (markerMatch.Success)
                className = markerMatch.Groups[1].Value;

            //导出前严格校验：有严重错误的表整体跳过，不生成C#类和数据文件
            var sheetErrors = ValidateSheetStrict(curSheet, className);
            if (sheetErrors.Count > 0)
            {
                foreach (var err in sheetErrors)
                {
                    err.FilePath = path;
                    ExportErrors.Add(err);
                    Logger.Error($"{Path.GetFileName(path)} [{className}] {err}");
                }
                Logger.Error($"表 '{className}' 存在{sheetErrors.Count}个严重错误，已跳过不导出 ({Path.GetFileName(path)})");
                return;
            }

            int columns = curSheet.Columns.Count;
            int rows = curSheet.Rows.Count;

            //sheet名 = 类名
            string cshapClassStr = classStr;
            string parseBytesStr = parseBytes;
            cshapClassStr = cshapClassStr.Replace("className", className);

            string parseByBytes = "";

            #region 生成C#类
            //第1行备注(以#开头可禁用该列)、第2行name、第3行类型
            for (int i = 0; i < columns; ++i)
            {
                if (curSheet.Rows[0][i].ToString().StartsWith("#"))
                    continue;

                string dataType = curSheet.Rows[2][i].ToString();
                string dataName = UpperFirstLetter(curSheet.Rows[1][i].ToString());
                string add = "";

                if (dataType.EndsWith("[]"))
                {
                    add = dataType.Split('[')[0];
                    dataType = "array";
                }
                else if (dataType.StartsWith("dic"))
                {
                    dataType = dataType.Substring(0, dataType.Length - 1);
                    add = dataType.Split('<')[1];
                    dataType = "dic";
                }

                if (dataType == "dic")
                {
                    string[] tempStr3 = dataName.Split(':');
                    dataName = tempStr3[0];
                }

                    switch (dataType)
                    {
                        case "array":

                            string arrStr = @"
        public List<Type> arrName{ get; protected set; }
";
                            arrStr = arrStr.Replace("Type", add);
                            arrStr = arrStr.Replace("arrName", dataName);

                            cshapClassStr += arrStr;

                            string typeStr = "rd.ReadString()";
                            if (add == "int")
                                typeStr = "rd.ReadInt32()";
                            else if (add == "float")
                                typeStr = "rd.ReadSingle()";
                            else if (add == "bool")
                                typeStr = "rd.ReadBoolean()";

                            string countStr = "int count";
                            if (parseByBytes.Contains(countStr))
                                countStr = "count";

                            parseByBytes += $@"
            {countStr} = rd.ReadInt16();
            {dataName} = new List<{add}>();
            for(int i = 0; i < count; i++)
            {{
                {dataName}.Add({typeStr});
            }}
            ";
                            break;
                        case "dic":
                            {
                                string dicStr = @"
        public Dictionary<key,value> dicName{ get; protected set; }
";
                                string[] str1s = add.ToString().Split(',');
                                dicStr = dicStr.Replace("key", str1s[0]);
                                dicStr = dicStr.Replace("value", str1s[1]);
                                dicStr = dicStr.Replace("dicName", dataName.Split(':')[0]);

                                if (!cshapClassStr.Contains(dicStr))
                                    cshapClassStr += dicStr;
                                break;
                            }
                        default:
                            {
                                if (dataType.ToLower() == "vector3")
                                {
                                    string vecStr = @"
        public Vector3 name{ get; protected set; }
";
                                    vecStr = vecStr.Replace("name", dataName);
                                    cshapClassStr += vecStr;

                                    parseByBytes += $@"
            {dataName} = new Vector3(rd.ReadSingle(),rd.ReadSingle(),rd.ReadSingle());";
                                }
                                else if (dataType.ToLower() == "vector2")
                                {
                                    string vecStr = @"
        public Vector2 name{ get; protected set; }
";
                                    vecStr = vecStr.Replace("name", dataName);
                                    cshapClassStr += vecStr;

                                    parseByBytes += $@"
            {dataName} = new Vector2(rd.ReadSingle(),rd.ReadSingle());";
                                }
                                else if (dataType.ToLower() == "color")
                                {
                                    string colorStr = @"
        public Color name{ get; protected set; }
";
                                    colorStr = colorStr.Replace("name", dataName);
                                    cshapClassStr += colorStr;

                                    parseByBytes += $@"
            {dataName} = new Color(rd.ReadSingle(),rd.ReadSingle(),rd.ReadSingle(),rd.ReadSingle());";
                                }
                                else if (dataType.ToLower() == "int" ||
                                         dataType.ToLower() == "float" ||
                                         dataType.ToLower() == "string" ||
                                         dataType.ToLower() == "bool")
                                {
                                    string tempPropertyStr = propertyStr;
                                    tempPropertyStr = tempPropertyStr.Replace("dataType", dataType);
                                    tempPropertyStr = tempPropertyStr.Replace("dataName", dataName);
                                    cshapClassStr += tempPropertyStr;

                                    if (dataType == "int")
                                        parseByBytes += $@"
            {dataName} = rd.ReadInt32();";
                                    else if (dataType == "float")
                                        parseByBytes += $@"
            {dataName} = rd.ReadSingle();";
                                    else if (dataType == "string")
                                        parseByBytes += $@"
            {dataName} = rd.ReadString();";
                                    else if (dataType == "bool")
                                        parseByBytes += $@"
            {dataName} = rd.ReadBoolean();";
                                }
                                else if (dataType.ToLower() == "enum")
                                {
                                    //enum列：属性类型为 Enum{列名}（定义在 EnumType.cs，由 Enum.xlsx 生成）
                                    string tempPropertyStr = propertyStr;
                                    tempPropertyStr = tempPropertyStr.Replace("dataType", "Enum" + dataName);
                                    tempPropertyStr = tempPropertyStr.Replace("dataName", dataName);
                                    cshapClassStr += tempPropertyStr;

                                    parseByBytes += $@"
            {dataName} = (Enum{dataName})rd.ReadInt16();";
                                }
                                else if (dataType != "")
                                {
                                    Logger.Error($@"====Error====
Path:{path}
Sheet:{className},Columns:{columns},Rows:{2}
ErrorType:{dataType}
");
                                }

                                break;
                            }
                    }
                }

                parseBytesStr = parseBytesStr.Replace("value", parseByBytes);
                cshapClassStr += parseBytesStr;

                FileTool.WriteString($"{classPath}D{className}.cs", cshapClassStr);
                #endregion

                #region 生成数据文件
                CreateData(curSheet, rows, columns, className);

                ExportedSheetCount++;
                Logger.Log($"{className}.cs Created");
                #endregion
        }

        //自定义c#类
        private void InitClassStr()
        {
            classStr = @"using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DclassName: DataItem
    {
";
            propertyStr = @"
        public dataType dataName{ get;protected set; }
";

            parseBytes = @"
        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); value        
        }
    }
}   
";
        }

        //首字母大写
        private string UpperFirstLetter(string value)
        {
            //开头是一个字母或者空格后的第一个字母
            return Regex.Replace(value, @"\b(\w)|\s(\w)", match => match.Value.ToUpper());
        }

        public static List<DataTable> ExcelToDataTable(string fileName, List<ExportError>? seriousErrors = null)
        {
            IWorkbook workbook = null;
            ISheet sheet = null;

            List<DataTable> dataList = new List<DataTable>();
            int startRow = 0;

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fileName.IndexOf(".xlsx") > 0) // 2007版本
                    workbook = new XSSFWorkbook(fs);
                else if (fileName.IndexOf(".xls") > 0) // 2003版本
                    workbook = new HSSFWorkbook(fs);
            }

            if (workbook == null)
            {
                seriousErrors?.Add(new ExportError(fileName, "", 0, 0, "无法识别的文件格式，需要 .xls 或 .xlsx"));
                Logger.Error($"ExcelToDataTable: 无法识别的文件格式: {fileName}");
                return null;
            }

            try
            {
                for (int i = 0; i < workbook.NumberOfSheets; ++i)
                {
                    sheet = workbook.GetSheetAt(i);
                    if (sheet.SheetName.ToLower().Contains("sheet") && workbook.NumberOfSheets != 1)
                        continue;

                    DataTable data = new DataTable();
                    data.TableName = sheet.SheetName;

                    //行序固定：0=备注(可整行空白) 1=name 2=类型 3+=数据
                    IRow firstRow = sheet.GetRow(1); //name行
                    if (firstRow == null)
                    {
                        seriousErrors?.Add(new ExportError(fileName, sheet.SheetName, 0, 0, "缺少name行(第2行)，已跳过该表"));
                        Logger.Error($"ExcelToDataTable: sheet '{sheet.SheetName}' 缺少备注行或name行(前两行)，跳过");
                        continue;
                    }
                    int cellCount = firstRow.LastCellNum; //一行最后一个cell的编号 即总的列数

                    //记录每个DataTable行对应的Excel真实行号(从1开始)，供校验定位
                    var excelRowMap = new List<int>();
                    data.ExtendedProperties["ExcelRowMap"] = excelRowMap;

                    //按位置建列；name为空自动补名，重名自动去重，保证列序与Excel一致
                    for (int j = 0; j < cellCount; ++j)
                    {
                        string colName = firstRow.GetCell(j)?.ToString()?.Trim() ?? "";
                        if (colName.Length == 0)
                            colName = $"col{j}";
                        if (data.Columns.Contains(colName))
                            colName = $"{colName}_{j}";
                        data.Columns.Add(colName);
                    }

                    //行序固定：0=备注 1=name 2=类型 3+=数据；缺备注行时自动补空
                    void CopyRow(int sheetRowIdx)
                    {
                        excelRowMap.Add(sheetRowIdx + 1);
                        DataRow dataRow = data.NewRow();
                        IRow row = sheet.GetRow(sheetRowIdx);
                        if (row != null)
                        {
                            for (int k = 0; k < cellCount; ++k)
                            {
                                if (row.GetCell(k) != null) //没有数据的单元格默认是null
                                {
                                    if (row.GetCell(k).CellType == CellType.Formula)
                                        row.GetCell(k).SetCellType(CellType.String);
                                    dataRow[k] = row.GetCell(k).ToString();
                                }
                            }
                        }
                        data.Rows.Add(dataRow);
                    }

                    CopyRow(0); //备注（可为空）
                    CopyRow(1); //name
                    CopyRow(2); //类型

                    //最后一列的标号
                    int rowCount = sheet.LastRowNum;
                    for (int j = 3; j <= rowCount; ++j)
                    {
                        if (sheet.GetRow(j) == null)
                            continue; //没有数据的行默认是null
                        CopyRow(j);
                    }

                    dataList.Add(data);
                }
                return dataList;
            }
            catch (Exception ex)
            {
                seriousErrors?.Add(new ExportError(fileName, "", 0, 0, $"文件解析异常: {ex.Message}"));
                Console.WriteLine("Exception:{0}, datalistCount{1}", ex.Message, dataList.Count);
                return null;
            }
        }

        private void CreateData(DataTable curSheet, int rows, int columns, string className)
        {
            foreach (var enumType in Config.I.exportList)
            {
                if (enumType == Config.ExportType.Json)
                    CreateJson(curSheet, rows, columns, className);

                else if (enumType == Config.ExportType.Bytes)
                    CreateBytes(curSheet, rows, columns, className);
            }
        }

        public List<SheetInfo> GetSheetInfos(string filePath)
        {
            var result = new List<SheetInfo>();

            IWorkbook workbook;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (filePath.IndexOf(".xlsx") > 0)
                    workbook = new XSSFWorkbook(fs);
                else if (filePath.IndexOf(".xls") > 0)
                    workbook = new HSSFWorkbook(fs);
                else
                    return result;
            }

            int totalSheets = workbook.NumberOfSheets;

            for (int i = 0; i < totalSheets; i++)
            {
                ISheet sheet = workbook.GetSheetAt(i);
                string sheetName = sheet.SheetName;

                if (sheetName.ToLower().Contains("sheet") && totalSheets != 1)
                    continue;

                var info = new SheetInfo
                {
                    SheetName = sheetName,
                    ClassName = sheetName,
                    SheetIndex = result.Count
                };

                info.RowCount = sheet.LastRowNum + 1;
                IRow row1 = sheet.GetRow(1);
                info.ColumnCount = (row1 != null) ? row1.LastCellNum : 0;
                info.ModifyTime = File.GetLastWriteTime(filePath);

                result.Add(info);
            }

            return result;
        }

        public List<string> ValidateFile(string filePath)
        {
            var issues = new List<string>();

            LoadEnumData();

            List<DataTable> result = ExcelToDataTable(filePath);

            if (result == null || result.Count == 0)
            {
                issues.Add($"无法读取文件或文件为空: {filePath}");
                return issues;
            }

            for (int i = 0; i < result.Count; i++)
            {
                string sheetName = result[i].TableName;
                var sheetIssues = ValidateSheet(result[i], sheetName);
                foreach (var issue in sheetIssues)
                {
                    issues.Add($"[{sheetName}] {issue}");
                }
            }

            return issues;
        }

        public List<string> ValidateSheet(DataTable sheet, string sheetName)
        {
            var issues = new List<string>();

            if (sheet.Rows.Count < 3)
            {
                issues.Add($"行数不足: {sheet.Rows.Count} (需要至少4行: 备注+name+类型+数据)");
                return issues;
            }

            //类型行（第3行）整体为空 → 很可能缺了备注行
            int colCount = sheet.Columns.Count;
            bool hasAnyType = false;
            for (int j = 0; j < colCount; j++)
            {
                if (!string.IsNullOrEmpty(sheet.Rows[2][j].ToString().Trim()))
                { hasAnyType = true; break; }
            }
            if (!hasAnyType)
                issues.Add("第3行类型行全为空：请检查是否缺少备注行(备注行必须保留，可整行留空)");

            if (sheet.Rows.Count < 4 && hasAnyType)
                issues.Add($"缺少数据行: 共{sheet.Rows.Count}行 (需要至少4行: 备注+name+类型+数据)");

            for (int j = 0; j < colCount; j++)
            {
                string type = sheet.Rows[2][j].ToString().Trim();
                if (string.IsNullOrEmpty(type))
                    continue;

                if (type.StartsWith("#"))
                    continue;

                string typeLower = type.ToLower();
                string name = sheet.Rows[1][j].ToString();
                if (string.IsNullOrEmpty(name))
                {
                    issues.Add($"第{j + 1}列: 第2行name为空，但第3行有类型 '{type}'");
                    continue;
                }
                ValidateType(typeLower, name, j, issues);
            }

            return issues;
        }

        //类型格式检查：合法返回null，否则返回错误原因(供"检查"与导出严格校验共用)
        private static string? GetTypeFormatError(string typeLower)
        {
            if (typeLower == "int" || typeLower == "float" || typeLower == "bool" || typeLower == "string")
                return null;

            if (typeLower == "enum")
                return null;

            if (typeLower == "int[]" || typeLower == "float[]" || typeLower == "bool[]" || typeLower == "string[]")
                return null;

            if (typeLower == "vector2" || typeLower == "vector3" || typeLower == "color")
                return null;

            if (typeLower == "vector2[]" || typeLower == "vector3[]" || typeLower == "color[]")
                return $"不支持的数组类型 '{typeLower}'";

            if (typeLower.StartsWith("dic"))
            {
                if (!typeLower.StartsWith("dic<") || !typeLower.EndsWith(">"))
                    return $"dic类型格式错误，应为 'dic<keyType,valueType>'，实际为 '{typeLower}'";

                string inner = typeLower.Substring(4, typeLower.Length - 5);
                string[] parts = inner.Split(',');
                if (parts.Length != 2)
                    return $"dic类型格式错误，应为 'dic<keyType,valueType>'，实际为 '{typeLower}'";

                string keyType = parts[0].Trim();
                string valueType = parts[1].Trim();

                var errs = new List<string>();
                if (keyType != "int" && keyType != "float" && keyType != "bool" && keyType != "string")
                    errs.Add($"dic键类型 '{keyType}' 不是合法类型 (int/float/bool/string)");
                if (valueType != "int" && valueType != "float" && valueType != "bool" && valueType != "string")
                    errs.Add($"dic值类型 '{valueType}' 不是合法类型 (int/float/bool/string)");
                if (errs.Count > 0)
                    return string.Join("；", errs);
                return null;
            }

            return $"未知类型 '{typeLower}'";
        }

        private void ValidateType(string typeLower, string name, int colIndex, List<string> issues)
        {
            string? typeErr = GetTypeFormatError(typeLower);
            if (typeErr != null)
                issues.Add($"第{colIndex + 1}列: {typeErr}");
        }

        //JSON导出会把字符串值原样写出，对数据要求更严格；仅导出Bytes时部分检查可放宽
        private static bool ExportingJson()
        {
            return Config.I.exportList != null && Config.I.exportList.Contains(Config.ExportType.Json);
        }

        //导出前的严格校验：只收集会导致导出中断或生成损坏数据的严重错误；返回非空则该表整体跳过
        private List<ExportError> ValidateSheetStrict(DataTable sheet, string sheetName)
        {
            var errors = new List<ExportError>();
            var rowMap = sheet.ExtendedProperties["ExcelRowMap"] as List<int>;
            int ExcelRow(int i) => (rowMap != null && i >= 0 && i < rowMap.Count) ? rowMap[i] : i + 1;

            int colCount = sheet.Columns.Count;
            bool jsonExporting = ExportingJson();
            var activeCols = new List<(int colIdx, string typeLower, string? dicValueType, string colName)>();
            var seenProps = new HashSet<string>();

            for (int j = 0; j < colCount; j++)
            {
                //与导出一致：备注行以#开头禁用该列
                if ((sheet.Rows[0][j].ToString() ?? "").StartsWith("#"))
                    continue;

                string typeRaw = (sheet.Rows[2][j].ToString() ?? "").Trim();
                if (string.IsNullOrEmpty(typeRaw))
                    continue; //类型为空 → 该列不导出

                string name = sheet.Rows[1][j].ToString() ?? "";
                int col = j + 1;

                if (string.IsNullOrEmpty(name))
                {
                    errors.Add(new ExportError("", sheetName, ExcelRow(1), col, $"第2行name为空，但第3行有类型 '{typeRaw}'"));
                    continue;
                }
                if (name.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
                {
                    errors.Add(new ExportError("", sheetName, ExcelRow(1), col, $"name不能包含空格/换行: \"{name}\""));
                    continue;
                }

                string typeLower = typeRaw.ToLower();
                string? typeErr = GetTypeFormatError(typeLower);
                if (typeErr != null)
                {
                    errors.Add(new ExportError("", sheetName, ExcelRow(2), col, typeErr));
                    continue;
                }

                string propName;
                string? dicValueType = null;
                if (typeLower.StartsWith("dic"))
                {
                    //dic列name必须是 变量名:key，否则JSON导出取key时会崩溃
                    string[] nameParts = name.Split(':');
                    if (jsonExporting &&
                        (nameParts.Length < 2 || nameParts[0].Length == 0 || nameParts[1].Length == 0))
                    {
                        errors.Add(new ExportError("", sheetName, ExcelRow(1), col, $"dic类型的name格式应为 '变量名:key'，实际为 \"{name}\""));
                        continue;
                    }
                    propName = UpperFirstLetter(nameParts[0]);
                    if (jsonExporting)
                        dicValueType = typeLower.Substring(4, typeLower.Length - 5).Split(',')[1].Trim();
                }
                else
                {
                    propName = UpperFirstLetter(name);
                }

                //生成的属性名必须是合法C#标识符
                if (!Regex.IsMatch(propName, @"^[_\p{L}][_\p{L}\p{N}]*$"))
                {
                    errors.Add(new ExportError("", sheetName, ExcelRow(1), col, $"name不是合法的C#属性名: \"{propName}\""));
                    continue;
                }
                if (!seenProps.Add(propName))
                {
                    errors.Add(new ExportError("", sheetName, ExcelRow(1), col, $"属性名重复: {propName}"));
                    continue;
                }

                activeCols.Add((j, typeLower, dicValueType, name));
            }

            if (activeCols.Count == 0)
            {
                errors.Add(new ExportError("", sheetName, ExcelRow(2), 0, "没有可导出的数据列：请检查是否缺少备注行(备注行必须保留，可整行留空)，或第3行类型全为空"));
                return errors;
            }

            if (sheet.Rows.Count < 4)
            {
                errors.Add(new ExportError("", sheetName, ExcelRow(sheet.Rows.Count - 1), 0, $"缺少数据行: 共{sheet.Rows.Count}行 (需要至少4行: 备注+name+类型+数据)"));
                return errors;
            }

            //逐行校验数据值(与导出相同的跳过规则：首列以#开头)
            for (int i = 3; i < sheet.Rows.Count; ++i)
            {
                if ((sheet.Rows[i][0].ToString() ?? "").StartsWith("#"))
                    continue;

                foreach (var (colIdx, typeLower, dicValueType, colName) in activeCols)
                {
                    //Bytes导出会整体跳过dic列，无需校验其值
                    if (dicValueType != null && !jsonExporting)
                        continue;

                    string value = sheet.Rows[i][colIdx].ToString() ?? "";
                    string checkType = dicValueType ?? typeLower;
                    string? reason = GetDataValueError(checkType, value, !jsonExporting, colName);
                    if (reason != null)
                        errors.Add(new ExportError("", sheetName, ExcelRow(i), colIdx + 1, reason));
                }
            }

            return errors;
        }

        //校验单个数据值能否按指定类型被导出逻辑解析；返回null表示通过
        private string? GetDataValueError(string typeLower, string value, bool allowBoolNumeric, string colName)
        {
            if (string.IsNullOrEmpty(value))
            {
                //vector/color空值会在float.Parse("")处中断导出，其余类型空值有默认
                if (typeLower == "vector2" || typeLower == "vector3" || typeLower == "color")
                    return $"{typeLower}值不能为空，格式应为逗号分隔的数字(如 x,y,z)";
                return null;
            }

            switch (typeLower)
            {
                case "int":
                    return int.TryParse(value, out _) ? null : $"int值无法解析: \"{value}\"";
                case "float":
                    return float.TryParse(value, out _) ? null : $"float值无法解析: \"{value}\"";
                case "bool":
                {
                    string v = value.ToLower();
                    bool ok = v == "true" || v == "false" || (allowBoolNumeric && (value == "1" || value == "0"));
                    return ok ? null : $"bool值只能为 true/false: \"{value}\"";
                }
                case "enum":
                {
                    if (string.IsNullOrEmpty(value))
                        return $"enum值不能为空（应填 Enum.xlsx 中 {colName} 的成员名）";
                    if (enumTypesDic != null && enumTypesDic.TryGetValue(colName.ToLower(), out var members))
                        return members.ContainsKey(value.ToLower())
                            ? null
                            : $"enum '{colName}' 未定义成员 '{value}'（检查 Enum.xlsx）";
                    return $"未找到enum定义 '{colName}'（检查 Enum.xlsx 是否包含该列）";
                }
                case "vector2":
                    return CheckFloatTuple(value, 2, "vector2");
                case "vector3":
                    return CheckFloatTuple(value, 3, "vector3");
                case "color":
                    return CheckFloatTuple(value, 4, "color");
                default:
                    if (typeLower.EndsWith("[]"))
                    {
                        string elemType = typeLower.Substring(0, typeLower.Length - 2);
                        if (elemType == "string")
                            return null;

                        string[] arr = value.Split(',');
                        for (int k = 0; k < arr.Length; k++)
                        {
                            string elem = arr[k];
                            if (string.IsNullOrEmpty(elem))
                                continue; //空元素按默认0/false导出

                            if (elemType == "int" && !int.TryParse(elem, out _))
                                return $"{typeLower}第{k + 1}个元素无法解析为int: \"{elem}\"";
                            if (elemType == "float" && !float.TryParse(elem, out _))
                                return $"{typeLower}第{k + 1}个元素无法解析为float: \"{elem}\"";
                            if (elemType == "bool")
                            {
                                string v = elem.ToLower();
                                bool ok = v == "true" || v == "false" || (allowBoolNumeric && (elem == "1" || elem == "0"));
                                if (!ok)
                                    return $"{typeLower}第{k + 1}个元素只能为 true/false: \"{elem}\"";
                            }
                        }
                    }
                    return null;
            }
        }

        private static string? CheckFloatTuple(string value, int count, string typeName)
        {
            string[] arr = value.Split(',');
            if (arr.Length < count)
                return $"{typeName}值格式错误，需要{count}个逗号分隔的数字，实际为 \"{value}\"";
            for (int k = 0; k < count; k++)
            {
                if (!float.TryParse(arr[k], out _))
                    return $"{typeName}第{k + 1}个数字无法解析: \"{arr[k]}\"";
            }
            return null;
        }

        private string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            var sb = new StringBuilder();
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.Append($"\\u{(int)c:X4}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private string CreateJson(DataTable curSheet, int rows, int columns, string className)
        {
            if (rows <= 3)
                return null;

            var sb = new StringBuilder();
            sb.Append("[");

            int rowNum = 0;
            for (int i = 3; i < rows; ++i)
            {
                if (curSheet.Rows[i][0].ToString().StartsWith("#"))
                    continue;

                if (rowNum > 0)
                    sb.Append(",");
                sb.Append("{");

                bool isFirst = true;
                string activeDicName = null;
                string activeDicValueType = null;

                for (int j = 0; j < columns; ++j)
                {
                    //与C#类生成/严格校验一致：备注行以#开头的列不导出（含 #类名/#name/#type 标记列）
                    if ((curSheet.Rows[0][j].ToString() ?? "").StartsWith("#"))
                        continue;

                    string typeRaw = curSheet.Rows[2][j].ToString();
                    if (string.IsNullOrEmpty(typeRaw))
                        continue;

                    string type = typeRaw.ToLower();
                    string name = curSheet.Rows[1][j].ToString();
                    string value = curSheet.Rows[i][j].ToString();

                    if (type.StartsWith("dic"))
                    {
                        string dicTypes = typeRaw.Split('<')[1].Split('>')[0];
                        string[] dicTypeArr = dicTypes.Split(',');
                        string[] nameParts = name.Split(':');
                        string dicVarName = UpperFirstLetter(nameParts[0]);
                        string dicKey = nameParts[1];

                        if (activeDicName != dicVarName)
                        {
                            if (activeDicName != null)
                                sb.Append("},");

                            if (isFirst)
                                isFirst = false;
                            else
                                sb.Append(",");

                            sb.Append($"\"{dicVarName}\":{{");
                            activeDicName = dicVarName;
                            activeDicValueType = dicTypeArr[1];
                        }
                        else
                        {
                            sb.Append(",");
                        }

                        if (dicTypeArr[0] == "string")
                            sb.Append($"\"{UpperFirstLetter(dicKey)}\":");
                        else
                            sb.Append($"{UpperFirstLetter(dicKey)}:");

                        if (activeDicValueType == "string")
                            sb.Append($"\"{EscapeJsonString(value)}\"");
                        else if (activeDicValueType == "int")
                            sb.Append($"{(string.IsNullOrEmpty(value) ? 0 : int.Parse(value))}");
                        else if (activeDicValueType == "float")
                            sb.Append($"{(string.IsNullOrEmpty(value) ? 0 : float.Parse(value))}");
                        else if (activeDicValueType == "bool")
                            sb.Append($"{(string.IsNullOrEmpty(value) ? "false" : value.ToLower())}");
                    }
                    else
                    {
                        if (activeDicName != null)
                        {
                            sb.Append("}");
                            activeDicName = null;
                        }

                        if (isFirst)
                            isFirst = false;
                        else
                            sb.Append(",");

                        string pascalName = UpperFirstLetter(name);

                        switch (type)
                        {
                            case "int":
                                sb.Append($"\"{pascalName}\":{(string.IsNullOrEmpty(value) ? 0 : int.Parse(value))}");
                                break;
                            case "float":
                                sb.Append($"\"{pascalName}\":{(string.IsNullOrEmpty(value) ? 0 : float.Parse(value))}");
                                break;
                            case "bool":
                                sb.Append($"\"{pascalName}\":{(string.IsNullOrEmpty(value) ? "false" : value.ToLower())}");
                                break;
                            case "string":
                                sb.Append($"\"{pascalName}\":\"{EscapeJsonString(value)}\"");
                                break;
                            case "enum":
                                sb.Append($"\"{pascalName}\":{GetEnumValue(name, value)}");
                                break;
                            case "string[]":
                                {
                                    string[] arr = value.Split(',');
                                    sb.Append($"\"{pascalName}\":[");
                                    for (int k = 0; k < arr.Length; k++)
                                    {
                                        if (k > 0) sb.Append(",");
                                        sb.Append($"\"{EscapeJsonString(arr[k])}\"");
                                    }
                                    sb.Append("]");
                                }
                                break;
                            case "int[]":
                                {
                                    string[] arr = value.Split(',');
                                    sb.Append($"\"{pascalName}\":[");
                                    for (int k = 0; k < arr.Length; k++)
                                    {
                                        if (k > 0) sb.Append(",");
                                        sb.Append(string.IsNullOrEmpty(arr[k]) ? "0" : arr[k]);
                                    }
                                    sb.Append("]");
                                }
                                break;
                            case "float[]":
                                {
                                    string[] arr = value.Split(',');
                                    sb.Append($"\"{pascalName}\":[");
                                    for (int k = 0; k < arr.Length; k++)
                                    {
                                        if (k > 0) sb.Append(",");
                                        sb.Append(string.IsNullOrEmpty(arr[k]) ? "0" : arr[k]);
                                    }
                                    sb.Append("]");
                                }
                                break;
                            case "bool[]":
                                {
                                    if (string.IsNullOrEmpty(value))
                                        sb.Append($"\"{pascalName}\":[false]");
                                    else
                                        sb.Append($"\"{pascalName}\":[{value.ToLower()}]");
                                }
                                break;
                            case "vector3":
                                {
                                    string[] arr = value.Split(',');
                                    sb.Append($"\"{pascalName}\":{{\"x\":{float.Parse(arr[0])},\"y\":{float.Parse(arr[1])},\"z\":{float.Parse(arr[2])}}}");
                                }
                                break;
                            case "vector2":
                                {
                                    string[] arr = value.Split(',');
                                    sb.Append($"\"{pascalName}\":{{\"x\":{float.Parse(arr[0])},\"y\":{float.Parse(arr[1])}}}");
                                }
                                break;
                            case "color":
                                {
                                    string[] arr = value.Split(',');
                                    sb.Append($"\"{pascalName}\":{{\"r\":{float.Parse(arr[0])},\"g\":{float.Parse(arr[1])},\"b\":{float.Parse(arr[2])},\"a\":{float.Parse(arr[3])}}}");
                                }
                                break;
                            case "vector3[]":
                            case "vector2[]":
                            case "color[]":
                                Logger.Warning($"Unsupported array type '{type}' in row {rowNum}, column {j}, skipping");
                                break;
                            default:
                                Logger.Error($"wrong type in row:{rowNum}--column:{j}, type:{type}");
                                break;
                        }
                    }
                }

                if (activeDicName != null)
                {
                    sb.Append("}");
                }

                sb.Append("}");
                rowNum++;
            }

            sb.Append("]");

            string jsonData = sb.ToString();
            FileTool.WriteString($"{Config.I.dataPath}/Json/D{className}.json", jsonData);
            return jsonData;
        }

        private void CreateBytes(DataTable curSheet, int rows, int columns, string className)
        {
            if (rows <= 3)
                return;

            using (MemoryStream ms = new MemoryStream())
            {
                int dataCount = -1;
                Dictionary<int, long> dataInfos = new Dictionary<int, long>();

                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    for (int i = 3; i < rows; ++i)
                    {
                        if (curSheet.Rows[i][0].ToString().StartsWith("#"))
                            continue;
                        dataCount++;
                        dataInfos.Add(dataCount, bw.BaseStream.Position);

                        long bwLen = bw.BaseStream.Length;
                        for (int j = 0; j < columns; ++j)
                        {
                            //与C#类生成/严格校验一致：备注行以#开头的列不导出（含 #类名/#name/#type 标记列）
                            if ((curSheet.Rows[0][j].ToString() ?? "").StartsWith("#"))
                                continue;

                            if (string.IsNullOrEmpty(curSheet.Rows[2][j].ToString()))
                                continue;

                            string type = curSheet.Rows[2][j].ToString().ToLower();
                            string value = curSheet.Rows[i][j].ToString();
                            switch (type)
                            {
                                case "int":
                                    if (string.IsNullOrEmpty(value))
                                        value = "0";
                                    bw.Write(Convert.ToInt32(value)); break;
                                case "float":
                                    if (string.IsNullOrEmpty(value))
                                        value = "0";
                                    bw.Write(Convert.ToSingle(value)); break;
                                case "string":
                                    bw.Write(value); break;
                                case "bool":
                                    if (string.IsNullOrEmpty(value))
                                        value = "false";
                                    bw.Write(value.ToLower() == "true" || value == "1"); break;
                                case "enum":
                                    bw.Write((short)GetEnumValue(curSheet.Rows[1][j].ToString(), value));
                                    break;
                                case "vector3":
                                    string[] tempArr = value.Split(',');
                                    bw.Write(Convert.ToSingle(tempArr[0]));
                                    bw.Write(Convert.ToSingle(tempArr[1]));
                                    bw.Write(Convert.ToSingle(tempArr[2]));
                                    break;
                                case "color":
                                    tempArr = value.Split(',');
                                    bw.Write(Convert.ToSingle(tempArr[0]));
                                    bw.Write(Convert.ToSingle(tempArr[1]));
                                    bw.Write(Convert.ToSingle(tempArr[2]));
                                    bw.Write(Convert.ToSingle(tempArr[3]));
                                    break;
                                case "vector2":
                                    tempArr = value.Split(',');
                                    bw.Write(Convert.ToSingle(tempArr[0]));
                                    bw.Write(Convert.ToSingle(tempArr[1]));
                                    break;
                                default:
                                    if (type.StartsWith("dic"))
                                        Logger.Warning($"dic type is not supported in bytes export (column {j}, row {i}), skipping. Use a linked table instead.");
                                    else if (type.EndsWith("[]"))
                                        Logger.Warning($"Unsupported array type '{type}' in bytes export (column {j}, row {i}), skipping.");
                                    break;
                            }

                            if (type.EndsWith("[]"))
                            {
                                string[] tempArr = value.Split(',');
                                Int16 count = (Int16)tempArr.Length;
                                bw.Write(count);
                                foreach (string str in tempArr)
                                {
                                    switch (type.Substring(0, type.Length - 2))
                                    {
                                        case "int":
                                            if (str != "")
                                                bw.Write(Convert.ToInt32(str));
                                            else
                                                bw.Write(0);
                                            break;
                                        case "float":
                                            if (str != "")
                                                bw.Write(Convert.ToSingle(str));
                                            else
                                                bw.Write(0);
                                            break;
                                        case "bool":
                                            if (str != "")
                                                bw.Write(str.ToLower() == "true" || str == "1");
                                            else
                                                bw.Write(false);
                                            break;
                                        case "string": bw.Write(str); break;
                                        default: Logger.Error("no type:" + type); break;
                                    }
                                }
                            }
                        }
                    }

                    long pos = bw.BaseStream.Position;
                    foreach (var kv in dataInfos)
                    {
                        bw.Write(kv.Key);
                        bw.Write(kv.Value);
                    }
                    bw.Write(bw.BaseStream.Position - pos);

                    FileTool.WriteBytes($"{Config.I.dataPath}/Bytes/D{className}.bytes", ms);
                }
            }
        }
    }
}
