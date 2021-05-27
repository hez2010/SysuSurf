namespace SysuH3C.Eap
{
    public record EapOptions(string UserName, string Password, string DeviceName, EapMd5AuthMethod Md5Method = EapMd5AuthMethod.Xor);
}
