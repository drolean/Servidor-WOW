﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common.Crypt;
using Common.Database.Tables;
using Common.Globals;
using Common.Helpers;
using Common.Network;

namespace RealmServer
{
    public class RealmServerSession
    {
        public Socket ConnectionSocket { get; }
        public VanillaCrypt PacketCrypto { get; set; }
        public const int BufferSize = 2048 * 2;
        public int ConnectionId { get; private set; }
        public byte[] DataBuffer { get; }
        public string ConnectionRemoteIp => ConnectionSocket.RemoteEndPoint.ToString();
        public static List<RealmServerSession> Sessions = new List<RealmServerSession>();

        //
        public Users Users { get; set; }

        //
        sealed class SmsgAuthChallenge : PacketServer
        {
            public SmsgAuthChallenge() : base(RealmCMD.SMSG_AUTH_CHALLENGE)
            {
                Write(1);
                Write((uint)new Random().Next(0, int.MaxValue));
                Write(0);
                Write(0);
                Write(0);
                Write(0);
            }
        }

        public RealmServerSession(int connectionId, Socket connectionSocket)
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

            SendPacket(new SmsgAuthChallenge());
        }

        public void SendPacket(PacketServer packet)
        {
            SendPacket(packet.Opcode, packet.Packet);
        }

        public void SendPacket(int opcode, byte[] data)
        {
            Log.Print(LogType.RealmServer, $"[{ConnectionSocket.RemoteEndPoint}] [INIT] Server -> Client [{((RealmCMD)opcode).ToString().PadRight(25, ' ')}] = {data.Length}");
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            byte[] header = Encode(data.Length, opcode);
            writer.Write(header);
            writer.Write(data);
            SendData(((MemoryStream)writer.BaseStream).ToArray(), Convert.ToString((RealmCMD)opcode));
        }

        internal void SendData(byte[] send, string v)
        {
            Log.Print(LogType.RealmServer, $"[{ConnectionSocket.RemoteEndPoint}] [HEAD] Server -> Client [{v.PadRight(25, ' ')}] = {send.Length}");

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
                Log.Print(LogType.AuthServer, "User Disconnected");
                ConnectionSocket.Shutdown(SocketShutdown.Both);
                ConnectionSocket.Close();
            }
            catch (Exception e)
            {
                var trace = new StackTrace(e, true);
                Log.Print(LogType.Error, $"{e.Message}: {e.Source}\n{trace.GetFrame(trace.FrameCount - 1).GetFileName()}:{trace.GetFrame(trace.FrameCount - 1).GetFileLineNumber()}");
            }
        }

        public virtual void DataArrival(IAsyncResult asyncResult)
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
                    Log.Print(LogType.RealmServer, $"[{ConnectionSocket.RemoteEndPoint}] [INCO] Client -> Server [{((RealmCMD)opcode).ToString().PadRight(25, ' ')}] = {length}");
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

        public static void DumpPacket(byte[] data, RealmServerSession client)
        {
            int j;
            string buffer = "";

            if (client == null)
                Log.Print($"DEBUG: Packet Dump");
            else
                Log.Print($"[{client.ConnectionSocket.RemoteEndPoint}] DEBUG: Packet Dump");

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

            if (PacketCrypto != null)
                PacketCrypto.Decrypt(header, 6);

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

            header[0] = BitConverter.GetBytes(opcode)[0];
            header[1] = BitConverter.GetBytes(opcode)[1];

            header[2] = BitConverter.GetBytes(length)[0];
            header[3] = BitConverter.GetBytes(length)[1];
        }
    }
}
