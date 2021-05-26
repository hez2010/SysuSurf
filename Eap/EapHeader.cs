using System.Runtime.InteropServices;

namespace SysuH3c.Eap
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EapHeader
    {
        public EapCode Code;
        public byte Identifier;
        public ushort Length;
    }
}