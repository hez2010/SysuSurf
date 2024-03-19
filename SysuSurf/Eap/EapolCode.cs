// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

namespace SysuSurf.Eap
{
    public enum EapolCode : byte
    {
        Packet, Start, LogOff, Key, Asf, RjPropKeepAlive = 0xbf
    }
}