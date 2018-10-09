# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.4] - 2018-10-09
### Added
- RealmEnums.CMSG_AUTH_SESSION wow send this packet to handle session.
- RealmEnums.SMSG_AUTH_RESPONSE server return this with OK or error.
- Doc for Common.Utils.ByteArrayToHex
- RealmServer Session to handler sessions between players
- RealmServer Database
  - GetAccount
- Common.Utils.DumpPacket

### Removed
- Common.Utils.Int32ToBigEndianHexByteString

### Moved
- Common.Utils.Decode moved to RealmServerSession.Decode
- Common.Utils.Encode moved to RealmServerSession.Encode

## [0.0.3] - 2018-10-08
### Added
- RealmServer Enums.
- LogPacket (Sending/Receive) on Common.Helpers.Log.
- Encode/Decode Packet for Vanilla.
- RealmServer Class to handle connections.
- RealmServer Router to handle opcodes.
- RealmEnums.SMSG_AUTH_CHALLENGE this packet is sending for every new connection.

### Removed
- Removed Print unused function.

## [0.0.2] - 2018-10-08
### Added
- App.config file

### Changed
- Improved code (Styled).

### Removed
- Non used imports.
- Log.Print of "User disconnected".

## [0.0.1] - 2018-10-08
### Added
- AuthLogonChallenge ( all packets for 1.2.* ).

### Changed
- Moved PacketReader handlers to correct folder.
- Moved PacketServer handlers to correct folder.
- Improve code.

### Removed
- All RealmServer Files.