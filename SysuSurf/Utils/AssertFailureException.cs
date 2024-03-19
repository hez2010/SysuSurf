// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;

namespace SysuSurf.Utils
{
    internal class AssertFailureException : Exception
    {
        internal AssertFailureException(string? message) : base(message) { }
    }
}