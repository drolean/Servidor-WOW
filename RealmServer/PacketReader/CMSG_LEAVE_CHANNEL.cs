﻿using Common.Helpers;

namespace RealmServer.PacketReader
{
    public class CMSG_LEAVE_CHANNEL : Common.Network.PacketReader
    {
        public string Channel;

        public CMSG_LEAVE_CHANNEL(byte[] data) : base(data)
        {
            Channel = ReadCString();

#if DEBUG
            Log.Print(LogType.Debug, $"[CMSG_LEAVE_CHANNEL] Channel: {Channel}");
#endif
        }
    }
}