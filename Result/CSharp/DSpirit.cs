using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DSpirit: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public int GroupID{ get;protected set; }

        public int Element{ get;protected set; }

        public int Icon{ get;protected set; }

        public List<int> Attribute{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            GroupID = rd.ReadInt32();
            Element = rd.ReadInt32();
            Icon = rd.ReadInt32();
            int count = rd.ReadInt16();
            Attribute = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Attribute.Add(rd.ReadInt32());
            }
                    
        }
    }
}   
