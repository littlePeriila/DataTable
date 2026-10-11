using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DEquip: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public string Description{ get;protected set; }

        public string SpritePath{ get;protected set; }

        public int Equip{ get;protected set; }

        public int IconID{ get;protected set; }

        public int Rank{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Description = rd.ReadString();
            SpritePath = rd.ReadString();
            Equip = rd.ReadInt32();
            IconID = rd.ReadInt32();
            Rank = rd.ReadInt32();        
        }
    }
}   
