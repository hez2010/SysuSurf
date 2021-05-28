// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace SysuSurf.Utils
{

    public static class AssertHelpers
    {
        public static void Assert(bool value, [CallerFilePath] string? filePath = default, [CallerMemberName] string? memberName = default, [CallerLineNumber] int lineNumber = default, [CallerArgumentExpression("expression")] string? expression = default)
        {
#if DEBUG
            if (!value) throw new AssertFailureException($"{filePath}:{lineNumber}: {expression} ({memberName})");
#endif
        }
    }
}