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

    class ExcelTool
    {
        public ExcelTool()
        {
            InitClassStr();
        }

        private string classStr;
        private string propertyStr;
        private string parseBytes;

        public void CreateDataTable(string path)
        {
            string classPath = Config.I.classPath;
            List<DataTable> result = ExcelToDataTable(path);
            if (result == null)
            {
                Logger.Log($"ExcelToDataTable:{path} NULL");
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
            string classPath = Config.I.classPath;
            List<DataTable> result = ExcelToDataTable(path);
            if (result == null)
            {
                Logger.Log($"ExcelToDataTable:{path} NULL");
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
            //类名即sheet名（sheet名包含"sheet"的表单已在读取时跳过）
            string className = curSheet.TableName;

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

        public static List<DataTable> ExcelToDataTable(string fileName)
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
                        Logger.Error($"ExcelToDataTable: sheet '{sheet.SheetName}' 缺少备注行或name行(前两行)，跳过");
                        continue;
                    }
                    int cellCount = firstRow.LastCellNum; //一行最后一个cell的编号 即总的列数

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

        private void ValidateType(string typeLower, string name, int colIndex, List<string> issues)
        {
            if (typeLower == "int" || typeLower == "float" || typeLower == "bool" || typeLower == "string")
                return;

            if (typeLower == "int[]" || typeLower == "float[]" || typeLower == "bool[]" || typeLower == "string[]")
                return;

            if (typeLower == "vector2" || typeLower == "vector3" || typeLower == "color")
                return;

            if (typeLower == "vector2[]" || typeLower == "vector3[]" || typeLower == "color[]")
            {
                issues.Add($"第{colIndex + 1}列: 不支持的数组类型 '{typeLower}'");
                return;
            }

            if (typeLower.StartsWith("dic"))
            {
                if (!typeLower.StartsWith("dic<") || !typeLower.EndsWith(">"))
                {
                    issues.Add($"第{colIndex + 1}列: dic类型格式错误，应为 'dic<keyType,valueType>'，实际为 '{typeLower}'");
                    return;
                }

                string inner = typeLower.Substring(4, typeLower.Length - 5);
                string[] parts = inner.Split(',');
                if (parts.Length != 2)
                {
                    issues.Add($"第{colIndex + 1}列: dic类型格式错误，应为 'dic<keyType,valueType>'，实际为 '{typeLower}'");
                    return;
                }

                string keyType = parts[0].Trim();
                string valueType = parts[1].Trim();

                if (keyType != "int" && keyType != "float" && keyType != "bool" && keyType != "string")
                {
                    issues.Add($"第{colIndex + 1}列: dic键类型 '{keyType}' 不是合法类型 (int/float/bool/string)");
                }
                if (valueType != "int" && valueType != "float" && valueType != "bool" && valueType != "string")
                {
                    issues.Add($"第{colIndex + 1}列: dic值类型 '{valueType}' 不是合法类型 (int/float/bool/string)");
                }
                return;
            }

            issues.Add($"第{colIndex + 1}列: 未知类型 '{typeLower}'");
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
