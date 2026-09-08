using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DBuff: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public string Icon{ get;protected set; }

        public int Buff{ get;protected set; }

        public int BuffType{ get;protected set; }

        public string FxName{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Icon = rd.ReadString();
            Buff = rd.ReadInt32();
            BuffType = rd.ReadInt32();
            FxName = rd.ReadString();        
        }
    }
}   
