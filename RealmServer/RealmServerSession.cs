﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common.Crypt;
using Common.Database.Tables;
using Common.Globals;
using Common.Helpers;
using Common.Network;
using RealmServer.Game.Entitys;
using RealmServer.Handlers;

namespace RealmServer
{
    public class RealmServerSession
    {
        public Socket ConnectionSocket { get; }
        public VanillaCrypt PacketCrypto { get; set; }
        public const int BufferSize = 2048 * 2;
        public int ConnectionId { get; }
        public byte[] DataBuffer { get; }
        public string ConnectionRemoteIp => ConnectionSocket.RemoteEndPoint.ToString();
        public static List<RealmServerSession> Sessions = new List<RealmServerSession>();

        //
        public Users Users { get; set; }
        public uint OutOfSyncDelay { get; set; }

        public Characters Target;
        public Characters Character;
        public PlayerEntity Entity;

        internal RealmServerSession(int connectionId, Socket connectionSocket)
        {
            ConnectionId = connectionId;
            ConnectionSocket = connectionSocket;
            DataBuffer = new byte[BufferSize];

            try
            {
                Log.Print(LogType.RealmServer, $"Incoming connection from [{ConnectionRemoteIp}]");
                ConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, DataArrival, null);
            }
            catch (SocketException e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                Disconnect();
            }

            Thread.Sleep(500);

            // Connection Packet
            using (PacketServer packet = new PacketServer(RealmCMD.SMSG_AUTH_CHALLENGE))
            {
                // TODO: review this
                packet.WriteBytes(new byte[] { 0x33, 0x18, 0x34, 0xC8 });
                SendPacket(packet);
            }
        }

        internal void SendPacket(PacketServer packet)
        {
            LogPacket(packet);
            SendPacket(packet.Opcode, packet.Packet);
        }

        private void SendPacket(int opcode, byte[] data)
        {
            Log.Print(LogType.RealmServer, $"[{ConnectionSocket.RemoteEndPoint}] [SERVER] [{((RealmCMD)opcode).ToString().PadRight(25, ' ')}] = {data.Length}");
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            byte[] header = Encode(data.Length, opcode);
            writer.Write(header);
            writer.Write(data);
            SendData(((MemoryStream)writer.BaseStream).ToArray());
        }

        internal void SendData(byte[] send)
        {
            byte[] buffer = new byte[send.Length];
            Buffer.BlockCopy(send, 0, buffer, 0, send.Length);

            try
            {
                ConnectionSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, delegate { }, null);
            }
            catch (SocketException e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                Disconnect();
            }
            catch (NullReferenceException e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                Disconnect();
            }
        }

        private void Disconnect()
        {
            try
            {
                Log.Print(LogType.RealmServer, "User Disconnected");
                ConnectionSocket.Shutdown(SocketShutdown.Both);
                ConnectionSocket.Close();
            }
            catch (Exception e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
            }
        }

        internal virtual void DataArrival(IAsyncResult asyncResult)
        {
            int bytesRecived = 0;

            try
            {
                bytesRecived = ConnectionSocket.EndReceive(asyncResult);
            }
            catch (Exception e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
            }

            if (bytesRecived != 0)
            {
                byte[] data = new byte[bytesRecived];
                Array.Copy(DataBuffer, data, bytesRecived);

                OnPacket(data);

                try
                {
                    ConnectionSocket.BeginReceive(DataBuffer, 0, DataBuffer.Length, SocketFlags.None, DataArrival, null);
                }
                catch (SocketException e)
                {
                    var trace = new StackTrace(e, true);
                    Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                    ConnectionSocket.Close();
                }
                catch (Exception e)
                {
                    var trace = new StackTrace(e, true);
                    Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void OnPacket(byte[] data)
        {
            try
            {
                for (int index = 0; index < data.Length; index++)
                {
                    byte[] headerData = new byte[6];
                    Array.Copy(data, index, headerData, 0, 6);

                    ushort length;
                    short opcode;

                    Decode(headerData, out length, out opcode);
                    Log.Print(LogType.RealmServer, $"[{ConnectionSocket.RemoteEndPoint}] [CLIENT] [{((RealmCMD)opcode).ToString().PadRight(25, ' ')}] = {length}");
                    RealmCMD code = (RealmCMD)opcode;

                    byte[] packetDate = new byte[length];
                    Array.Copy(data, index + 6, packetDate, 0, length - 4);
                    RealmServerRouter.CallHandler(this, code, packetDate);

                    index += 2 + (length - 1);
                }
            }
            catch (Exception e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
                DumpPacket(data, this);
            }
        }

        internal static void DumpPacket(byte[] data, RealmServerSession client = null)
        {
            int j;
            string buffer = "";

            Log.Print(client == null
                ? "DEBUG: Packet Dump"
                : $"[{client.ConnectionSocket.RemoteEndPoint}] DEBUG: Packet Dump");

            if (data.Length % 16 == 0)
            {
                for (j = 0; j <= data.Length - 1; j += 16)
                {
                    Log.Print($"| {BitConverter.ToString(data, j, 16).Replace("-", " ")} | " +
                              Encoding.ASCII.GetString(data, j, 16)
                                  .Replace("\t", "?")
                                  .Replace("\b", "?")
                                  .Replace("\r", "?")
                                  .Replace("\f", "?")
                                  .Replace("\n", "?") + " |");
                }
            }
            else
            {
                for (j = 0; j <= data.Length - 1 - 16; j += 16)
                {
                    Log.Print($"| {BitConverter.ToString(data, j, 16).Replace("-", " ")} | " +
                              Encoding.ASCII.GetString(data, j, 16)
                                  .Replace("\t", "?")
                                  .Replace("\b", "?")
                                  .Replace("\r", "?")
                                  .Replace("\f", "?")
                                  .Replace("\n", "?") + " |");
                }

                Log.Print($"| {BitConverter.ToString(data, j, data.Length % 16).Replace("-", " ")} " +
                          $"{buffer.PadLeft((16 - data.Length % 16) * 3, ' ')}" +
                          "| " + Encoding.ASCII.GetString(data, j, data.Length % 16)
                              .Replace("\t", "?")
                              .Replace("\b", "?")
                              .Replace("\r", "?")
                              .Replace("\f", "?")
                              .Replace("\n", "?") +
                          $"{buffer.PadLeft(16 - data.Length % 16, ' ')}|");
            }
        }

        private byte[] Encode(int size, int opcode)
        {
            int index = 0;
            int newSize = size + 2;
            byte[] header = new byte[4];
            if (newSize > 0x7FFF)
                header[index++] = (byte)(0x80 | (0xFF & (newSize >> 16)));

            header[index++] = (byte)(0xFF & (newSize >> 8));
            header[index++] = (byte)(0xFF & (newSize >> 0));
            header[index++] = (byte)(0xFF & opcode);
            header[index] = (byte)(0xFF & (opcode >> 8));

            if (PacketCrypto != null) header = PacketCrypto.Encrypt(header);

            return header;
        }

        private void Decode(byte[] header, out ushort length, out short opcode)
        {
            PacketCrypto?.Decrypt(header, 6);

            if (PacketCrypto == null)
            {
                length = BitConverter.ToUInt16(new[] { header[1], header[0] }, 0);
                opcode = BitConverter.ToInt16(header, 2);
            }
            else
            {
                length = BitConverter.ToUInt16(new[] { header[1], header[0] }, 0);
                opcode = BitConverter.ToInt16(new[] { header[2], header[3] }, 0);
            }
        }

        private static string ByteArrayToHex(IReadOnlyCollection<byte> data)
        {
            string packetOutput = string.Empty;

            for (int i = 0; i < data.Count; i++)
            {
                packetOutput += i.ToString("X2") + " ";
            }

            return packetOutput;
        }

        private static void LogPacket(PacketServer packet)
        {
            try
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");

                var filename = $"logs/packet_log-{DateTime.Now:yyyy-M-d}.txt";
                if (!File.Exists(filename)) File.Create(filename).Close();

                using (StreamWriter w = File.AppendText(filename))
                {
                    w.WriteLine($"[{DateTime.Now:yyyy-M-d H:mm:ss}] = Opcode: {(RealmCMD)packet.Opcode} [Length: {packet.Packet.Length}]");
                    w.Write(ByteArrayToHex(packet.Packet));
                    w.WriteLine();
                    w.WriteLine();
                }
            }
            catch (Exception e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
            }
        }

        /// <summary>
        /// Send World Message System
        /// </summary>
        /// <param name="msg">message</param>
        internal void SendMessageMotd(string msg)
        {
            SendPacket(new SmsgMessagechat(ChatMessageType.CHAT_MSG_SYSTEM, ChatMessageLanguage.LANG_UNIVERSAL,
                (ulong) Character.Id, msg));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        internal void TransmitToAll(PacketServer packet)
        {
            Sessions.FindAll(s => s.Character != null).ForEach(s => s.SendPacket(packet));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        internal static RealmServerSession GetSessionByPlayerName(string playerName)
        {
            return Sessions.First(user => user.Character.name.ToLower() == playerName.ToLower());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        internal static RealmServerSession GetSessionByUserName(string userName)
        {
            return Sessions.Find(user => user.ConnectionId == int.Parse(userName));
        }
    }
}