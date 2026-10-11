using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DWeapon: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public bool IsBoom{ get;protected set; }

        public int Weapon{ get;protected set; }

        public int IconID{ get;protected set; }

        public int Rank{ get;protected set; }

        public int Element{ get;protected set; }

        public string Description{ get;protected set; }

        public int Range{ get;protected set; }

        public string SpriteName{ get;protected set; }

        public string Path{ get;protected set; }

        public string SpriteName2{ get;protected set; }

        public string BloomCol{ get;protected set; }

        public float Intensity{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            IsBoom = rd.ReadBoolean();
            Weapon = rd.ReadInt32();
            IconID = rd.ReadInt32();
            Rank = rd.ReadInt32();
            Element = rd.ReadInt32();
            Description = rd.ReadString();
            Range = rd.ReadInt32();
            SpriteName = rd.ReadString();
            Path = rd.ReadString();
            SpriteName2 = rd.ReadString();
            BloomCol = rd.ReadString();
            Intensity = rd.ReadSingle();        
        }
    }
}   
