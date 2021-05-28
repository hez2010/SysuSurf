namespace SysuSurf.Options
{
    public enum H3CMd5ChallengeMethod { Xor, Md5 }
    public record H3COptions(
        string UserName,
        string Password,
        string DeviceName,
        H3CMd5ChallengeMethod Md5Method) : SurfOptions(UserName, Password, DeviceName);
}
