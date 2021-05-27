using System;

namespace SysuH3C.Utils
{
    public class AssertFailureException : Exception
    {
        public AssertFailureException(string? message) : base(message) { }
    }
}