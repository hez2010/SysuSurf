// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

namespace SysuSurf.Options
{
    public enum RuijieGroupcastMode { Standard, Private, Saier }
    public enum RuijieDhcpMode { None, SecondAuth, AfterAuth, BeforeAuth }
    public record RuijieOptions(
        string UserName,
        string Password,
        string DeviceName,
        RuijieGroupcastMode GroupcastMode,
        RuijieDhcpMode DhcpMode
    ) : SurfOptions(UserName, Password, DeviceName);
}
