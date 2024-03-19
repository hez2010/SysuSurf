// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        private readonly static ReadOnlyMemory<byte> versionInfo = (byte[])[0x06, 0x07, .. Encoding.ASCII.GetBytes("bjQ7SE8BZ3MqHhs3clMregcDY3Y="), 0x20, 0x20];
        private readonly ReadOnlyMemory<byte> userName;
        private readonly ReadOnlyMemory<byte> password;
        private readonly ReadOnlyMemory<byte> paddedPassword;
        private DateTime lastRequest;

        public H3CEapAuth(SurfOptions options, IHostLifetime lifetime, ILogger<H3CEapAuth> logger) : base(options, lifetime, logger)
        {
            userName = Encoding.ASCII.GetBytes(options.UserName);
            password = Encoding.ASCII.GetBytes(options.Password.Length > 16 ? options.Password[0..16] : options.Password);
            paddedPassword = (byte[])[.. password.Span, .. Enumerable.Repeat<byte>(0, 16 - password.Length)];
        }

        private void SendResponse(byte id, EapMethod type, ReadOnlySpan<byte> data = default)
        {
            device.SendPacket([.. ethernetHeader.Span, .. PacketHelpers.GetEapolPacket(EapolCode.Packet, PacketHelpers.GetEapPacket(EapCode.Response, id, type, data))]);
        }

        private void SendIdResponse(byte id)
        {
            logger.LogInformation("Send Id Response.");
            SendResponse(id, EapMethod.Identity, [.. versionInfo.Span, .. userName.Span]);
        }

        private void SendH3cResponse(byte id)
        {
            logger.LogInformation("Send H3C Response.");
            SendResponse(id, EapMethod.SysuH3C, [(byte)password.Length, .. password.Span, .. userName.Span]);
        }

        private void SendMd5Response(byte id, ReadOnlySpan<byte> md5Data)
        {
            Assert(md5Data.Length == 16);
            logger.LogInformation("Send MD5-Challenge Response.");
            Span<byte> digest;

            if (options.Md5Method == H3CMd5ChallengeMethod.Xor)
            {
                digest = new byte[16];
                for (var i = 0; i < 16; i++)
                {
                    digest[i] = (byte)(paddedPassword.Span[i] ^ md5Data[i]);
                }
            }
            else
            {
                digest = MD5.HashData([id, .. password.Span, .. md5Data]);
            }
            SendResponse(id, EapMethod.Md5, [(byte)digest.Length, .. digest, .. userName.Span]);
        }

        private void SendStartRequest()
        {
            logger.LogInformation("Send EAPOL Start Request.");
            lastRequest = DateTime.Now;
            device.SendPacket([.. ethernetHeader.Span, .. PacketHelpers.GetEapolPacket(EapolCode.Start)]);
        }

        protected override void EapWorker(EapWorkerState state)
        {
            SendStartRequest();
            while (!state.CancellationToken.IsCancellationRequested)
            {
                if (device.GetNextPacket(out var packet) == GetPacketStatus.PacketRead)
                {
                    var buffer = packet.Data;
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
                                    logger.LogWarning("Got EAP Failure.");
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
                                            ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(state.CancellationToken), false);
                                            return;
                                    }
                                }
                                break;
                            case EapCode.Request:
                                if (buffer.Length < 8) break;

                                var reqType = (EapMethod)buffer[8];

                                ReadOnlySpan<byte> data;
                                if (buffer.Length <= 8 || 4 + eapLen <= 9)
                                {
                                    data = [];
                                }
                                else
                                {
                                    data = buffer[9..Math.Min(buffer.Length, 4 + eapLen)];
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
                                logger.LogInformation($"Got Message: {Encoding.Default.GetString(buffer[12..])}.");
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
                    ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(state.CancellationToken), false);
                    return;
                }
            }
        }
    }
}