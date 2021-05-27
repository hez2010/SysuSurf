using System;
using System.IO;
using System.Text.Json;
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
                var config = await JsonSerializer.DeserializeAsync<EapOptions>(fileStream);
                if (config is null) throw new InvalidDataException("Invalid Config.");
                var auth = new EapAuth(config);
                Console.CancelKeyPress += (_, _) => auth.LogOff();
            }

            await Semaphore.WaitAsync();
        }
    }
}
