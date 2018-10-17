﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using Common.Database;
using Common.Database.Dbc;
using Common.Globals;
using Common.Helpers;
using RealmServer.Handlers;
using RealmServer.PacketReader;

namespace RealmServer
{
    internal class MainProgram
    {
        private static bool _keepGoing = true;
        private static readonly uint Time = Common.Helpers.Time.GetMsTime();
        private static readonly IPEndPoint RealmPoint = new IPEndPoint(IPAddress.Any, 1001);

        public static readonly AreaTableReader AreaTableReader = new AreaTableReader();
        public static readonly CharStartOutfitReader CharacterOutfitReader = new CharStartOutfitReader();
        public static readonly ChrRacesReader ChrRacesReader = new ChrRacesReader();
        public static readonly EmotesTextReader EmotesTextReader = new EmotesTextReader();
        public static readonly FactionReader FactionReader = new FactionReader();
        public static readonly MapReader MapReader = new MapReader();

        public static readonly List<RealmEnums> MovementOpcodes = new List<RealmEnums>
        {
            RealmEnums.MSG_MOVE_HEARTBEAT,
            RealmEnums.MSG_MOVE_START_FORWARD,
            RealmEnums.MSG_MOVE_START_BACKWARD,
            RealmEnums.MSG_MOVE_STOP,
            RealmEnums.MSG_MOVE_START_STRAFE_LEFT,
            RealmEnums.MSG_MOVE_START_STRAFE_RIGHT,
            RealmEnums.MSG_MOVE_STOP_STRAFE,
            RealmEnums.MSG_MOVE_JUMP,
            RealmEnums.MSG_MOVE_START_TURN_LEFT,
            RealmEnums.MSG_MOVE_START_TURN_RIGHT,
            RealmEnums.MSG_MOVE_STOP_TURN,
            RealmEnums.MSG_MOVE_START_PITCH_UP,
            RealmEnums.MSG_MOVE_START_PITCH_DOWN,
            RealmEnums.MSG_MOVE_STOP_PITCH,
            RealmEnums.MSG_MOVE_SET_RUN_MODE,
            RealmEnums.MSG_MOVE_SET_WALK_MODE,
            RealmEnums.MSG_MOVE_SET_FACING,
            RealmEnums.MSG_MOVE_SET_PITCH,
            RealmEnums.MSG_MOVE_START_SWIM,
            RealmEnums.MSG_MOVE_STOP_SWIM
        };

        public static RealmServerClass RealmServerClass { get; set; }
        public static RealmServerDatabase RealmServerDatabase { get; set; }

        private static void Main()
        {
            // Set Culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            Console.SetWindowSize(
                Math.Min(110, Console.LargestWindowWidth),
                Math.Min(20, Console.LargestWindowHeight)
            );
            Console.Title =
                $@"{Assembly.GetExecutingAssembly().GetName().Name} v{
                        Assembly.GetExecutingAssembly().GetName().Version
                    }";

            Log.Print(LogType.RealmServer, $"Version {Assembly.GetExecutingAssembly().GetName().Version}");
            Log.Print(LogType.RealmServer, $"Running on .NET Framework Version {Environment.Version}");

            ConfigFile();

            XmlReader.Boot();
            //
            DbcInit();
            //
            Initalizing();

            Log.Print(LogType.RealmServer, $"Running from: {AppDomain.CurrentDomain.BaseDirectory}");
            Log.Print(LogType.RealmServer,
                $"Successfully started in {Common.Helpers.Time.GetMsTimeDiff(Time, Common.Helpers.Time.GetMsTime()) / 100}ms");


            // Commands
            while (_keepGoing)
            {
                var command = Console.ReadLine();
                switch (command)
                {
                    case "/config":
                    case "config":
                        ConfigFile(true);
                        break;
                    case "/up":
                    case "up":
                        Log.Print(LogType.Console,
                            $"Uptime {DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()}");
                        break;

                    case "/reload":
                    case "reload":
                        Log.Print(LogType.Console, "XML reloaded.");
                        break;

                    case "/gc":
                    case "gc":
                        Console.Clear();
                        GC.Collect();
                        Log.Print(LogType.Console,
                            $"Total Memory: {Convert.ToSingle(GC.GetTotalMemory(false) / 1024 / 1024)}MB");
                        break;

                    case "/q":
                    case "q":
                        Log.Print(LogType.Console, "Halting process ".PadRight(40, '.'));
                        Thread.Sleep(500);
                        _keepGoing = false;
                        Environment.Exit(-1);
                        return;

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

        private static void Initalizing()
        {
            RealmServerClass = new RealmServerClass(RealmPoint);
            RealmServerDatabase = new RealmServerDatabase();

            // Handlers
            RealmServerRouter.AddHandler<CMSG_AUTH_SESSION>(RealmEnums.CMSG_AUTH_SESSION, OnAuthSession.Handler);
            RealmServerRouter.AddHandler<CMSG_PING>(RealmEnums.CMSG_PING, OnPing.Handler);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_CHAR_ENUM, OnCharEnum.Handler);
            RealmServerRouter.AddHandler<CMSG_CHAR_CREATE>(RealmEnums.CMSG_CHAR_CREATE, OnCharCreate.Handler);
            RealmServerRouter.AddHandler<CMSG_CHAR_DELETE>(RealmEnums.CMSG_CHAR_DELETE, OnCharDelete.Handler);
            RealmServerRouter.AddHandler<CMSG_PLAYER_LOGIN>(RealmEnums.CMSG_PLAYER_LOGIN, OnPlayerLogin.Handler);

            RealmServerRouter.AddHandler<CMSG_UPDATE_ACCOUNT_DATA>(RealmEnums.CMSG_UPDATE_ACCOUNT_DATA,
                OnUpdateAccountData.Handler);
            RealmServerRouter.AddHandler<CMSG_STANDSTATECHANGE>(RealmEnums.CMSG_STANDSTATECHANGE,
                OnStandStateChange.Handler);
            RealmServerRouter.AddHandler<CMSG_NAME_QUERY>(RealmEnums.CMSG_NAME_QUERY, OnNameQuery.Handler);

            RealmServerRouter.AddHandler(RealmEnums.CMSG_REQUEST_RAID_INFO, OnRequestRaidInfo.Handler);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GMTICKET_GETTICKET, OnGmTicketGetTicket.Handler);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_QUERY_TIME, OnQueryTime.Handler);
            RealmServerRouter.AddHandler(RealmEnums.MSG_QUERY_NEXT_MAIL_TIME, OnQueryNextMailTime.Handler);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_BATTLEFIELD_STATUS, OnBattleFieldStatus.Handler);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_MEETINGSTONE_INFO, OnMeetingStoneInfo.Handler);

            RealmServerRouter.AddHandler<CMSG_ZONEUPDATE>(RealmEnums.CMSG_ZONEUPDATE, OnZoneUpdate.Handler);
            RealmServerRouter.AddHandler<CMSG_JOIN_CHANNEL>(RealmEnums.CMSG_JOIN_CHANNEL, OnJoinChannel.Handler);
            RealmServerRouter.AddHandler<CMSG_SET_ACTIVE_MOVER>(RealmEnums.CMSG_SET_ACTIVE_MOVER,
                OnSetActiveMover.Handler);
            RealmServerRouter.AddHandler<MSG_MOVE_FALL_LAND>(RealmEnums.MSG_MOVE_FALL_LAND, OnMoveFallLand.Handler);

            RealmServerRouter.AddHandler(RealmEnums.CMSG_COMPLETE_CINEMATIC, OnCompleteCinematic.Handler);
            RealmServerRouter.AddHandler<CMSG_TUTORIAL_FLAG>(RealmEnums.CMSG_TUTORIAL_FLAG, OnTutorialFlag.Handler);

            RealmServerRouter.AddHandler(RealmEnums.CMSG_TUTORIAL_CLEAR, OnTutorialClear.Handler);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_TUTORIAL_RESET, OnTutorialReset.Handler);

            RealmServerRouter.AddHandler<CMSG_SET_FACTION_ATWAR>(RealmEnums.CMSG_SET_FACTION_ATWAR,
                OnSetFactionAtWar.Handler);

            MovementOpcodes.ForEach(code => RealmServerRouter.AddHandler(code, OnMovements.Handler(code)));
            /*
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GMTICKET_SYSTEMSTATUS, OnGmTicketSystemStatus.Handler);
            RealmServerRouter.AddHandler<CMSG_GMTICKET_CREATE>(RealmEnums.CMSG_GMTICKET_CREATE, OnGmTicketCreate.Handler); 
            RealmServerRouter.AddHandler(RealmEnums.CMSG_LOGOUT_REQUEST, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_LOGOUT_CANCEL, Future);
            RealmServerRouter.AddHandler<CMSG_PET_NAME_QUERY>(RealmEnums.CMSG_PET_NAME_QUERY, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_QUERY>(RealmEnums.CMSG_GUILD_QUERY, Future);
            RealmServerRouter.AddHandler<CMSG_ITEM_QUERY_SINGLE>(RealmEnums.CMSG_ITEM_QUERY_SINGLE, Future);
            RealmServerRouter.AddHandler<CMSG_PAGE_TEXT_QUERY>(RealmEnums.CMSG_PAGE_TEXT_QUERY, Future);
            RealmServerRouter.AddHandler<CMSG_QUEST_QUERY>(RealmEnums.CMSG_PAGE_TEXT_QUERY, Future);
            RealmServerRouter.AddHandler<CMSG_GAMEOBJECT_QUERY>(RealmEnums.CMSG_PAGE_TEXT_QUERY, Future);
            RealmServerRouter.AddHandler<CMSG_CREATURE_QUERY>(RealmEnums.CMSG_PAGE_TEXT_QUERY, Future);
            RealmServerRouter.AddHandler<CMSG_WHO>(RealmEnums.CMSG_WHO, Future);
            RealmServerRouter.AddHandler<CMSG_WHOIS>(RealmEnums.CMSG_WHOIS, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_FRIEND_LIST, Future);
            RealmServerRouter.AddHandler<CMSG_ADD_FRIEND>(RealmEnums.CMSG_ADD_FRIEND, Future);
            RealmServerRouter.AddHandler<CMSG_DEL_FRIEND>(RealmEnums.CMSG_DEL_FRIEND, Future);
            RealmServerRouter.AddHandler<CMSG_ADD_IGNORE>(RealmEnums.CMSG_ADD_IGNORE, Future);
            RealmServerRouter.AddHandler<CMSG_DEL_IGNORE>(RealmEnums.CMSG_DEL_IGNORE, Future);
            RealmServerRouter.AddHandler<CMSG_GROUP_INVITE>(RealmEnums.CMSG_GROUP_INVITE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GROUP_CANCEL, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GROUP_ACCEPT, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GROUP_DECLINE, Future);
            RealmServerRouter.AddHandler<CMSG_GROUP_UNINVITE>(RealmEnums.CMSG_GROUP_UNINVITE, Future);
            RealmServerRouter.AddHandler<CMSG_GROUP_UNINVITE_GUID>(RealmEnums.CMSG_GROUP_UNINVITE_GUID, Future);
            RealmServerRouter.AddHandler<CMSG_GROUP_SET_LEADER>(RealmEnums.CMSG_GROUP_SET_LEADER, Future);
            RealmServerRouter.AddHandler<CMSG_LOOT_METHOD>(RealmEnums.CMSG_LOOT_METHOD, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GROUP_DISBAND, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_CREATE>(RealmEnums.CMSG_GUILD_CREATE, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_INVITE>(RealmEnums.CMSG_GUILD_INVITE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GUILD_ACCEPT, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GUILD_DECLINE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GUILD_INFO, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GUILD_ROSTER, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_PROMOTE>(RealmEnums.CMSG_GUILD_PROMOTE, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_DEMOTE>(RealmEnums.CMSG_GUILD_DEMOTE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GUILD_LEAVE, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_REMOVE>(RealmEnums.CMSG_GUILD_REMOVE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_GUILD_DISBAND, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_LEADER>(RealmEnums.CMSG_GUILD_LEADER, Future);
            RealmServerRouter.AddHandler<CMSG_GUILD_MOTD>(RealmEnums.CMSG_GUILD_MOTD, Future);

            RealmServerRouter.AddHandler<CMSG_MESSAGECHAT>(RealmEnums.CMSG_MESSAGECHAT, Future);

            RealmServerRouter.AddHandler<CMSG_LEAVE_CHANNEL>(RealmEnums.CMSG_LEAVE_CHANNEL, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_LIST>(RealmEnums.CMSG_CHANNEL_LIST, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_PASSWORD>(RealmEnums.CMSG_CHANNEL_PASSWORD, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_SET_OWNER>(RealmEnums.CMSG_CHANNEL_SET_OWNER, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_OWNER>(RealmEnums.CMSG_CHANNEL_OWNER, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_MODERATOR>(RealmEnums.CMSG_CHANNEL_MODERATOR, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_UNMODERATOR>(RealmEnums.CMSG_CHANNEL_UNMODERATOR, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_MUTE>(RealmEnums.CMSG_CHANNEL_MUTE, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_UNMUTE>(RealmEnums.CMSG_CHANNEL_UNMUTE, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_INVITE>(RealmEnums.CMSG_CHANNEL_INVITE, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_KICK>(RealmEnums.CMSG_CHANNEL_KICK, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_BAN>(RealmEnums.CMSG_CHANNEL_BAN, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_UNBAN>(RealmEnums.CMSG_CHANNEL_UNBAN, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_ANNOUNCEMENTS>(RealmEnums.CMSG_CHANNEL_ANNOUNCEMENTS, Future);
            RealmServerRouter.AddHandler<CMSG_CHANNEL_MODERATE>(RealmEnums.CMSG_CHANNEL_MODERATE, Future);

            RealmServerRouter.AddHandler<CMSG_USE_ITEM>(RealmEnums.CMSG_USE_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_OPEN_ITEM>(RealmEnums.CMSG_OPEN_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_READ_ITEM>(RealmEnums.CMSG_READ_ITEM, Future);

            RealmServerRouter.AddHandler<CMSG_AREATRIGGER>(RealmEnums.CMSG_AREATRIGGER, Future);

            RealmServerRouter.AddHandler<CMSG_EMOTE>(RealmEnums.CMSG_EMOTE, Future);
            RealmServerRouter.AddHandler<CMSG_TEXT_EMOTE>(RealmEnums.CMSG_TEXT_EMOTE, Future);

            RealmServerRouter.AddHandler<CMSG_AUTOSTORE_LOOT_ITEM>(RealmEnums.CMSG_AUTOSTORE_LOOT_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_AUTOEQUIP_ITEM>(RealmEnums.CMSG_AUTOEQUIP_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_AUTOSTORE_BAG_ITEM>(RealmEnums.CMSG_AUTOSTORE_BAG_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_SWAP_ITEM>(RealmEnums.CMSG_SWAP_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_SWAP_INV_ITEM>(RealmEnums.CMSG_SWAP_INV_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_SPLIT_ITEM>(RealmEnums.CMSG_SPLIT_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_DESTROYITEM>(RealmEnums.CMSG_DESTROYITEM, Future);

            RealmServerRouter.AddHandler<CMSG_INSPECT>(RealmEnums.CMSG_INSPECT, Future);

            RealmServerRouter.AddHandler<CMSG_INITIATE_TRADE>(RealmEnums.CMSG_INITIATE_TRADE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_BEGIN_TRADE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_BUSY_TRADE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_IGNORE_TRADE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_ACCEPT_TRADE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_UNACCEPT_TRADE, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_CANCEL_TRADE, Future);
            RealmServerRouter.AddHandler<CMSG_SET_TRADE_ITEM>(RealmEnums.CMSG_SET_TRADE_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_CLEAR_TRADE_ITEM>(RealmEnums.CMSG_CLEAR_TRADE_ITEM, Future);
            RealmServerRouter.AddHandler<CMSG_SET_TRADE_GOLD>(RealmEnums.CMSG_SET_TRADE_GOLD, Future);

            RealmServerRouter.AddHandler<CMSG_SET_ACTION_BUTTON>(RealmEnums.CMSG_SET_ACTION_BUTTON, Future);

            RealmServerRouter.AddHandler<CMSG_CAST_SPELL>(RealmEnums.CMSG_CAST_SPELL, Future);
            RealmServerRouter.AddHandler<CMSG_CANCEL_CAST>(RealmEnums.CMSG_CANCEL_CAST, Future);
            RealmServerRouter.AddHandler<CMSG_CANCEL_AURA>(RealmEnums.CMSG_CANCEL_AURA, Future);
            RealmServerRouter.AddHandler<CMSG_CANCEL_CHANNELLING>(RealmEnums.CMSG_CANCEL_CHANNELLING, Future);

            RealmServerRouter.AddHandler<CMSG_SET_SELECTION>(RealmEnums.CMSG_SET_SELECTION, Future);
            RealmServerRouter.AddHandler<CMSG_ATTACKSWING>(RealmEnums.CMSG_ATTACKSWING, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_ATTACKSTOP, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_REPOP_REQUEST, Future);

            RealmServerRouter.AddHandler<CMSG_LOOT>(RealmEnums.CMSG_LOOT, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_LOOT_MONEY, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_LOOT_RELEASE, Future);

            RealmServerRouter.AddHandler(RealmEnums.CMSG_MOUNTSPECIAL_ANIM, Future);

            RealmServerRouter.AddHandler<CMSG_PET_SET_ACTION>(RealmEnums.CMSG_PET_SET_ACTION, Future);
            RealmServerRouter.AddHandler<CMSG_PET_ACTION>(RealmEnums.CMSG_PET_ACTION, Future);
            RealmServerRouter.AddHandler<CMSG_PET_ABANDON>(RealmEnums.CMSG_PET_ABANDON, Future);
            RealmServerRouter.AddHandler<CMSG_PET_RENAME>(RealmEnums.CMSG_PET_RENAME, Future);

            RealmServerRouter.AddHandler<CMSG_GOSSIP_HELLO>(RealmEnums.CMSG_GOSSIP_HELLO, Future);
            RealmServerRouter.AddHandler<CMSG_GOSSIP_SELECT_OPTION>(RealmEnums.CMSG_GOSSIP_SELECT_OPTION, Future);
            RealmServerRouter.AddHandler<CMSG_NPC_TEXT_QUERY>(RealmEnums.CMSG_NPC_TEXT_QUERY, Future);

            RealmServerRouter.AddHandler<CMSG_QUESTGIVER_HELLO>(RealmEnums.CMSG_QUESTGIVER_HELLO, Future);
            RealmServerRouter.AddHandler<CMSG_QUESTGIVER_QUERY_QUEST>(RealmEnums.CMSG_QUESTGIVER_QUERY_QUEST, Future);
            RealmServerRouter.AddHandler<CMSG_QUESTGIVER_ACCEPT_QUEST>(RealmEnums.CMSG_QUESTGIVER_ACCEPT_QUEST, Future);
            RealmServerRouter.AddHandler<CMSG_QUESTGIVER_COMPLETE_QUEST>(RealmEnums.CMSG_QUESTGIVER_COMPLETE_QUEST, Future);
            RealmServerRouter.AddHandler<CMSG_QUESTGIVER_REQUEST_REWARD>(RealmEnums.CMSG_QUESTGIVER_REQUEST_REWARD, Future);
            RealmServerRouter.AddHandler<CMSG_QUESTGIVER_CHOOSE_REWARD>(RealmEnums.CMSG_QUESTGIVER_CHOOSE_REWARD, Future);
            RealmServerRouter.AddHandler(RealmEnums.CMSG_QUESTGIVER_CANCEL, Future);
            RealmServerRouter.AddHandler<CMSG_QUESTLOG_REMOVE_QUEST>(RealmEnums.CMSG_QUESTLOG_REMOVE_QUEST, Future); 
            RealmServerRouter.AddHandler(RealmEnums.CMSG_QUEST_CONFIRM_ACCEPT, Future);
            RealmServerRouter.AddHandler<CMSG_PUSHQUESTTOPARTY>(RealmEnums.CMSG_PUSHQUESTTOPARTY, Future); 
            */

        }

        private static void Future(RealmServerSession session, byte[] data)
        {
            throw new NotImplementedException();
        }

        private static async void DbcInit()
        {
            Log.Print(LogType.RealmServer, "Loading DBCs ".PadRight(40, '.') + " [OK] ");
            await AreaTableReader.Load("AreaTable.dbc");
            await CharacterOutfitReader.Load("CharStartOutfit.dbc");
            await ChrRacesReader.Load("ChrRaces.dbc");
            await EmotesTextReader.Load("EmotesText.dbc");
            await FactionReader.Load("Faction.dbc");
            await MapReader.Load("Map.dbc");
        }

        private static void ConfigFile(bool reload = false)
        {
            Log.Print(LogType.RealmServer,
                reload
                    ? "Reloading settings ".PadRight(40, '.') + " [OK]"
                    : "Loading settings ".PadRight(40, '.') + " [OK] ");

            try
            {
                Config.Load();
            }
            catch (Exception)
            {
                Log.Print(LogType.RealmServer, "Failed to load config from file. Loading default config.");
                Config.Default();
                Log.Print(LogType.RealmServer, "Saving config ".PadRight(40, '.') + " [OK] ");
                Config.Instance.Save();
            }
        }

        private static void PrintHelp()
        {
            Console.Clear();
            Console.WriteLine(@"RealmServer help
Commands:
  /config   Reload configuration file.
  /g 'msg'  Send Global message to players.
  /reload   Reload XML.
  /gc       Show garbage collection.
  /up       Show uptime.
  /q 900    Shutdown server in 900sec = 15min. *Debug exit now!
  /help     Show this help.");
        }
    }
}