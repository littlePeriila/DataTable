using System;
using System.Collections.Generic;
using System.IO;

namespace PerillaTable
{
    class FileTool
    {
        public readonly List<string> fileList = new List<string>();
        public void GetAllFiles(string path)
        {
            if (!Directory.Exists(path))
                return;

            string[] filePaths = Directory.GetFiles(path);

            foreach (string filePath in filePaths)
            {
                FileInfo fi = new FileInfo(filePath);
                if (!fileList.Contains(filePath))
                    fileList.Add(filePath);
            }


            string[] dirPaths = Directory.GetDirectories(path);

            if (dirPaths.Length > 0)
            {
                foreach (string dirPath in dirPaths)
                {
                    GetAllFiles(dirPath);
                }
            }
        }

        public static string ReadString(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            string str = "";
            if (!fileInfo.Exists)
            {
                Console.WriteLine($"path:{path} no exists");
                return str;
            }

            using (StreamReader reader = new StreamReader(path))
            {
                if (reader.Peek() != -1)//判断文件中是否有字符
                {
                    str = reader.ReadToEnd();
                }
                reader.Close();
            }

            return str;
        }

        public static void WriteString(string path, string data)
        {
            string folderPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                fileInfo.Create().Close();
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.Write(data);
                writer.Flush();
            }
        }

        public static void WriteBytes(string path, Stream ms)
        {
            string folderPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            ms.Position = 0;
            FileInfo f = new FileInfo(path);
            FileStream fs = f.Create();
            ms.CopyTo(fs);
            ms.Position = 0;
            fs.Close();
        }

        public static BinaryReader ReadBytes(string path)
        {
            FileStream fs = new FileInfo(path).OpenRead();
            BinaryReader reader = new BinaryReader(fs);
            return reader;
        }
    }
}
