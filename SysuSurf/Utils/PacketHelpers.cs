// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using SysuSurf.Eap;

namespace SysuSurf.Utils
{
    internal static class PacketHelpers
    {
        internal static ReadOnlySpan<byte> GetEthernetHeader(ReadOnlySpan<byte> src, ReadOnlySpan<byte> dst, ushort type)
        {
            Span<byte> buffer = new byte[src.Length + dst.Length + sizeof(ushort)];
            dst.CopyTo(buffer);
            src.CopyTo(buffer[dst.Length..]);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[(src.Length + dst.Length)..], type);
            return buffer;
        }

        internal static ReadOnlySpan<byte> GetEapolPacket(EapolCode code, ReadOnlySpan<byte> payload = default)
        {
            Span<byte> buffer = new byte[4 + payload.Length];
            buffer[0] = 1;
            buffer[1] = (byte)code;
            BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)payload.Length);
            payload.CopyTo(buffer[4..]);
            return buffer;
        }

        internal static ReadOnlySpan<byte> GetEapPacket(EapCode code, byte id, EapMethod type, ReadOnlySpan<byte> data = default)
        {
            switch (code)
            {
                case EapCode.Success:
                case EapCode.Failure:
                    {
                        var buffer = new byte[] { (byte)code, id, 0, 0 };
                        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan()[2..], 4);
                        return buffer;
                    }
                default:
                    {
                        Span<byte> buffer = new byte[5 + data.Length];
                        buffer[0] = (byte)code;
                        buffer[1] = id;
                        BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)(5 + data.Length));
                        buffer[4] = (byte)type;
                        data.CopyTo(buffer[5..]);
                        return buffer;
                    }
            }
        }
    }
}