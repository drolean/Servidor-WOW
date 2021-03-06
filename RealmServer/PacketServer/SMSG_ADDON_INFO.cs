﻿using System.Collections.Generic;
using Common.Globals;

namespace RealmServer.PacketServer
{
    /// <summary>
    ///     SMSG_ADDON_INFO represents a server packet with the addon info.
    /// </summary>
    internal sealed class SMSG_ADDON_INFO : Common.Network.PacketServer
    {
        /// <summary>
        /// </summary>
        /// <param name="addOnsNames"></param>
        public SMSG_ADDON_INFO(List<string> addOnsNames) : base(RealmEnums.SMSG_ADDON_INFO)
        {
            AddOnsNames = addOnsNames;
            for (var i = 0; i <= addOnsNames.Count; i++)
            {
                /*	
                if (File.Exists($"interface\\{addOnsNames[i]}.pub"))	
                {	
                    Write((byte)2); // AddOn Type [1-enabled, 0-banned, 2-blizzard]	
                    Write((byte)1);	
                    FileStream fs = new FileStream($"interface\\{addOnsNames[i]}.pub", FileMode.Open, FileAccess.Read,	
                    FileShare.Read, 258, FileOptions.SequentialScan);	
                    byte[] fb = new byte[257];	
                    fs.Read(fb, 0, 257);	
                    //NOTE: Read from file	
                    //AddByteArray(fb);	
                    Write((uint)0);	
                    Write((short)0);	
                } else	
                */
                Write((byte) 2); // AddOn Type [1-enabled, 0-banned, 2-blizzard]	
                Write((byte) 1);
                Write((uint) 0);
                Write((short) 0);
            }
        }

        /// <summary>
        /// </summary>
        public List<string> AddOnsNames { get; }
    }
}
