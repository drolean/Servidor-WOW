﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Database;
using Common.Database.Tables;
using Common.Database.Xml;
using Common.Globals;
using Common.Helpers;
using Common.Network;
using RealmServer.Game.Entitys;

namespace RealmServer.Game
{
    public sealed class UpdateObject : PacketServer
    {
        // MUDAR ISSO AQUI PARA OUTRO
        private static readonly Random Random = new Random();
        public static float GetRandomNumber(double minimum, double maximum)
        {
            return (float) (Random.NextDouble() * (maximum - minimum) + minimum);
        }

        //
        private UpdateObject(List<byte[]> blocks, int hasTansport = 0) : base(RealmCMD.SMSG_UPDATE_OBJECT)
        {
            Write((uint) blocks.Count);
            Write((byte) hasTansport);
            blocks.ForEach(Write);
        }

        // Create Character on World
        internal static UpdateObject CreateOwnCharacterUpdate(Characters character, out PlayerEntity entity)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte) ObjectUpdateType.UPDATETYPE_CREATE_OBJECT_SELF);

            writer.WritePackedUInt64((ulong) character.Id);

            writer.Write((byte) TypeId.TypeidPlayer);

            const ObjectUpdateFlag updateFlags = ObjectUpdateFlag.UpdateflagAll |
                                                 ObjectUpdateFlag.UpdateflagHasPosition |
                                                 ObjectUpdateFlag.UpdateflagLiving |
                                                 ObjectUpdateFlag.UpdateflagSelf;

            writer.Write((byte) updateFlags);

            writer.Write((uint) MovementFlags.MoveflagNone);
            writer.Write((uint) Environment.TickCount);

            writer.Write(character.MapX);
            writer.Write(character.MapY);
            writer.Write(character.MapZ);
            writer.Write(character.MapO);

            writer.Write((float) 0);

            writer.Write(2.5f);
            writer.Write(7f * 10);
            writer.Write(4.5f);
            writer.Write(4.72f);
            writer.Write(2.5f);
            writer.Write(3.14f);

            writer.Write(0x1);

            entity = new PlayerEntity(character)
            {
                ObjectGuid = new ObjectGuid((ulong) character.Id),
                Guid = (ulong) character.Id
            };
            entity.WriteUpdateFields(writer);

            return new UpdateObject(new List<byte[]> {((MemoryStream) writer.BaseStream).ToArray()});
        }

        // Create Game Object on World
        internal static UpdateObject CreateGameObject(zoneObjeto gameObject)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte)ObjectUpdateType.UPDATETYPE_CREATE_OBJECT);

            GameObjectEntity entity = new GameObjectEntity(gameObject);

            writer.WritePackedUInt64(entity.ObjectGuid.RawGuid);

            writer.Write((byte)TypeId.TypeidGameobject);

            ObjectUpdateFlag updateFlags = ObjectUpdateFlag.UpdateflagTransport |
                                           ObjectUpdateFlag.UpdateflagAll |
                                           ObjectUpdateFlag.UpdateflagHasPosition;

            writer.Write((byte)updateFlags);

            writer.Write(gameObject.map.mapX);
            writer.Write(gameObject.map.mapY);
            writer.Write(gameObject.map.mapZ);

            writer.Write((float)0);

            writer.Write((uint)0x1);
            writer.Write((uint)0);

            entity.WriteUpdateFields(writer);

            return new UpdateObject(new List<byte[]> { (writer.BaseStream as MemoryStream)?.ToArray() }, 1);
        }

        internal static UpdateObject CreateItem(CharactersInventorys inventory, Characters sessionCharacter)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte) ObjectUpdateType.UPDATETYPE_CREATE_OBJECT);

            ItemEntity entity = new ItemEntity(inventory, sessionCharacter)
            {
                ObjectGuid = new ObjectGuid(inventory.item),
                Guid = inventory.item
            };

            writer.WritePackedUInt64(entity.ObjectGuid.RawGuid);
            writer.Write((byte) TypeId.TypeidItem);

            ObjectUpdateFlag updateFlags = ObjectUpdateFlag.UpdateflagTransport |
                                           ObjectUpdateFlag.UpdateflagAll |
                                           ObjectUpdateFlag.UpdateflagHasPosition;

            writer.Write((byte) updateFlags);

            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0f);

            writer.Write((float) 0);

            writer.Write((uint) 0x1);
            writer.Write((uint) 0);

            entity.WriteUpdateFields(writer);

            return new UpdateObject(new List<byte[]> {(writer.BaseStream as MemoryStream)?.ToArray()});
        }

        // USO INTERNO Criando OBJETO
        internal static UpdateObject CreateGameObject(float x, float y, float z)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte) ObjectUpdateType.UPDATETYPE_CREATE_OBJECT);

            var abacate = XmlReader.ObjectsAzeroth.objeto.First(a => a.id == 31000001);

            Console.WriteLine(abacate.name);

            GameObjectEntity entity = new GameObjectEntity(abacate);

            writer.WritePackedUInt64(entity.ObjectGuid.RawGuid);

            writer.Write((byte) TypeId.TypeidGameobject);

            ObjectUpdateFlag updateFlags = ObjectUpdateFlag.UpdateflagTransport |
                                           ObjectUpdateFlag.UpdateflagAll |
                                           ObjectUpdateFlag.UpdateflagHasPosition;

            writer.Write((byte) updateFlags);

            // Position

            writer.Write(x);
            writer.Write(y);
            writer.Write(z);

            writer.Write((float) 0); // R

            writer.Write((uint) 0x1); // Unkown... time?
            writer.Write((uint) 0); // Unkown... time?

            entity.WriteUpdateFields(writer);

            return new UpdateObject(new List<byte[]> {(writer.BaseStream as MemoryStream)?.ToArray()}, 1);
        }

        public static int Abab;
        // USO INTERNO Criando Spawn NPC
        internal static UpdateObject CreateUnit(float x, float y, float z, float o)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte) ObjectUpdateType.UPDATETYPE_CREATE_OBJECT);

            var abacate = XmlReader.ObjectsAzeroth.objeto.First(a => a.id == 31000001);

            UnitEntity entity = new UnitEntity(abacate);

            writer.WritePackedUInt64(entity.ObjectGuid.RawGuid);

            writer.Write((byte) TypeId.TypeidUnit);

            ObjectUpdateFlag updateFlags = ObjectUpdateFlag.UpdateflagAll |
                                           ObjectUpdateFlag.UpdateflagLiving |
                                           ObjectUpdateFlag.UpdateflagHasPosition;

            writer.Write((byte) updateFlags);
            writer.Write((UInt32) 0x00000000); //MovementFlags
            writer.Write((UInt32) Environment.TickCount); // Time

            // Position
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
            // Check if spawn have a Orientation if not random
            writer.Write(GetRandomNumber(0, 12)); // R

            // Movement speeds
            writer.Write((float) 0); // ????

            writer.Write(2.5f);  // MOVE_WALK
            writer.Write(7f);    // MOVE_RUN
            writer.Write(4.5f);  // MOVE_RUN_BACK
            writer.Write(4.72f); // MOVE_SWIM
            writer.Write(2.5f);  // MOVE_SWIM_BACK
            writer.Write(3.14f); // MOVE_TURN_RATE

            writer.Write(Abab);   // Unkown...

            entity.WriteUpdateFields(writer);
            Console.WriteLine($@"Vem aqui cao => {Abab}");
            Abab++;

            return new UpdateObject(new List<byte[]> {(writer.BaseStream as MemoryStream)?.ToArray()}, 1);
        }

        // Out of Range Player in World
        internal static PacketServer CreateOutOfRangeUpdate(List<ObjectEntity> despawnPlayer)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte) ObjectUpdateType.UPDATETYPE_OUT_OF_RANGE_OBJECTS);
            writer.Write((uint) despawnPlayer.Count);

            foreach (ObjectEntity entity in despawnPlayer)
            {
                writer.WritePackedUInt64(entity.ObjectGuid.RawGuid);
            }

            return new UpdateObject(new List<byte[]> {((MemoryStream) writer.BaseStream).ToArray()});
        }

        // Update Values for Character 
        internal static PacketServer CreateCharacterUpdate(Characters character)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte)ObjectUpdateType.UPDATETYPE_CREATE_OBJECT_SELF);

            byte[] guidBytes = GenerateGuidBytes((ulong)character.Id);
            WriteBytes(writer, guidBytes, guidBytes.Length);

            writer.Write((byte)TypeId.TypeidPlayer);

            ObjectUpdateFlag updateFlags = ObjectUpdateFlag.UpdateflagAll |
                                           ObjectUpdateFlag.UpdateflagHasPosition |
                                           ObjectUpdateFlag.UpdateflagLiving;

            writer.Write((byte)updateFlags);

            writer.Write((uint)MovementFlags.MoveflagNone);
            writer.Write((uint)Environment.TickCount); // Time?

            // Position
            writer.Write(character.MapX);
            writer.Write(character.MapY);
            writer.Write(character.MapZ);
            writer.Write(character.MapO); // R

            // Movement speeds
            writer.Write((float)0); // ????

            writer.Write(2.5f); // MOVE_WALK
            writer.Write(7f); // MOVE_RUN
            writer.Write(4.5f); // MOVE_RUN_BACK
            writer.Write(4.72f * 20); // MOVE_SWIM
            writer.Write(2.5f); // MOVE_SWIM_BACK
            writer.Write(3.14f); // MOVE_TURN_RATE

            writer.Write(0x1); // Unkown...

            PlayerEntity playerEntity = new PlayerEntity(character) { Guid = (uint)character.Id };

            playerEntity.WriteUpdateFields(writer);

            return new UpdateObject(new List<byte[]> { (writer.BaseStream as MemoryStream)?.ToArray() });
        }

        // Update Values Player on World
        internal static PacketServer UpdateValues(PlayerEntity player)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte)ObjectUpdateType.UPDATETYPE_VALUES);

            byte[] guidBytes = GenerateGuidBytes(player.ObjectGuid.RawGuid);
            WriteBytes(writer, guidBytes, guidBytes.Length);

            player.WriteUpdateFields(writer);

            return new UpdateObject(new List<byte[]> { (writer.BaseStream as MemoryStream)?.ToArray() });
        }

        // Out of Range Object in World
        internal static PacketServer CreateOutOfRangeUpdate(List<zoneObjeto> despawnObjeto)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            writer.Write((byte)ObjectUpdateType.UPDATETYPE_OUT_OF_RANGE_OBJECTS);
            writer.Write((uint)1);

            foreach (zoneObjeto entity in despawnObjeto)
            {
                writer.WritePackedUInt64(entity.id);
            }

            return new UpdateObject(new List<byte[]> { ((MemoryStream)writer.BaseStream).ToArray() });
        }

        //
        internal static byte[] GenerateGuidBytes(ulong id)
        {
            byte[] packedGuid = new byte[9];
            byte length = 1;

            for (byte i = 0; id != 0; i++)
            {
                if ((id & 0xFF) != 0)
                {
                    packedGuid[0] |= (byte) (1 << i);
                    packedGuid[length] = (byte) (id & 0xFF);
                    ++length;
                }

                id >>= 8;
            }

            byte[] clippedArray = new byte[length];
            Array.Copy(packedGuid, clippedArray, length);

            return clippedArray;
        }

        //
        internal static void WriteBytes(BinaryWriter writer, byte[] data, int count = 0)
        {
            if (count == 0)
                writer.Write(data);
            else
                writer.Write(data, 0, count);
        }

    }

    public enum TypeId : byte
    {
        TypeidObject = 0,
        TypeidItem = 1,
        TypeidContainer = 2,
        TypeidUnit = 3,
        TypeidPlayer = 4,
        TypeidGameobject = 5,
        TypeidDynamicobject = 6,
        TypeidCorpse = 7
    }

    [Flags]
    public enum ObjectUpdateFlag : byte
    {
        UpdateflagNone = 0x0000,
        UpdateflagSelf = 0x0001,
        UpdateflagTransport = 0x0002,
        UpdateflagFullguid = 0x0004,
        UpdateflagHighguid = 0x0008,
        UpdateflagAll = 0x0010,
        UpdateflagLiving = 0x0020,
        UpdateflagHasPosition = 0x0040
    }

    [Flags]
    public enum MovementFlags
    {
        MoveflagNone = 0x00000000,
        MoveflagForward = 0x00000001,
        MoveflagBackward = 0x00000002,
        MoveflagStrafeLeft = 0x00000004,
        MoveflagStrafeRight = 0x00000008,
        MoveflagTurnLeft = 0x00000010,
        MoveflagTurnRight = 0x00000020,
        MoveflagPitchUp = 0x00000040,
        MoveflagPitchDown = 0x00000080,
        MoveflagWalkMode = 0x00000100, // Walking

        MoveflagLevitating = 0x00000400,
        MoveflagRoot = 0x00000800, // [-ZERO] is it really need and correct value
        MoveflagFalling = 0x00002000,
        MoveflagFallingfar = 0x00004000,
        MoveflagSwimming = 0x00200000, // appears with fly flag also
        MoveflagAscending = 0x00400000, // [-ZERO] is it really need and correct value
        MoveflagCanFly = 0x00800000, // [-ZERO] is it really need and correct value
        MoveflagFlying = 0x01000000, // [-ZERO] is it really need and correct value

        MoveflagOntransport = 0x02000000, // Used for flying on some creatures
        MoveflagSplineElevation = 0x04000000, // used for flight paths
        MoveflagSplineEnabled = 0x08000000, // used for flight paths
        MoveflagWaterwalking = 0x10000000, // prevent unit from falling through water
        MoveflagSafeFall = 0x20000000, // active rogue safe fall spell (passive)
        MoveflagHover = 0x40000000
    }
}