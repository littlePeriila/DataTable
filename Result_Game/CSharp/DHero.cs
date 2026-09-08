using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DHero: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public int Element{ get;protected set; }

        public int Weapon{ get;protected set; }

        public int Rank{ get;protected set; }

        public int MatrixID{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Element = rd.ReadInt32();
            Weapon = rd.ReadInt32();
            Rank = rd.ReadInt32();
            MatrixID = rd.ReadInt32();        
        }
    }
}   
