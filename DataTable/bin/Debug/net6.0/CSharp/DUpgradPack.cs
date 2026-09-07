using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DUpgradPack: DataItem
    {

        public int Rank{ get;protected set; }

        public float Hp{ get;protected set; }

        public float Atk{ get;protected set; }

        public float Def{ get;protected set; }

        public float Vel{ get;protected set; }

        public int Exp{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Rank = rd.ReadInt32();
            Hp = rd.ReadSingle();
            Atk = rd.ReadSingle();
            Def = rd.ReadSingle();
            Vel = rd.ReadSingle();
            Exp = rd.ReadInt32();        
        }
    }
}   
