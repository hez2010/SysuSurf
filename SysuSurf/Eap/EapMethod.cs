// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

namespace SysuSurf.Eap
{
    public enum EapMethod : byte
    {
        Identity = 1,
        Notification = 2,
        Nak = 3,
        Md5 = 4,
        Otp = 5,
        Gtc = 6,
        SysuH3C = 7,
        Expanded = 254,
        Experimental = 255
    }
}