using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DUI: DataItem
    {

        public int Id{ get;protected set; }

        public string AssetName{ get;protected set; }

        public int UIType{ get;protected set; }

        public string UILayer{ get;protected set; }

        public bool AllowMultiInstance{ get;protected set; }

        public bool PauseCoveredUIForm{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            AssetName = rd.ReadString();
            UIType = rd.ReadInt32();
            UILayer = rd.ReadString();
            AllowMultiInstance = rd.ReadBoolean();
            PauseCoveredUIForm = rd.ReadBoolean();        
        }
    }
}   
