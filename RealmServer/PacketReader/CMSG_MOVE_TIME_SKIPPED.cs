﻿using System;
using Common.Helpers;

namespace RealmServer.PacketReader
{
    public sealed class CMSG_MOVE_TIME_SKIPPED : Common.Network.PacketReader
    {
        public UInt64 Uid;
        public uint Lag;

        public CMSG_MOVE_TIME_SKIPPED(byte[] data) : base(data)
        {
            Uid = ReadUInt64();
            Lag = ReadUInt32();

#if DEBUG
            Log.Print(LogType.Debug, $"[CMSG_MOVE_TIME_SKIPPED] Uid: {Uid} Lag: {Lag}");
#endif
        }
    }
}