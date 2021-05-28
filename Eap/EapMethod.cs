namespace SysuSurf.Eap
{
    public enum EapMethod : byte
    {
        Identity = 1,
        Notification = 2,
        Nak = 3,
        Md5 = 4,
        Otp = 5,
        Gtc = 6,
        SysuH3c = 7,
        Expanded = 254,
        Experimental = 255
    }
}