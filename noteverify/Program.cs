using PerillaTable;

Environment.CurrentDirectory = @"C:\Project\DataTable\DataTable";
var tool = new ExcelTool();

foreach (var f in Directory.GetFiles(@"C:\Project\DataTable\data", "*.xlsx"))
{
    var fname = Path.GetFileName(f);
    if (fname.Contains("~$") || fname.EndsWith(".bak") || fname.StartsWith("NoteTest")) continue;
    var issues = tool.ValidateFile(f);
    Console.WriteLine($"{fname}: {(issues.Count == 0 ? "OK" : issues.Count + " issues")}");
    foreach (var i in issues) Console.WriteLine("  " + i);
}
