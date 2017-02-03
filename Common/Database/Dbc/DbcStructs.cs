﻿using System;
using System.Linq;

namespace Common.Database.Dbc
{

    #region AreaTable.dbc
    public class AreaTableReader : DbcReader<AreaTable> { }

    public class AreaTable : DbcRecordBase
    {
        public int AreaId;
        public int AreaMapId;
        public int AreaZone;
        public int AreaExploreFlag;
        public int AreaZoneType;
        public int AreaLevel;
        public string AreaName;

        public override int Read()
        {
            AreaId = GetInt32(0);
            AreaMapId = GetInt32(1); // May be needed in the future
            AreaZone = GetInt32(2);
            AreaExploreFlag = GetInt32(3);
            AreaZoneType = GetInt32(4);
            AreaLevel = GetInt32(10);
            AreaName = GetString(11);

            if (AreaLevel > 255)
                AreaLevel = 255;

            if (AreaLevel < 0)
                AreaLevel = 0;

            return AreaId;
        }
    }
    #endregion

    #region Faction.dbc
    public class FactionReader : DbcReader<Faction>
    {
        public Faction GetFaction(int id)
        {
            return RecordDataIndexed.Values.ToArray().FirstOrDefault(a => a.FactionFlag == id);
        }
    }

    public class Faction : DbcRecordBase
    {
        public int FactionId;
        public int FactionFlag;
        public int[] Flags = new int[4];
        public int[] ReputationStats = new int[4];
        public int[] ReputationFlags = new int[4];
        public string FactionName;

        public override int Read()
        {
            FactionId = GetInt32(0);
            FactionFlag = GetInt32(1);

            Flags[0] = GetInt32(2);
            Flags[1] = GetInt32(3);
            Flags[2] = GetInt32(4);
            Flags[3] = GetInt32(5);

            ReputationStats[0] = GetInt32(10);
            ReputationStats[1] = GetInt32(11);
            ReputationStats[2] = GetInt32(12);
            ReputationStats[3] = GetInt32(13);

            ReputationFlags[0] = GetInt32(14);
            ReputationFlags[1] = GetInt32(15);
            ReputationFlags[2] = GetInt32(16);
            ReputationFlags[3] = GetInt32(17);

            FactionName = GetString(19);

            return FactionId;
        }
    }
    #endregion

    #region CharStartOutfit.dbc
    public class CharStartOutfitReader : DbcReader<CharStartOutfit>
    {
        public CharStartOutfit Get(uint Class, uint race, uint gender)
        {
            return RecordDataIndexed.Values.ToArray().FirstOrDefault(c => c.Class == Class && c.Race == race && c.Gender == gender);
        }
    }

    public class CharStartOutfit : DbcRecordBase
    {
        public uint Id;
        public uint Race;
        public uint Class;
        public uint Gender;
        public uint[] Items = new uint[24];

        public override int Read()
        {
            Id = GetUInt32(0);

            var tmp = GetUInt32(1);
            Race = tmp & 0xFF;
            Class = (tmp >> 8) & 0xFF;
            Gender = (tmp >> 16) & 0xFF;

            for (int i = 0; i < Items.Length; ++i)
                Items[i] = GetUInt32(2 + i);

            return (int)Id;
        }
    }
    #endregion
}