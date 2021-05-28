// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using SysuSurf.Options;

namespace SysuSurf
{
    public abstract class EapAuth : IDisposable
    {
        public abstract void Dispose();

        public abstract void LogOff();
    }

    public abstract class EapAuth<TOptions> : EapAuth where TOptions : SurfOptions
    {
        protected readonly TOptions options;
        public EapAuth(SurfOptions options)
        {
            if (options is not TOptions tOptions)
            {
                throw new InvalidOperationException("Invalid Options.");
            }

            this.options = tOptions;
        }
    }
}