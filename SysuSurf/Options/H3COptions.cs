// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

namespace SysuSurf.Options
{
    public enum H3CMd5ChallengeMethod { Xor, Md5 }
    public record H3COptions(
        string UserName,
        string Password,
        string DeviceName,
        H3CMd5ChallengeMethod Md5Method) : SurfOptions(UserName, Password, DeviceName);
}
