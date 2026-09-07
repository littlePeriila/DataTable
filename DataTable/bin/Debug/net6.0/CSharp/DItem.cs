using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DItem: DataItem
    {

        public string Name{ get;protected set; }

        public string Icon{ get;protected set; }

        public int Rank{ get;protected set; }

        public EnumItemType ItemType{ get; protected set; }

        public EnumUseType UseType{ get; protected set; }

        public int CanStack{ get;protected set; }

        public string Description{ get;protected set; }

        public int ExpValue{ get;protected set; }

        public List<int> DropMap{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Icon = rd.ReadString();
            Rank = rd.ReadInt32();
            ItemType = (EnumItemType)rd.ReadInt16();
            UseType = (EnumUseType)rd.ReadInt16();
            CanStack = rd.ReadInt32();
            Description = rd.ReadString();
            ExpValue = rd.ReadInt32();
            int count = rd.ReadInt16();
            DropMap = new List<int>();
            for(int i = 0; i < count; i++)
            {
                DropMap.Add(rd.ReadInt32());
            }
                    
        }
    }
}   
