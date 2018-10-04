﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using Common.Database;
using Common.Database.Dbc;
using Common.Globals;
using Common.Helpers;
using Common.Network;
using RealmServer.Game.Entitys;
using RealmServer.Game.Managers;
using RealmServer.Handlers;
using RealmServer.Scripting;

namespace RealmServer
{
    internal class MainProgram
    {
        // Distance Config
        public static int DistanceConfig = 75;
        public static int LimitCharacterPerRealm = 10;
        public static int IsJitI = 1;

        public static RealmServerDatabase Database { get; set; }
        public static RealmServerClass RealmServerClass { get; set; }

        private static void Main()
        {
            // Set Culture
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-us");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

            Console.SetWindowSize(
                Math.Min(110, Console.LargestWindowWidth),
                Math.Min(20, Console.LargestWindowHeight));

            var time = Time.GetMsTime();
            var realmPoint = new IPEndPoint(IPAddress.Any, 1001);

            Console.Title = $@"{Assembly.GetExecutingAssembly().GetName().Name} v{Assembly.GetExecutingAssembly().GetName().Version}";

            Log.Print(LogType.RealmServer, $"Version {Assembly.GetExecutingAssembly().GetName().Version}");
            Log.Print(LogType.RealmServer, $"Running on .NET Framework Version {Environment.Version}");

            // Boot
            XmlReader.Boot();
            ScriptManager.Boot();
            PlayerManager.Boot();
            AiBrain.Boot();

            // Database
            Database = new RealmServerDatabase();
            DatabaseManager();

            // Initali
            IntializePacketHandlers();

            // Socket Class
            RealmServerClass = new RealmServerClass(realmPoint);

            // TODO: Set Realm to ONLINE

            Log.Print(LogType.RealmServer, $"Running from: {AppDomain.CurrentDomain.BaseDirectory}");
            Log.Print(LogType.RealmServer, $"Successfully started in {Time.GetMsTimeDiff(time, Time.GetMsTime()) / 100}ms");

            // Commands
            while (true)
            {
                var command = Console.ReadLine();
                switch (command)
                {
                    case "/db":
                    case "db":
                        XmlReader.Boot();
                        Log.Print(LogType.Console, "XML reloaded.");
                        break;

                    case "/gc":
                    case "gc":
                        GC.Collect();
                        Log.Print(LogType.Console,
                            $"Total Memory: {Convert.ToSingle(GC.GetTotalMemory(false) / 1024 / 1024)}MB");
                        break;

                    case "/q":
                    case "q":
                        // TODO: set all players to OFFLine
                        //       send Global message to shutdown server
                        //       set realm to offline
                        Log.Print(LogType.Console, "Halting process...");
                        Thread.Sleep(500);
                        Environment.Exit(-1);
                        break;

                    case "/help":
                    case "help":
                    case "/?":
                    case "?":
                        PrintHelp();
                        Console.WriteLine();
                        break;
                    default:
                        Log.Print(LogType.Debug, $"Unknown Command: {command}");
                        break;
                }
            }
        }

        private static void IntializePacketHandlers()
        {
            RealmServerRouter.AddHandler<CmsgAuthSession>(RealmCMD.CMSG_AUTH_SESSION, RealmServerHandler.OnAuthSession);
            RealmServerRouter.AddHandler<CmsgPing>(RealmCMD.CMSG_PING, RealmServerHandler.OnPingPacket);

            // Character Handlers
            RealmServerRouter.AddHandler(RealmCMD.CMSG_CHAR_ENUM, CharacterHandler.OnCharEnum);
            RealmServerRouter.AddHandler<CmsgCharCreate>(RealmCMD.CMSG_CHAR_CREATE, CharacterHandler.OnCharCreate);

            
            /*
            // Character Handlers
            RealmServerRouter.AddHandler(RealmCMD.CMSG_CHAR_ENUM, CharacterHandler.OnCharEnum);                             // DONE
            
            RealmServerRouter.AddHandler<CmsgCharRename>(RealmCMD.CMSG_CHAR_RENAME, CharacterHandler.OnCharRename);         // DONE
            RealmServerRouter.AddHandler<CmsgCharDelete>(RealmCMD.CMSG_CHAR_DELETE, CharacterHandler.OnCharDelete);         // DONE
            RealmServerRouter.AddHandler<CmsgPlayerLogin>(RealmCMD.CMSG_PLAYER_LOGIN, CharacterHandler.OnPlayerLogin);
            RealmServerRouter.AddHandler<CmsgUpdateAccountData>(RealmCMD.CMSG_UPDATE_ACCOUNT_DATA, CharacterHandler.OnUpdateAccountData);
            RealmServerRouter.AddHandler(RealmCMD.CMSG_LOGOUT_REQUEST, CharacterHandler.OnLogoutRequest);                   // PARTIAL
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_LOGOUT_CANCEL, CharacterHandler.OnLogoutCancel);       // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_STANDSTATECHANGE, CharacterHandler.OnStandStateChange);// PARTIAL

            // Miscs Handlers
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_NAME_QUERY, MiscHandler.OnNameQuery);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SET_ACTIVE_MOVER, MiscHandler.OnSetActiveMover);
            RealmServerRouter.AddHandler(RealmCMD.CMSG_QUERY_TIME, MiscHandler.OnQueryTime);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_BATTLEFIELD_STATUS, MiscHandler.OnBattlefieldStatus);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_MEETINGSTONE_INFO, MiscHandler.OnMeetingstoneInfo);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_TEXT_EMOTE, MiscHandler.OnTextEmote);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SET_FACTION_ATWAR, MiscHandler.OnSetFactionAtwar);         // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SET_FACTION_INACTIVE, MiscHandler.OnSetFactionInactive);   // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SET_WATCHED_FACTION, MiscHandler.OnSetWatchedFaction);     // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_COMPLETE_CINEMATIC, MiscHandler.OnCompleteCinematic);      // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_TUTORIAL_FLAG, MiscHandler.OnTutorialFlag);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_TUTORIAL_CLEAR, MiscHandler.OnTutorialClear);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_TUTORIAL_RESET, MiscHandler.OnTutorialReset);

            // Group Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_REQUEST_RAID_INFO, GroupHandler.OnRequestRaidInfo);

            // GM Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_GMTICKET_GETTICKET, GmHandler.OnGmTicketGetTicket);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_GMTICKET_SYSTEMSTATUS, GmHandler.OnGmTicketSystemStatus);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_GMTICKET_CREATE, GmHandler.OnGmTicketCreate);

            // Mail Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.MSG_QUERY_NEXT_MAIL_TIME, MailHandler.OnQueryNextMailTime);

            // Character Movement Handler
            MovementOpcodes.ForEach(code => RealmServerRouter.AddHandler(code, MovementHandler.GenerateResponse(code)));
            RealmServerRouter.AddHandler<CmsgMoveTimeSkipped>(RealmCMD.CMSG_MOVE_TIME_SKIPPED, MovementHandler.OnMoveTimeSkipped);
            RealmServerRouter.AddHandler<MsgMoveInfo>(RealmCMD.MSG_MOVE_FALL_LAND, MovementHandler.OnMoveFallLand);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_ZONEUPDATE, MovementHandler.OnZoneUpdate);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_AREATRIGGER, MovementHandler.OnAreaTrigger);

            // Chat Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_JOIN_CHANNEL, ChatHandler.OnJoinChannel);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_LEAVE_CHANNEL, ChatHandler.OnLeaveChannel);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_LIST, ChatHandler.OnChannelList);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_PASSWORD, ChatHandler.OnChannelPassword);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_SET_OWNER, ChatHandler.OnChannelSetOwner);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_OWNER, ChatHandler.OnChannelOwner);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_MODERATOR, ChatHandler.OnChannelModerator);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_UNMODERATOR, ChatHandler.OnChannelUnmoderator);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_MUTE, ChatHandler.OnChannelMute);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_UNMUTE, ChatHandler.OnChannelUnmute);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_KICK, ChatHandler.OnChannelKick);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_INVITE, ChatHandler.OnChannelInvite);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_BAN, ChatHandler.OnChannelBan);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_UNBAN, ChatHandler.OnChannelUnban);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_ANNOUNCEMENTS, ChatHandler.OnChannelAnnouncements);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CHANNEL_MODERATE, ChatHandler.OnChannelModerate);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_MESSAGECHAT, ChatHandler.OnMessageChat);

            // Trade Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CANCEL_TRADE, TradeHandler.OnCancelTrade);

            // Combat Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SETSHEATHED, CombatHandler.OnSetsHeathed);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SET_SELECTION, CombatHandler.OnSetSelection);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_ATTACKSWING, CombatHandler.OnAttackSwing);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_ATTACKSTOP, CombatHandler.OnAttackStop);

            // Item Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_ITEM_QUERY_SINGLE, ItemHandler.OnItemQuerySingle);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SWAP_INV_ITEM, ItemHandler.OnSwapInvItem);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_DESTROYITEM, ItemHandler.OnDestroyItem);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_USE_ITEM, ItemHandler.OnUseItem);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_AUTOEQUIP_ITEM, ItemHandler.OnAutoEquipItem);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_SPLIT_ITEM, ItemHandler.OnSplitItem);

            // Social Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_FRIEND_LIST, SocialHandler.OnFriendList);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_ADD_FRIEND, SocialHandler.OnAddFriend);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_ADD_IGNORE, SocialHandler.OnAddIgnore);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_DEL_FRIEND, SocialHandler.OnDelFriend);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_DEL_IGNORE, SocialHandler.OnDelIgnore);
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_WHO, SocialHandler.OnWho);

            // Spell Handler
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_CAST_SPELL, SpellHandler.OnCastSpell);

            // Nulled Packets
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_NEXT_CINEMATIC_CAMERA, OnNull); // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_FORCE_MOVE_ROOT_ACK, OnNull);   // DONE
            RealmServerRouter.AddHandler<PacketReader>(RealmCMD.CMSG_FORCE_MOVE_UNROOT_ACK, OnNull); // DONE
            */
        }

        private static void OnNull(RealmServerSession session, PacketReader handler)
        {
            Log.Print(LogType.Debug, "Null Code");
        }

        private static readonly List<RealmCMD> MovementOpcodes = new List<RealmCMD>()
        {
            RealmCMD.MSG_MOVE_HEARTBEAT,
            RealmCMD.MSG_MOVE_START_FORWARD,
            RealmCMD.MSG_MOVE_START_BACKWARD,
            RealmCMD.MSG_MOVE_STOP,
            RealmCMD.MSG_MOVE_START_STRAFE_LEFT,
            RealmCMD.MSG_MOVE_START_STRAFE_RIGHT,
            RealmCMD.MSG_MOVE_STOP_STRAFE,
            RealmCMD.MSG_MOVE_JUMP,
            RealmCMD.MSG_MOVE_START_TURN_LEFT,
            RealmCMD.MSG_MOVE_START_TURN_RIGHT,
            RealmCMD.MSG_MOVE_STOP_TURN,
            RealmCMD.MSG_MOVE_START_PITCH_UP,
            RealmCMD.MSG_MOVE_START_PITCH_DOWN,
            RealmCMD.MSG_MOVE_STOP_PITCH,
            RealmCMD.MSG_MOVE_SET_RUN_MODE,
            RealmCMD.MSG_MOVE_SET_WALK_MODE,
            RealmCMD.MSG_MOVE_SET_FACING,
            RealmCMD.MSG_MOVE_SET_PITCH
        };

        public static readonly AreaTableReader AreaTableReader = new AreaTableReader();
        public static readonly CharStartOutfitReader CharacterOutfitReader = new CharStartOutfitReader();
        public static readonly ChrRacesReader ChrRacesReader = new ChrRacesReader();
        public static readonly EmotesTextReader EmotesTextReader = new EmotesTextReader();
        public static readonly FactionReader FactionReader = new FactionReader();
        public static readonly MapReader MapReader = new MapReader();

        private static async void DatabaseManager()
        {
            Log.Print(LogType.Loading, "Loading DBCs ......................... [OK]");
            await AreaTableReader.Load("AreaTable.dbc");
            await CharacterOutfitReader.Load("CharStartOutfit.dbc");
            await ChrRacesReader.Load("ChrRaces.dbc");
            await EmotesTextReader.Load("EmotesText.dbc");
            await FactionReader.Load("Faction.dbc");
            await MapReader.Load("Map.dbc");
        }

        private static void PrintHelp()
        {
            Console.Clear();
            Console.WriteLine(@"AuthServer help
Commands:
  /c 'msg'  Send Global message to players.
  /db       Reload XML.
  /gc       Show garbage collection.
  /q 900    Shutdown server in 900sec = 15min. *Debug exit now!
  /help     Show this help.
");
        }
    }
}
