using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DSound: DataItem
    {

        public int Id{ get;protected set; }

        public string AssetName{ get;protected set; }

        public int Priority{ get;protected set; }

        public bool Loop{ get;protected set; }

        public float Volume{ get;protected set; }

        public float SpatialBlend{ get;protected set; }

        public float MaxDistance{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            AssetName = rd.ReadString();
            Priority = rd.ReadInt32();
            Loop = rd.ReadBoolean();
            Volume = rd.ReadSingle();
            SpatialBlend = rd.ReadSingle();
            MaxDistance = rd.ReadSingle();        
        }
    }
}   
