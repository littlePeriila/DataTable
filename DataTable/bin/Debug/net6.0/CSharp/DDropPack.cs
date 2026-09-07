using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DDropPack: DataItem
    {

        public bool IsBind{ get;protected set; }

        public bool IsReplace{ get;protected set; }

        public List<string> DropMap{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            IsBind = rd.ReadBoolean();
            IsReplace = rd.ReadBoolean();
            int count = rd.ReadInt16();
            DropMap = new List<string>();
            for(int i = 0; i < count; i++)
            {
                DropMap.Add(rd.ReadString());
            }
                    
        }
    }
}   
