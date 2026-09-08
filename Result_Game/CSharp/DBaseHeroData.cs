using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DBaseHeroData: DataItem
    {

        public int Id{ get;protected set; }

        public int Weapon{ get;protected set; }

        public int Level{ get;protected set; }

        public float HP{ get;protected set; }

        public float Atk{ get;protected set; }

        public float DEF{ get;protected set; }

        public float Hit{ get;protected set; }

        public float Crit{ get;protected set; }

        public float CritDMG{ get;protected set; }

        public float Miss{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Weapon = rd.ReadInt32();
            Level = rd.ReadInt32();
            HP = rd.ReadSingle();
            Atk = rd.ReadSingle();
            DEF = rd.ReadSingle();
            Hit = rd.ReadSingle();
            Crit = rd.ReadSingle();
            CritDMG = rd.ReadSingle();
            Miss = rd.ReadSingle();        
        }
    }
}   
