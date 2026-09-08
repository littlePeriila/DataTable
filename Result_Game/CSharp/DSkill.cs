using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Database
{
    [Serializable]
    public class DSkill: DataItem
    {

        public int Id{ get;protected set; }

        public string Name{ get;protected set; }

        public int Icon{ get;protected set; }

        public string Desp{ get;protected set; }

        public int Element{ get;protected set; }

        public List<int> Attribute{ get; protected set; }

        public float DamageRate{ get;protected set; }

        public int Cost{ get;protected set; }

        public float CoolDown{ get;protected set; }

        public int AtkMaxRange{ get;protected set; }

        public int AtkMinRange{ get;protected set; }

        public string AnimaName{ get;protected set; }

        public int AtkType{ get;protected set; }

        public int FxType{ get;protected set; }

        public string FxName{ get;protected set; }

        public float FxEndTime{ get;protected set; }

        public float FxAtkTime{ get;protected set; }

        public string HitFxName{ get;protected set; }

        public float HitEndTime{ get;protected set; }

        public string CastFxName{ get;protected set; }

        public float CastEndTime{ get;protected set; }

        public int HitPos{ get;protected set; }

        public bool IsOneFx{ get;protected set; }

        public int AtkTimes{ get;protected set; }

        public float FireInterval{ get;protected set; }

        public bool IsRotate{ get;protected set; }

        public List<float> Offsets{ get; protected set; }

        public bool IsHarmSelf{ get;protected set; }

        public int AtkScale{ get;protected set; }

        public List<int> ScaleVals{ get; protected set; }

        public bool IsBuff{ get;protected set; }

        public int BuffId{ get;protected set; }

        public List<float> BuffVals{ get; protected set; }

        public override void ParseByBytes(MemoryStream ms)
        {
            BinaryReader rd = new BinaryReader(ms); 
            Id = rd.ReadInt32();
            Name = rd.ReadString();
            Icon = rd.ReadInt32();
            Desp = rd.ReadString();
            Element = rd.ReadInt32();
            int count = rd.ReadInt16();
            Attribute = new List<int>();
            for(int i = 0; i < count; i++)
            {
                Attribute.Add(rd.ReadInt32());
            }
            
            DamageRate = rd.ReadSingle();
            Cost = rd.ReadInt32();
            CoolDown = rd.ReadSingle();
            AtkMaxRange = rd.ReadInt32();
            AtkMinRange = rd.ReadInt32();
            AnimaName = rd.ReadString();
            AtkType = rd.ReadInt32();
            FxType = rd.ReadInt32();
            FxName = rd.ReadString();
            FxEndTime = rd.ReadSingle();
            FxAtkTime = rd.ReadSingle();
            HitFxName = rd.ReadString();
            HitEndTime = rd.ReadSingle();
            CastFxName = rd.ReadString();
            CastEndTime = rd.ReadSingle();
            HitPos = rd.ReadInt32();
            IsOneFx = rd.ReadBoolean();
            AtkTimes = rd.ReadInt32();
            FireInterval = rd.ReadSingle();
            IsRotate = rd.ReadBoolean();
            count = rd.ReadInt16();
            Offsets = new List<float>();
            for(int i = 0; i < count; i++)
            {
                Offsets.Add(rd.ReadSingle());
            }
            
            IsHarmSelf = rd.ReadBoolean();
            AtkScale = rd.ReadInt32();
            count = rd.ReadInt16();
            ScaleVals = new List<int>();
            for(int i = 0; i < count; i++)
            {
                ScaleVals.Add(rd.ReadInt32());
            }
            
            IsBuff = rd.ReadBoolean();
            BuffId = rd.ReadInt32();
            count = rd.ReadInt16();
            BuffVals = new List<float>();
            for(int i = 0; i < count; i++)
            {
                BuffVals.Add(rd.ReadSingle());
            }
                    
        }
    }
}   
