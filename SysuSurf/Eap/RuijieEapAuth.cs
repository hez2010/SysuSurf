// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpPcap;
using SysuSurf.Options;
using SysuSurf.Utils;

namespace SysuSurf.Eap
{
    public sealed class RuijieEapAuth : EapAuth<RuijieEapAuth, RuijieOptions>
    {
        public RuijieEapAuth(SurfOptions options, IHostLifetime lifetime, ILogger<RuijieEapAuth> logger) : base(options, lifetime, logger)
        {
            throw new NotImplementedException("Ruijie authentication is not yet implemented.");
        }

        protected override void EapWorker(EapWorkerState state)
        {
            SendStartRequest();
            throw new NotImplementedException();
        }
    }
}