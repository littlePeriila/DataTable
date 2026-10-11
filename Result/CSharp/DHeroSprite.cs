using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DHeroSprite: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public string Path{ get;protected set; }

        public int HeroSprite{ get;protected set; }

        public int IconID{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Path = rd.ReadString();
            HeroSprite = rd.ReadInt32();
            IconID = rd.ReadInt32();        
        }
    }
}   
