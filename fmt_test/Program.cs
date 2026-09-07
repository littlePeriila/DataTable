using System;
using System.Data;
using System.IO;
using PerillaTable;

class FmtTest
{
    static int Main()
    {
        string root = "C:\\Project\\DataTable";
        Environment.CurrentDirectory = Path.Combine(root, "fmt_test");

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

        Console.WriteLine("== outputs ==");
        foreach (var f in Directory.GetFiles(Path.Combine(root, "Result", "CSharp"), "D*.cs"))
            Console.WriteLine("  CS: " + Path.GetFileName(f));
        foreach (var f in Directory.GetFiles(Path.Combine(root, "Result", "Data", "Json"), "*.json"))
            Console.WriteLine("  JSON: " + Path.GetFileName(f));

        string fishJson = File.ReadAllText(Path.Combine(root, "Result", "Data", "Json", "DFish.json"));
        Console.WriteLine("DFish.json first 200 chars: " + fishJson.Substring(0, Math.Min(200, fishJson.Length)));

        return 0;
    }
}
