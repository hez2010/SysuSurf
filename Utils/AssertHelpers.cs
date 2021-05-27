using System.Runtime.CompilerServices;

namespace SysuH3C.Utils
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