using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DEquipLevelData: DataItem
    {

        public int Id{ get;protected set; }

        public int Level{ get;protected set; }

        public float HP{ get;protected set; }

        public float HPRate{ get;protected set; }

        public float DEFRate{ get;protected set; }

        public float ATK{ get;protected set; }

        public float ATKRate{ get;protected set; }

        public float Crit{ get;protected set; }

        public float CritDMG{ get;protected set; }

        public float Energy_RE{ get;protected set; }

        public float Recover{ get;protected set; }

        public float Hit{ get;protected set; }

        public float Miss{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Level = rd.ReadInt32();
            HP = rd.ReadSingle();
            HPRate = rd.ReadSingle();
            DEFRate = rd.ReadSingle();
            ATK = rd.ReadSingle();
            ATKRate = rd.ReadSingle();
            Crit = rd.ReadSingle();
            CritDMG = rd.ReadSingle();
            Energy_RE = rd.ReadSingle();
            Recover = rd.ReadSingle();
            Hit = rd.ReadSingle();
            Miss = rd.ReadSingle();        
        }
    }
}   
