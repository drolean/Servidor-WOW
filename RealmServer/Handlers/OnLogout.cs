﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Globals;
using RealmServer.Database;
using RealmServer.Enums;
using RealmServer.PacketServer;
using RealmServer.World.Managers;

namespace RealmServer.Handlers
{
    public class OnLogout
    {
        private static Dictionary<RealmServerSession, DateTime> _logoutQueue;
        private static readonly bool KeepGoing = true;

        private static void Update(int sec)
        {
            while (KeepGoing)
            {
                foreach (var entry in _logoutQueue.ToArray())
                {
                    if (DateTime.Now.Subtract(entry.Value).Seconds < sec)
                        continue;

                    // Save Character
                    Characters.UpdateCharacter(entry.Key.Character);

                    // Send Packet
                    entry.Key.SendPacket(new SMSG_LOGOUT_COMPLETE(0));
                    entry.Key.Entity.KnownPlayers.Clear();
                    WorldManager.DispatchOnPlayerDespawn(entry.Key.Entity);
                    _logoutQueue.Remove(entry.Key);
                }

                Thread.Sleep(1000);
            }
        }

        public static void Request(RealmServerSession session, byte[] data)
        {
            _logoutQueue = new Dictionary<RealmServerSession, DateTime>();
            if (_logoutQueue.ContainsKey(session)) _logoutQueue.Remove(session);

            session.SendPacket(new SMSG_LOGOUT_RESPONSE(LogoutResponseCode.LOGOUT_RESPONSE_ACCEPTED));

            session.Entity.SetUpdateField((int) UnitFields.UNIT_FIELD_FLAGS, UnitFlags.UNIT_FLAG_STUNTED);
            session.Entity.SetUpdateField((int) UnitFields.UNIT_FIELD_BYTES_1, StandStates.Sit);

            session.SendPacket(new SMSG_FORCE_MOVE_ROOT(session.Character.Uid));

            Characters.UpdateCharacter(session.Character);

            _logoutQueue.Add(session, DateTime.Now);
            var thread = new Thread(() => Update(1));
            thread.Start();
        }

        public static void Cancel(RealmServerSession session, byte[] data)
        {
            session.SendPacket(new SMSG_LOGOUT_CANCEL_ACK());

            session.Entity.SetUpdateField((int) UnitFields.UNIT_FIELD_FLAGS, UnitFlags.UNIT_FLAG_NONE);
            session.Entity.SetUpdateField((int) UnitFields.UNIT_FIELD_BYTES_1, StandStates.Stand);

            session.SendPacket(new SMSG_FORCE_MOVE_UNROOT(session.Character.Uid, 0));
        }
    }
}
