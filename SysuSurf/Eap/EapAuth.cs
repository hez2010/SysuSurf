// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpPcap;
using SharpPcap.LibPcap;
using SysuSurf.Eap;
using SysuSurf.Options;
using SysuSurf.Utils;

namespace SysuSurf
{
    public abstract class EapAuth : IHostedService, IDisposable
    {
        public abstract void Dispose();
        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract Task StopAsync(CancellationToken cancellationToken);
    }

    public abstract class EapAuth<TAuth, TOptions> : EapAuth where TOptions : SurfOptions where TAuth : EapAuth<TAuth, TOptions>
    {
        protected readonly TOptions options;
        protected readonly IHostLifetime lifetime;
        protected readonly ILogger<TAuth> logger;
        protected readonly ILiveDevice device;
        protected bool disposed = false;
        protected bool hasLogOff = false;
        protected DateTime lastRequest;

        protected readonly ReadOnlyMemory<byte> ethernetHeader;
        protected readonly CancellationTokenSource cancellationTokenSource;

        private static ReadOnlySpan<byte> PaeGroupAddr => [0x01, 0x80, 0xc2, 0x00, 0x00, 0x03];

        public EapAuth(SurfOptions options, IHostLifetime lifetime, ILogger<TAuth> logger)
        {
            if (options is not TOptions tOptions)
            {
                throw new InvalidOperationException("Invalid Options.");
            }

            this.options = tOptions;
            this.lifetime = lifetime;
            this.logger = logger;

            var devices = LibPcapLiveDeviceList.Instance;
            var device = devices.FirstOrDefault(i => i.Name == options.DeviceName);
            if (device is null)
            {
                var deviceMessage = devices.Count > 0 ? $"Available devices: \nDevice Name (Device Description) \n{devices.Select(i => $"{i.Name} ({i.Description})").Aggregate((a, n) => $"{a}\n{n}")}" : "No available network devices.";
                throw new FileNotFoundException($"Network device '{options.DeviceName}' doesn't exist. \n{deviceMessage}");
            }

            this.device = device;
            cancellationTokenSource = new();

            this.ethernetHeader = PacketHelpers.GetEthernetHeader(device.MacAddress.GetAddressBytes(), PaeGroupAddr, 0x888e).ToArray();
        }

        protected void SendResponse(byte id, EapMethod type, ReadOnlySpan<byte> data = default)
        {
            device.SendPacket([.. ethernetHeader.Span, .. PacketHelpers.GetEapolPacket(EapolCode.Packet, PacketHelpers.GetEapPacket(EapCode.Response, id, type, data))]);
        }

        protected void SendStartRequest()
        {
            logger.LogInformation("Send EAPOL Start Request.");
            lastRequest = DateTime.Now;
            device.SendPacket([.. ethernetHeader.Span, .. PacketHelpers.GetEapolPacket(EapolCode.Start)]);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            device.Open(DeviceModes.NoCaptureLocal | DeviceModes.NoCaptureRemote);
            device.Filter = "not (tcp or udp or arp or rarp or ip or ip6)";
            ThreadPool.UnsafeQueueUserWorkItem(EapWorker, new EapWorkerState(cancellationTokenSource.Token), false);

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("Send EAPOL LogOff Request.");
            hasLogOff = true;
            device.SendPacket([.. ethernetHeader.Span, .. PacketHelpers.GetEapolPacket(EapolCode.LogOff)]);
            cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        protected abstract void EapWorker(EapWorkerState state);

        ~EapAuth()
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                cancellationTokenSource.Dispose();
                device.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}