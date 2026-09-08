using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DEquipData: DataItem
    {

        public int Id{ get;protected set; }

        public int Equip{ get;protected set; }

        public List<int> Main{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Equip = rd.ReadInt32();
            int count = rd.ReadInt16();
            Main = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Main.Add(rd.ReadInt32());
            }
                    
        }
    }
}   
