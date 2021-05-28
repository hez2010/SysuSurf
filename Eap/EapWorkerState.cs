// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

namespace SysuSurf.Eap
{
    public class EapWorkerState
    {
        public int FailureCount { get; set; }
        public bool Succeeded { get; set; }
        public byte LastId { get; set; }
    }
}