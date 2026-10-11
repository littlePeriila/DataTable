using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DMatrix: DataItem
    {

        public int Id{ get;protected set; }

        public List<int> SpiritsType{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            int count = rd.ReadInt16();
            SpiritsType = new List<int>();
            for(int i = 0; i < count; i++)
            {
                SpiritsType.Add(rd.ReadInt32());
            }
                    
        }
    }
}   
