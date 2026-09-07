using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DMintPack: DataItem
    {

        public int MintTime{ get;protected set; }

        public float Rank1{ get;protected set; }

        public float Rank2{ get;protected set; }

        public float Rank3{ get;protected set; }

        public float Rank4{ get;protected set; }

        public float Rank5{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            MintTime = rd.ReadInt32();
            Rank1 = rd.ReadSingle();
            Rank2 = rd.ReadSingle();
            Rank3 = rd.ReadSingle();
            Rank4 = rd.ReadSingle();
            Rank5 = rd.ReadSingle();        
        }
    }
}   
