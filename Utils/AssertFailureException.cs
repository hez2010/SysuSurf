using System;

namespace SysuSurf.Utils
{
    public class AssertFailureException : Exception
    {
        public AssertFailureException(string? message) : base(message) { }
    }
}