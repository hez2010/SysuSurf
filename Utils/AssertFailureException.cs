// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;

namespace SysuSurf.Utils
{
    public class AssertFailureException : Exception
    {
        public AssertFailureException(string? message) : base(message) { }
    }
}