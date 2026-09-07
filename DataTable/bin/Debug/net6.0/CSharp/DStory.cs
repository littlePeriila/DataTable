using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DStory: DataItem
    {

        public int Chapter{ get;protected set; }

        public string Text{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Chapter = rd.ReadInt32();
            Text = rd.ReadString();        
        }
    }
}   
