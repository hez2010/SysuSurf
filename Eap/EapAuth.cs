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
using SysuSurf.Options;

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
        protected readonly CancellationTokenSource cancellationTokenSource;

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
                throw new FileNotFoundException($"Network device '{options.DeviceName}' doesn't exist. \nAvailable interfaces: \nDevice Name (Device Description) \n{devices.Select(i => $"{i.Name} ({i.Description})").Aggregate((a, n) => $"{a}\n{n}")}");
            }

            this.device = device;
            cancellationTokenSource = new();
        }

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