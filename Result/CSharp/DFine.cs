using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DFine: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public int Hp{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Hp = rd.ReadInt32();        
        }
    }
}   
