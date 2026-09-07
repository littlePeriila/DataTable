using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DDragon: DataItem
    {

        public string Icon{ get;protected set; }

        public int MaxExp{ get;protected set; }

        public int MaxLevel{ get;protected set; }

        public int EggRank{ get;protected set; }

        public int ChestID{ get;protected set; }

        public int ChestRewardInterval{ get;protected set; }

        public string ItemValue{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Icon = rd.ReadString();
            MaxExp = rd.ReadInt32();
            MaxLevel = rd.ReadInt32();
            EggRank = rd.ReadInt32();
            ChestID = rd.ReadInt32();
            ChestRewardInterval = rd.ReadInt32();
            ItemValue = rd.ReadString();        
        }
    }
}   
