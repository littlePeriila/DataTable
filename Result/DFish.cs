using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DFish: DataItem
    {

        public int Id{ get;protected set; }

        public float ProbabilityBirth{ get;protected set; }

        public int Price{ get;protected set; }

        public float BodyType{ get;protected set; }

        public float Speed{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            ProbabilityBirth = rd.ReadSingle();
            Price = rd.ReadInt32();
            BodyType = rd.ReadSingle();
            Speed = rd.ReadSingle();        
        }
    }
}   
