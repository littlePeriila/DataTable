using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DAdventure: DataItem
    {

        public string Name{ get;protected set; }

        public List<int> Hp{ get; protected set; }

        public List<int> Attack{ get; protected set; }

        public List<int> Defense{ get; protected set; }

        public List<int> Speed{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            int count = rd.ReadInt16();
            Hp = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Hp.Add(rd.ReadInt32());
            }
            
            count = rd.ReadInt16();
            Attack = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Attack.Add(rd.ReadInt32());
            }
            
            count = rd.ReadInt16();
            Defense = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Defense.Add(rd.ReadInt32());
            }
            
            count = rd.ReadInt16();
            Speed = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Speed.Add(rd.ReadInt32());
            }
                    
        }
    }
}   
