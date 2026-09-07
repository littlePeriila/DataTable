using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DMonster: DataItem
    {

        public string Name{ get;protected set; }

        public string Icon{ get;protected set; }

        public int Race{ get;protected set; }

        public int Rank{ get;protected set; }

        public int LevelType{ get;protected set; }

        public List<int> HP{ get; protected set; }

        public List<int> Attcak{ get; protected set; }

        public List<int> Defence{ get; protected set; }

        public List<int> Speed{ get; protected set; }

        public string Description{ get;protected set; }

        public int UpgradeID{ get;protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Icon = rd.ReadString();
            Race = rd.ReadInt32();
            Rank = rd.ReadInt32();
            LevelType = rd.ReadInt32();
            int count = rd.ReadInt16();
            HP = new List<int>();
            for(int i = 0; i < count; i++)
            {
                HP.Add(rd.ReadInt32());
            }
            
            count = rd.ReadInt16();
            Attcak = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Attcak.Add(rd.ReadInt32());
            }
            
            count = rd.ReadInt16();
            Defence = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Defence.Add(rd.ReadInt32());
            }
            
            count = rd.ReadInt16();
            Speed = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Speed.Add(rd.ReadInt32());
            }
            
            Description = rd.ReadString();
            UpgradeID = rd.ReadInt32();        
        }
    }
}   
