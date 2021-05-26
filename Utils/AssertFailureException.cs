using System;

namespace SysuH3c.Utils
{
    public class AssertFailureException : Exception
    {
        public AssertFailureException(string? message) : base(message) { }
    }
}