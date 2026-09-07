using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DUI: DataItem
    {

        public string UIName{ get;protected set; }

        public string UILayer{ get;protected set; }

        public bool AllowMultiInstance{ get;protected set; }

        public bool PauseCoveredUIForm{ get;protected set; }

        public override void ParseByString(string data)
        {
            string[] tempStrs = data.Split('-');
            int count = -1;
            Id = int.Parse(tempStrs[count++]);
            UIName = tempStrs[count++];
            UILayer = tempStrs[count++];
            AllowMultiInstance = int.Parse(tempStrs[count++]) != 0;
            PauseCoveredUIForm = int.Parse(tempStrs[count++]) != 0;
        }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            UIName = rd.ReadString();
            UILayer = rd.ReadString();
            AllowMultiInstance = rd.ReadBoolean();
            PauseCoveredUIForm = rd.ReadBoolean();        
        }
    }
}   
