using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DUpPool: DataItem
    {

        public int Id{ get;protected set; }

        public int PoolGroup{ get;protected set; }

        public int Rank{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            PoolGroup = rd.ReadInt32();
            Rank = rd.ReadInt32();        
        }
    }
}   
