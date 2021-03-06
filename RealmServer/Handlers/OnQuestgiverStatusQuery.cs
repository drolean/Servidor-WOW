﻿using RealmServer.Enums;
using RealmServer.PacketReader;
using RealmServer.PacketServer;

namespace RealmServer.Handlers
{
    public class OnQuestgiverStatusQuery
    {
        public static void Handler(RealmServerSession session, CMSG_QUESTGIVER_STATUS_QUERY handler)
        {
            //5var status = 1;
            session.SendPacket(new SMSG_QUESTGIVER_STATUS(handler, QuestgiverStatusFlag.DIALOG_STATUS_NONE));
        }
    }
}
