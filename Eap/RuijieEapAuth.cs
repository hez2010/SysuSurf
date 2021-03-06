// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SysuSurf.Options;

namespace SysuSurf.Eap
{
    public sealed class RuijieEapAuth : EapAuth<RuijieEapAuth, RuijieOptions>
    {
        public RuijieEapAuth(SurfOptions options, IHostLifetime lifetime, ILogger<RuijieEapAuth> logger) : base(options, lifetime, logger)
        {
            throw new NotImplementedException("Ruijie authentication is not yet implemented.");
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}