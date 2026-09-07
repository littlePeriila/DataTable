using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DHook: DataItem
    {

        public int Id{ get;protected set; }

        public string EnglishText{ get;protected set; }

        public int Price{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            EnglishText = rd.ReadString();
            Price = rd.ReadInt32();        
        }
    }
}   
