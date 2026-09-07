using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DPool: DataItem
    {

        public int Id{ get;protected set; }

        public List<int> FishIDs{ get; protected set; }

        public int MinNum{ get;protected set; }

        public int MaxNum{ get;protected set; }

        public int Level{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            int count = rd.ReadInt16();
            FishIDs = new List<int>();
            for(int i = 0; i < count; i++)
            {
                FishIDs.Add(rd.ReadInt32());
            }
            
            MinNum = rd.ReadInt32();
            MaxNum = rd.ReadInt32();
            Level = rd.ReadInt32();        
        }
    }
}   
