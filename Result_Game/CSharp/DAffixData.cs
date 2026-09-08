using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DAffixData: DataItem
    {

        public int Id{ get;protected set; }

        public int Affix{ get;protected set; }

        public float Main{ get;protected set; }

        public float MainMax{ get;protected set; }

        public float SubMin{ get;protected set; }

        public float SubMax{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Affix = rd.ReadInt32();
            Main = rd.ReadSingle();
            MainMax = rd.ReadSingle();
            SubMin = rd.ReadSingle();
            SubMax = rd.ReadSingle();        
        }
    }
}   
