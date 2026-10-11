using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DUIForm: DataItem
    {

        public int Id{ get;protected set; }

        public string AssetName{ get;protected set; }

        public string UIGroupName{ get;protected set; }

        public bool AllowMultiInstance{ get;protected set; }

        public bool PauseCoveredUIForm{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            AssetName = rd.ReadString();
            UIGroupName = rd.ReadString();
            AllowMultiInstance = rd.ReadBoolean();
            PauseCoveredUIForm = rd.ReadBoolean();        
        }
    }
}   
