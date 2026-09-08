using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using PerillaTable;

class FmtTest
{
    static int Main(string[] args)
    {
        string root = "C:\\Project\\DataTable";
        Environment.CurrentDirectory = Path.Combine(root, "fmt_test");

        // genbad: 生成含各类错误的测试Excel，用于验证导出错误提示
        if (args.Length > 0 && args[0] == "genbad")
        {
            GenBad(Path.Combine(root, "data", "_errtest.xlsx"));
            Console.WriteLine("created " + Path.Combine(root, "data", "_errtest.xlsx"));
            return 0;
        }

        // game: 用游戏项目的真实Excel验证导出（输出到 Result_Game，不写游戏目录）
        if (args.Length > 0 && args[0] == "game")
        {
            string gameData = "C:\\Project\\Unity\\TrailsOfMeta\\TrailsOfMeta\\DataTable\\data\\";
            string outDir = Path.Combine(root, "Result_Game");
            string outData = Path.Combine(outDir, "Data");
            string outClass = Path.Combine(outDir, "CSharp");
            Directory.CreateDirectory(Path.Combine(outData, "Json"));
            Directory.CreateDirectory(Path.Combine(outData, "Bytes"));
            Directory.CreateDirectory(outClass);

            Config.I.ApplyConfig(gameData, outData + "\\", outClass + "\\",
                new List<Config.ExportType> { Config.ExportType.Json, Config.ExportType.Bytes });

            var gameTool = new ExcelTool();
            foreach (var f in Directory.GetFiles(gameData, "*.xlsx"))
            {
                string fname = Path.GetFileName(f);
                if (fname.Contains("~$"))
                    continue;
                try { gameTool.CreateDataTable(f); }
                catch (Exception ex) { Console.WriteLine("EXPORT FAIL " + fname + ": " + ex.Message); }
            }

            Console.WriteLine($"== game export: exportedSheets={gameTool.ExportedSheetCount} errors={gameTool.ExportErrors.Count} ==");
            foreach (var e in gameTool.ExportErrors)
                Console.WriteLine($"  ERR [{(e.SheetName.Length == 0 ? "-" : e.SheetName)}] {e}");

            foreach (var f in Directory.GetFiles(Path.Combine(outData, "Json"), "*.json"))
                Console.WriteLine("  JSON: " + Path.GetFileName(f));
            foreach (var f in Directory.GetFiles(Path.Combine(outData, "Bytes"), "*.bytes"))
                Console.WriteLine("  BYTES: " + Path.GetFileName(f) + " " + new FileInfo(f).Length);

            string ds = Path.Combine(outData, "Json", "DSkill.json");
            if (File.Exists(ds))
            {
                string json = File.ReadAllText(ds);
                Console.WriteLine("DSkill.json first 300: " + json.Substring(0, Math.Min(300, json.Length)));
                Console.WriteLine("Element fields: " + System.Text.RegularExpressions.Regex.Matches(json, "\"Element\":").Count);
                Console.WriteLine("AtkType fields: " + System.Text.RegularExpressions.Regex.Matches(json, "\"AtkType\":").Count);
            }
            return 0;
        }

        // verify: 按游戏 DSkill.ParseByBytes 的字节序列解码对比两个 DSkill.bytes
        if (args.Length > 0 && args[0] == "verify")
        {
            string[] paths =
            {
                "C:\\Project\\Unity\\TrailsOfMeta\\TrailsOfMeta\\Assets\\GameMain\\DataTableSelf\\Bytes\\DSkill.bytes",
                "C:\\Project\\DataTable\\Result_Game\\Data\\Bytes\\DSkill.bytes"
            };

            foreach (var p in paths)
            {
                using var fs = File.OpenRead(p);
                using var br = new BinaryReader(fs);
                fs.Position = fs.Length - 8;
                long idxLen = br.ReadInt64();
                long dataEnd = fs.Length - 8 - idxLen;
                fs.Position = 0;

                int rows = 0;
                string first = "", last = "";
                while (fs.Position < dataEnd)
                {
                    int id = br.ReadInt32();
                    string name = br.ReadString();
                    int icon = br.ReadInt32();
                    string desp = br.ReadString();
                    int element = br.ReadInt32();
                    int n = br.ReadInt16();
                    for (int k = 0; k < n; k++) br.ReadInt32();
                    float damageRate = br.ReadSingle();
                    int cost = br.ReadInt32();
                    float coolDown = br.ReadSingle();
                    int atkMax = br.ReadInt32();
                    int atkMin = br.ReadInt32();
                    string anima = br.ReadString();
                    int atkType = br.ReadInt32();
                    int fxType = br.ReadInt32();
                    string fxName = br.ReadString();
                    br.ReadSingle(); br.ReadSingle();
                    br.ReadString(); br.ReadSingle();
                    br.ReadString(); br.ReadSingle();
                    int hitPos = br.ReadInt32();
                    br.ReadBoolean();
                    br.ReadInt32(); br.ReadSingle(); br.ReadBoolean();
                    n = br.ReadInt16();
                    for (int k = 0; k < n; k++) br.ReadSingle();
                    br.ReadBoolean();
                    int atkScale = br.ReadInt32();
                    n = br.ReadInt16();
                    for (int k = 0; k < n; k++) br.ReadInt32();
                    br.ReadBoolean();
                    br.ReadInt32();
                    n = br.ReadInt16();
                    for (int k = 0; k < n; k++) br.ReadSingle();

                    last = $"id={id} name={name} element={element} atkType={atkType} atkScale={atkScale}";
                    if (rows == 0)
                        first = last + $" desp={desp} anima={anima} fxName={fxName} hitPos={hitPos} dmg={damageRate} cost={cost}";
                    rows++;
                }
                Console.WriteLine($"{Path.GetFileName(p)}: size={fs.Length} idxLen={idxLen} dataEnd={dataEnd} rows={rows}");
                Console.WriteLine($"  first: {first}");
                Console.WriteLine($"  last : {last}");
            }
            return 0;
        }

        var tool = new ExcelTool();

        Console.WriteLine("== validate ==");
        foreach (var f in Directory.GetFiles(Path.Combine(root, "data"), "*.xlsx"))
        {
            string fname = Path.GetFileName(f);
            if (fname.Contains("~$") || fname.Equals("Enum.xlsx", StringComparison.OrdinalIgnoreCase)) continue;
            var issues = tool.ValidateFile(f);
            Console.WriteLine($"--- {fname}: {(issues.Count == 0 ? "OK" : issues.Count + " issues")}");
            foreach (var issue in issues) Console.WriteLine("    " + issue);
        }

        Console.WriteLine("== export ==");
        foreach (var f in Directory.GetFiles(Path.Combine(root, "data"), "*.xlsx"))
        {
            string fname = Path.GetFileName(f);
            if (fname.Contains("~$") || fname.Equals("Enum.xlsx", StringComparison.OrdinalIgnoreCase)) continue;
            try { tool.CreateDataTable(f); }
            catch (Exception ex) { Console.WriteLine("EXPORT FAIL " + fname + ": " + ex.Message); }
        }

        Console.WriteLine("== export errors ==");
        Console.WriteLine($"  exportedSheets: {tool.ExportedSheetCount}");
        foreach (var e in tool.ExportErrors)
            Console.WriteLine($"  {Path.GetFileName(e.FilePath)} [{(e.SheetName.Length == 0 ? "-" : e.SheetName)}] {e}");

        Console.WriteLine("== outputs ==");
        foreach (var f in Directory.GetFiles(Path.Combine(root, "Result", "CSharp"), "D*.cs"))
            Console.WriteLine("  CS: " + Path.GetFileName(f));
        foreach (var f in Directory.GetFiles(Path.Combine(root, "Result", "Data", "Json"), "*.json"))
            Console.WriteLine("  JSON: " + Path.GetFileName(f));

        string fishJson = File.ReadAllText(Path.Combine(root, "Result", "Data", "Json", "DFish.json"));
        Console.WriteLine("DFish.json first 200 chars: " + fishJson.Substring(0, Math.Min(200, fishJson.Length)));

        return 0;
    }

    static void GenBad(string path)
    {
        var wb = new NPOI.XSSF.UserModel.XSSFWorkbook();

        void AddRow(NPOI.SS.UserModel.ISheet s, int idx, params string[] vals)
        {
            var row = s.CreateRow(idx);
            for (int i = 0; i < vals.Length; i++) row.CreateCell(i).SetCellValue(vals[i]);
        }

        // Fine: all correct, should export
        var s = wb.CreateSheet("Fine");
        AddRow(s, 0, "comment");
        AddRow(s, 1, "id", "name", "hp");
        AddRow(s, 2, "int", "string", "int");
        AddRow(s, 3, "1", "Tom", "10");
        AddRow(s, 4, "2", "Jerry", "20");

        // BadInt: row4 col3 int value unparseable
        s = wb.CreateSheet("BadInt");
        AddRow(s, 0, "comment");
        AddRow(s, 1, "id", "name", "hp");
        AddRow(s, 2, "int", "string", "int");
        AddRow(s, 3, "1", "Tom", "abc");
        AddRow(s, 4, "2", "Jerry", "20");

        // BadType: row3 col2 unknown type
        s = wb.CreateSheet("BadType");
        AddRow(s, 0, "comment");
        AddRow(s, 1, "id", "count");
        AddRow(s, 2, "int", "lst");
        AddRow(s, 3, "1", "5");

        // BadName: row2 col2 name contains space
        s = wb.CreateSheet("BadName");
        AddRow(s, 0, "comment");
        AddRow(s, 1, "id", "player hp");
        AddRow(s, 2, "int", "int");
        AddRow(s, 3, "1", "2");

        // BadVals: row4 vector3 needs 3 numbers but has 2; bool value illegal
        s = wb.CreateSheet("BadVals");
        AddRow(s, 0, "comment");
        AddRow(s, 1, "id", "pos", "flag");
        AddRow(s, 2, "int", "vector3", "bool");
        AddRow(s, 3, "1", "1,2", "yes");

        // NoName: missing name row
        s = wb.CreateSheet("NoName");
        AddRow(s, 0, "comment");
        AddRow(s, 2, "int");

        using (var fs = File.Create(path))
            wb.Write(fs);
    }
}
