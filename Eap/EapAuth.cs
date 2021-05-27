using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SharpPcap;
using SysuH3C.Utils;

namespace SysuH3C.Eap
{
    public sealed class EapAuth : IDisposable
    {
        private readonly static ReadOnlyMemory<byte> paeGroupAddr = new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x03 };
        private readonly static ReadOnlyMemory<byte> versionInfo = new byte[] { 0x06, 0x07, }.Concat(Encoding.ASCII.GetBytes("bjQ7SE8BZ3MqHhs3clMregcDY3Y=")).Concat(new byte[] { 0x20, 0x20 }).ToArray();
        private readonly ReadOnlyMemory<byte> ethernetHeader;
        private readonly ReadOnlyMemory<byte> userName;
        private readonly ReadOnlyMemory<byte> password;
        private readonly ILiveDevice device;
        private readonly EapOptions options;
        private bool disposed = false;
        private bool hasLogOff = false;

        public EapAuth(EapOptions options)
        {
            this.options = options;

            var devices = CaptureDeviceList.Instance;
            var device = devices.FirstOrDefault(i => i.Name == options.DeviceName);
            if (device is null)
            {
                throw new FileNotFoundException($"Network device '{options.DeviceName}' doesn't exist. \nAvailable interfaces: \n{devices.Select(i => $"{i.Name} ({i.Description})").Aggregate((a, n) => $"{a}\n{n}")}");
            }

            ethernetHeader = PacketHelpers.GetEthernetHeader(device.MacAddress.GetAddressBytes(), paeGroupAddr, 0x888e);
            device.Open(DeviceModes.None);
            this.device = device;

            userName = Encoding.ASCII.GetBytes(options.UserName);
            password = Encoding.ASCII.GetBytes(options.Password.Length > 16 ? options.Password[0..16] : options.Password);
            ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(), false);
        }

        ~EapAuth()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                device.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        private void SendResponse(byte id, EapMethod type, ReadOnlyMemory<byte> data = default)
        {
            device.SendPacket(ethernetHeader.Concat(PacketHelpers.GetEapolPacket(EapolCode.Packet, PacketHelpers.GetEapPacket(EapCode.Response, id, type, data))).Span);
        }

        private void SendIdResponse(byte id)
        {
            Console.WriteLine("Send Id Response.");
            SendResponse(id, EapMethod.Identity, versionInfo.Concat(userName));
        }

        private void SendH3cResponse(byte id)
        {
            Console.WriteLine("Send H3C Response.");
            SendResponse(id, EapMethod.SysuH3c, new ReadOnlyMemory<byte>(new byte[] { (byte)password.Length }).Concat(password).Concat(userName));
        }

        private void SendMd5Response(byte id, ReadOnlyMemory<byte> md5Data)
        {
            Console.WriteLine("Send MD5-Challenge Response.");
            var data = new ReadOnlyMemory<byte>(new[] { id }).Concat(password).Concat(md5Data);
            var digest = MD5.HashData(data.ToArray());
            var response = new ReadOnlyMemory<byte>(new[] { (byte)digest.Length }).Concat(digest).Concat(userName);
            SendResponse(id, EapMethod.Md5, response);
        }

        private void SendStartRequest()
        {
            Console.WriteLine("Send EAPOL Start Request.");
            device.SendPacket(ethernetHeader.Concat(PacketHelpers.GetEapolPacket(EapolCode.Start)).Span);
        }

        public void LogOff()
        {
            Console.WriteLine("Send EAPOL LogOff Request.");
            device.SendPacket(ethernetHeader.Concat(PacketHelpers.GetEapolPacket(EapolCode.LogOff)).Span);
            hasLogOff = true;
        }

        private void EapWorker(EapWorkerState state)
        {
            SendStartRequest();
            while (true)
            {
                if (device.GetNextPacket(out var packet) == GetPacketStatus.PacketRead)
                {
                    var buffer = packet.Data.ToArray().AsSpan();
                    if (buffer.Length < 14 + 8) continue;
                    buffer = buffer[14..];
                    var (_, type, _) = (buffer[0], (EapolCode)buffer[1], BinaryPrimitives.ReadInt16BigEndian(buffer[2..4]));
                    if (type == EapolCode.Packet)
                    {
                        var (code, id, eapLen) = ((EapCode)buffer[4], buffer[5], BinaryPrimitives.ReadInt16BigEndian(buffer[6..8]));
                        switch (code)
                        {
                            case EapCode.Success:
                                state.Succeeded = true;
                                Console.WriteLine("Got EAP Success.");
                                break;
                            case EapCode.Failure:
                                if (hasLogOff)
                                {
                                    Console.WriteLine("Log Off Succeeded.");
                                    Program.Semaphore.Release();
                                    return;
                                }
                                else
                                {
                                    Console.WriteLine("Got EAP Failure.");
                                    switch (state)
                                    {
                                        case { Succeeded: false, FailureCount: < 2 }:
                                            state.FailureCount++;
                                            SendStartRequest();
                                            break;
                                        case { Succeeded: false }:
                                            ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(), false);
                                            break;
                                        case { Succeeded: true, FailureCount: 0 }:
                                            state.LastId--;
                                            state.FailureCount++;
                                            SendIdResponse(state.LastId);
                                            break;
                                        case { Succeeded: true, FailureCount: < 10 }:
                                            if (state.FailureCount % 2 == 0) state.LastId++;
                                            state.FailureCount++;
                                            SendIdResponse(state.LastId);
                                            break;
                                        default:
                                            Thread.Sleep(5000);
                                            ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(), false);
                                            return;
                                    }
                                }
                                break;
                            case EapCode.Request:
                                if (buffer.Length < 8) break;

                                var reqType = (EapMethod)buffer[8];

                                byte[] data;
                                if (buffer.Length <= 8 || 4 + eapLen <= 9)
                                {
                                    data = Array.Empty<byte>();
                                }
                                else
                                {
                                    data = buffer[9..Math.Min(buffer.Length, 4 + eapLen)].ToArray();
                                }

                                switch (reqType)
                                {
                                    case EapMethod.Identity:
                                        Console.WriteLine("Got EAP Request for Identity.");
                                        state.LastId = id;
                                        SendIdResponse(id);
                                        break;
                                    case EapMethod.SysuH3c:
                                        Console.WriteLine("Got EAP Request for H3C.");
                                        SendH3cResponse(id);
                                        break;
                                    case EapMethod.Md5:
                                        Console.WriteLine("Got EAP Request for MD5-Challenge.");
                                        var dataLen = data[0];
                                        var md5Data = data[1..Math.Min(data.Length, 1 + dataLen)];
                                        SendMd5Response(id, md5Data);
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case EapCode.LoginMessage when id == 5 && buffer.Length >= 12:
                                Console.WriteLine("Got Message: " + Encoding.Default.GetString(buffer[12..]));
                                break;
                            default:
                                break;
                        }

                        if (code != EapCode.Failure)
                        {
                            state.FailureCount = 0;
                        }
                    }
                }
            }
        }
    }
}