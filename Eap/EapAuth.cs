// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        public EapAuth(SurfOptions options, IHostLifetime lifetime, ILogger<TAuth> logger)
        {
            if (options is not TOptions tOptions)
            {
                throw new InvalidOperationException("Invalid Options.");
            }
            
            this.options = tOptions;
            this.lifetime = lifetime;
            this.logger = logger;
        }
    }
}