// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpPcap;
using SysuSurf.Options;
using SysuSurf.Utils;
using static SysuSurf.Utils.AssertHelpers;

namespace SysuSurf.Eap
{
    public sealed class H3CEapAuth : EapAuth<H3CEapAuth, H3COptions>
    {
        private readonly static ReadOnlyMemory<byte> paeGroupAddr = new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x03 };
        private readonly static ReadOnlyMemory<byte> versionInfo = new byte[] { 0x06, 0x07, }.Concat(Encoding.ASCII.GetBytes("bjQ7SE8BZ3MqHhs3clMregcDY3Y=")).Concat(new byte[] { 0x20, 0x20 }).ToArray();
        private readonly ReadOnlyMemory<byte> ethernetHeader;
        private readonly ReadOnlyMemory<byte> userName;
        private readonly ReadOnlyMemory<byte> password;
        private readonly ReadOnlyMemory<byte> paddedPassword;
        private DateTime lastRequest;
        private bool hasLogOff = false;

        public H3CEapAuth(SurfOptions options, IHostLifetime lifetime, ILogger<H3CEapAuth> logger) : base(options, lifetime, logger)
        {
            ethernetHeader = PacketHelpers.GetEthernetHeader(device.MacAddress.GetAddressBytes(), paeGroupAddr, 0x888e);
            userName = Encoding.ASCII.GetBytes(options.UserName);
            password = Encoding.ASCII.GetBytes(options.Password.Length > 16 ? options.Password[0..16] : options.Password);
            paddedPassword = password.ToArray().Concat(Enumerable.Repeat<byte>(0, 16 - password.Length)).ToArray();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            device.Open(DeviceModes.NoCaptureLocal | DeviceModes.NoCaptureRemote);
            device.Filter = "not (tcp or udp or arp or rarp or ip or ip6)";
            ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState { CancellationToken = cancellationTokenSource.Token }, false);

            return Task.CompletedTask;
        }

        private void SendResponse(byte id, EapMethod type, ReadOnlyMemory<byte> data = default)
        {
            device.SendPacket(ethernetHeader.Concat(PacketHelpers.GetEapolPacket(EapolCode.Packet, PacketHelpers.GetEapPacket(EapCode.Response, id, type, data))).Span);
        }

        private void SendIdResponse(byte id)
        {
            logger.LogInformation("Send Id Response.");
            SendResponse(id, EapMethod.Identity, versionInfo.Concat(userName));
        }

        private void SendH3cResponse(byte id)
        {
            logger.LogInformation("Send H3C Response.");
            SendResponse(id, EapMethod.SysuH3C, new ReadOnlyMemory<byte>(new byte[] { (byte)password.Length }).Concat(password).Concat(userName));
        }

        private void SendMd5Response(byte id, ReadOnlyMemory<byte> md5Data)
        {
            Assert(md5Data.Length == 16);
            logger.LogInformation("Send MD5-Challenge Response.");
            byte[] digest;

            if (options.Md5Method == H3CMd5ChallengeMethod.Xor)
            {
                digest = new byte[16];
                for (var i = 0; i < 16; i++)
                {
                    digest[i] = (byte)(paddedPassword.Span[i] ^ md5Data.Span[i]);
                }
            }
            else
            {
                var data = new ReadOnlyMemory<byte>(new[] { id }).Concat(password).Concat(md5Data);
                digest = MD5.HashData(data.ToArray());
            }
            var response = new ReadOnlyMemory<byte>(new[] { (byte)digest.Length }).Concat(digest).Concat(userName);
            SendResponse(id, EapMethod.Md5, response);
        }

        private void SendStartRequest()
        {
            logger.LogInformation("Send EAPOL Start Request.");
            lastRequest = DateTime.Now;
            device.SendPacket(ethernetHeader.Concat(PacketHelpers.GetEapolPacket(EapolCode.Start)).Span);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            logger.LogInformation("Send EAPOL LogOff Request.");
            hasLogOff = true;
            device.SendPacket(ethernetHeader.Concat(PacketHelpers.GetEapolPacket(EapolCode.LogOff)).Span);
            cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private void EapWorker(EapWorkerState state)
        {
            SendStartRequest();
            while (!state.CancellationToken.IsCancellationRequested)
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
                                logger.LogInformation("Got EAP Success.");
                                break;
                            case EapCode.Failure:
                                if (hasLogOff)
                                {
                                    logger.LogInformation("Log Off Succeeded.");
                                    lifetime.StopAsync(default);
                                    return;
                                }
                                else
                                {
                                    logger.LogInformation("Got EAP Failure.");
                                    switch (state)
                                    {
                                        case { Succeeded: false, FailureCount: < 3 }:
                                            state.FailureCount++;
                                            SendStartRequest();
                                            break;
                                        case { Succeeded: true, FailureCount: < 3 }:
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
                                        logger.LogInformation("Got EAP Request for Identity.");
                                        state.LastId = id;
                                        SendIdResponse(id);
                                        break;
                                    case EapMethod.SysuH3C:
                                        logger.LogInformation("Got EAP Request for H3C.");
                                        SendH3cResponse(id);
                                        break;
                                    case EapMethod.Md5:
                                        logger.LogInformation("Got EAP Request for MD5-Challenge.");
                                        var dataLen = data[0];
                                        var md5Data = data[1..Math.Min(data.Length, 1 + dataLen)];
                                        SendMd5Response(id, md5Data);
                                        break;
                                    default:
                                        break;
                                }

                                lastRequest = DateTime.Now;
                                break;
                            case EapCode.LoginMessage when id == 5 && buffer.Length >= 12:
                                logger.LogInformation("Got Message: " + Encoding.Default.GetString(buffer[12..]));
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

                if (DateTime.Now - lastRequest > TimeSpan.FromMinutes(1))
                {
                    ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(), false);
                    return;
                }
            }
        }
    }
}