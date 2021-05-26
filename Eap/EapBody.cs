using System;
using System.IO;

namespace SysuH3c.Eap
{
    public class EapBody
    {
        public static EapBodyType GetBodyType(ReadOnlySpan<byte> body) => body[0] switch
        {
            1 => EapBodyType.Request,
            2 => EapBodyType.Response,
            _ => throw new InvalidDataException($"Unexpected message type {body[0]}.")
        };

        public static ReadOnlySpan<byte> GetBodyData(ReadOnlySpan<byte> body) => body[1..];

        private static readonly ReadOnlyMemory<byte> requestType = new byte[] { 1 };

        public static ReadOnlyMemory<byte> CreateRequestBody(ReadOnlyMemory<byte> data)
        {
            Memory<byte> buffer = new byte[sizeof(byte) + data.Length];
            requestType.CopyTo(buffer);
            data.CopyTo(buffer[1..]);
            return buffer;
        }
    }
}