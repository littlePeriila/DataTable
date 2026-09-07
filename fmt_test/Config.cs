using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PerillaTable
{
    class Config
    {
        private static Config instance;
        public static Config I => instance ?? (instance = new Config());

        private Config()
        {
            LoadConfig();
        }

        public enum ExportType
        {
            Json,
            Protobuf,
            Xml,
            Bytes,
        }

        private string configPath;
        public string excelPath;
        public string dataPath;
        public string classPath;
        public List<ExportType> exportList = null;
        public bool isExportServer;

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            path = path.Replace('\\', '/');
            if (!path.EndsWith("/"))
                path += "/";
            return path;
        }

        private void LoadConfig()
        {
            exportList= new List<ExportType>(); 
            configPath = "./config.txt";
            string temp = FileTool.ReadString(configPath);
            string[] strs = temp.Split(';');

            for (int i = 0; i < strs.Length; ++i)
            {
                if (strs[i].StartsWith("#"))
                    continue;

                string[] tempStrs = strs[i].Split(new char[] { ':' }, 2);
                if (tempStrs.Length < 2)
                    continue;

                string name = Regex.Replace(tempStrs[0], "[^A-Za-z0-9]", "");
                string value = tempStrs[1].Trim();

                switch (name)
                {
                    case "excelPath": excelPath = NormalizePath(value); break;
                    case "dataPath": dataPath = NormalizePath(value); break;
                    case "classPath": classPath = NormalizePath(value); break;
                    case "exportType":
                        string[] types = value.Split('&');
                        foreach (string type in types)
                            exportList.Add((ExportType)Enum.Parse(typeof(ExportType), type));
                        break;
                    case "isExportServer": isExportServer = value.ToLower() != "false"; break;
                }
            }

            if (string.IsNullOrEmpty(excelPath))
                Logger.Error("Config: excelPath is not set in config.txt");
            if (string.IsNullOrEmpty(dataPath))
                Logger.Error("Config: dataPath is not set in config.txt");
            if (string.IsNullOrEmpty(classPath))
                Logger.Error("Config: classPath is not set in config.txt");
            if (exportList.Count == 0)
                Logger.Error("Config: exportType is not set in config.txt, no data will be exported");
        }

        public void SaveConfig()
        {
            configPath = "./config.txt";
            var sb = new StringBuilder();
            sb.AppendLine("#excel存放路径;");
            sb.AppendLine($"excelPath:{excelPath};");
            sb.AppendLine("#数据保存路径;");
            sb.AppendLine($"dataPath:{dataPath};");
            sb.AppendLine("#c#类保存路径;");
            sb.AppendLine($"classPath:{classPath};");
            sb.AppendLine("#输出类型 (多个用&连接，如 Json&Bytes);");
            string exportTypeStr = string.Join("&", exportList.Select(e => e.ToString()));
            sb.AppendLine($"exportType:{exportTypeStr};");
            sb.Append($"isExportServer:{isExportServer.ToString().ToLower()}");

            FileTool.WriteString(configPath, sb.ToString());
            Logger.Log("Config saved to config.txt");
        }

        public void ApplyConfig(string excelPath, string dataPath, string classPath, List<ExportType> exportTypes)
        {
            this.excelPath = NormalizePath(excelPath);
            this.dataPath = NormalizePath(dataPath);
            this.classPath = NormalizePath(classPath);
            this.exportList = exportTypes ?? new List<ExportType>();
        }
    }
}
