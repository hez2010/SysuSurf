using System;
using System.Buffers.Binary;
using System.Linq;
using SysuSurf.Eap;

namespace SysuSurf.Utils
{
    public static class PacketHelpers
    {
        public static ReadOnlyMemory<byte> GetEthernetHeader(ReadOnlyMemory<byte> src, ReadOnlyMemory<byte> dst, ushort type)
        {
            Memory<byte> buffer = new byte[src.Length + dst.Length + sizeof(ushort)];
            dst.CopyTo(buffer);
            src.CopyTo(buffer[dst.Length..]);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Span[(src.Length + dst.Length)..], type);
            return buffer;
        }

        public static ReadOnlyMemory<byte> GetEapolPacket(EapolCode code, ReadOnlyMemory<byte> payload = default)
        {
            Memory<byte> buffer = new byte[4 + payload.Length];
            buffer.Span[0] = 1;
            buffer.Span[1] = (byte)code;
            var lengthBuffer = BitConverter.GetBytes((ushort)payload.Length);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Span[2..], (ushort)payload.Length);
            payload.CopyTo(buffer[4..]);
            return buffer;
        }

        public static ReadOnlyMemory<byte> GetEapPacket(EapCode code, byte id, EapMethod type, ReadOnlyMemory<byte> data = default)
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
                        Memory<byte> buffer = new byte[5 + data.Length];
                        buffer.Span[0] = (byte)code;
                        buffer.Span[1] = id;
                        BinaryPrimitives.WriteUInt16BigEndian(buffer.Span[2..], (ushort)(5 + data.Length));
                        buffer.Span[4] = (byte)type;
                        data.CopyTo(buffer[5..]);
                        return buffer;
                    }
            }
        }

        public static ReadOnlyMemory<byte> Concat(this ReadOnlyMemory<byte> buffer, ReadOnlyMemory<byte> other)
        {
            Memory<byte> result = new byte[buffer.Length + other.Length];
            buffer.CopyTo(result);
            other.CopyTo(result[buffer.Length..]);
            return result;
        }
    }
}