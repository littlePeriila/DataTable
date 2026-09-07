using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DSheet1: DataItem
    {

        public string AssetName{ get;protected set; }

        public int UIType{ get;protected set; }

        public int UILayer{ get;protected set; }

        public bool AllowMultiInstance{ get;protected set; }

        public bool PauseCoveredUIForm{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            AssetName = rd.ReadString();
            UIType = rd.ReadInt32();
            UILayer = rd.ReadInt32();
            AllowMultiInstance = rd.ReadBoolean();
            PauseCoveredUIForm = rd.ReadBoolean();        
        }
    }
}   
