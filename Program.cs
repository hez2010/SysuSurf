using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Node;
using System.Threading;
using System.Threading.Tasks;
using SysuH3C.Eap;

namespace SysuH3C
{
    class Program
    {
        public readonly static SemaphoreSlim Semaphore = new(0, 1);
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SysuH3c [config_file]");
                return;
            }

            await using (var fileStream = new FileStream(args[0], FileMode.Open))
            {
                var json = JsonDocument.Parse(fileStream,
                    new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                if (json is null) throw new InvalidDataException("Invalid Config.");

                if (json.RootElement.TryGetProperty("UserName", out var userNameProperty) &&
                    json.RootElement.TryGetProperty("Password", out var passwordProperty) &&
                    json.RootElement.TryGetProperty("DeviceName", out var deviceNameProperty) &&
                    (userNameProperty.ToString(), passwordProperty.ToString(), deviceNameProperty.ToString())
                        is (string userName, string password, string deviceName))
                {
                    var options = new EapOptions(userName, password, deviceName);

                    if (json.RootElement.TryGetProperty("Md5Method", out var md5MethodProp) &&
                        md5MethodProp.TryGetInt32(out var md5Method) &&
                        md5Method is 0 or 1)
                    {
                        options = options with { Md5Method = (EapMd5AuthMethod)md5Method };
                    }

                    var auth = new EapAuth(options);
                    Console.CancelKeyPress += (_, _) => auth.LogOff();
                }
                else
                {
                    throw new InvalidDataException("Invalid Config.");
                }
            }

            await Semaphore.WaitAsync();
        }
    }
}
