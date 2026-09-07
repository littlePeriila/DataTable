using System;
using System.Collections.Generic;
using System.IO;

namespace Database
{
    public class DataItem
    {
        public virtual void ParseByBytes(MemoryStream ms)
        { }
    }

    public class DataLoader
    {
        private static Dictionary<string, Dictionary<int, long>> dataTableInofs = new Dictionary<string, Dictionary<int, long>>();

        public static T GetData<T>(string path, int index) where T : DataItem
        {
            if (!dataTableInofs.ContainsKey(path))
            {
                byte[] data;
                using (FileStream fs = new FileInfo(path).OpenRead())
                {
                    fs.Seek(0, SeekOrigin.End);
                    int len = (int)fs.Position;
                    fs.Seek(0, SeekOrigin.Begin);
                    data = new byte[len];
                    fs.Read(data, 0, len);
                }

                using (MemoryStream ms1 = new MemoryStream(data))
                using (BinaryReader rd = new BinaryReader(ms1))
                {
                    rd.BaseStream.Seek(-8, SeekOrigin.End);
                    long dataInfoLen = rd.ReadInt64();
                    int count = (int)dataInfoLen / 12;
                    rd.BaseStream.Seek(-dataInfoLen - 8, SeekOrigin.End);

                    Dictionary<int, long> dataInfos = new Dictionary<int, long>();
                    for (int i = 0; i < count; ++i)
                    {
                        int key = rd.ReadInt32();
                        long value = rd.ReadInt64();
                        dataInfos.Add(key, value);
                    }

                    dataTableInofs.Add(path, dataInfos);
                }
            }

            if (dataTableInofs[path].Count < index)
            {
                Logger.Error($"NoData:{index}");
                return null;
            }

            T res = (T)System.Activator.CreateInstance(typeof(T));
            using (FileStream fs = new FileInfo(path).OpenRead())
            {
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, (int)fs.Length);

                using (MemoryStream ms1 = new MemoryStream(data))
                using (BinaryReader rd = new BinaryReader(ms1))
                {
                    rd.BaseStream.Seek(dataTableInofs[path][index], SeekOrigin.Begin);
                    res.ParseByBytes(ms1);
                    Logger.Log(res.ToString());
                }
            }

            return res;
        }
    }
}
