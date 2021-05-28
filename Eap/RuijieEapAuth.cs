using System;
using SysuSurf.Options;

namespace SysuSurf.Eap
{
    public sealed class RuijieEapAuth : EapAuth<RuijieOptions>
    {
        private bool disposed = false;

        public RuijieEapAuth(SurfOptions options) : base(options)
        {
            throw new NotImplementedException("Ruijie authentication is not implemented.");
        }

        ~RuijieEapAuth()
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public override void LogOff()
        {

        }
    }
}