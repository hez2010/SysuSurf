// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpPcap.LibPcap;
using SysuSurf.Eap;
using SysuSurf.Options;
using static SysuSurf.Utils.AssertHelpers;

namespace SysuSurf
{
    class Program
    {
        static TOption LoadOptionalOption<TOption>(JsonElement element, string propertyName, Predicate<int> validate)
            where TOption : struct, Enum
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.TryGetInt32(out var value) &&
                validate(value))
            {
                Assert(Unsafe.SizeOf<TOption>() == sizeof(int));
                return Unsafe.As<int, TOption>(ref value);
            }

            return default;
        }

        static async ValueTask<SurfOptions> LoadOptions(string fileName)
        {
            await using var fileStream = new FileStream(fileName, FileMode.Open);

            var json = JsonDocument.Parse(fileStream,
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            if (json is null) throw new InvalidDataException("Invalid options.");

            if (!(json.RootElement.TryGetProperty("Type", out var typeProperty) && typeProperty.TryGetInt32(out var type)))
            {
                throw new InvalidDataException("Invalid options [missing type].");
            }

            if (json.RootElement.TryGetProperty("UserName", out var userNameProperty) &&
                json.RootElement.TryGetProperty("Password", out var passwordProperty) &&
                json.RootElement.TryGetProperty("DeviceName", out var deviceNameProperty) &&
                (userNameProperty.ToString(), passwordProperty.ToString(), deviceNameProperty.ToString())
                    is (string userName, string password, string deviceName))
            {

                return type switch
                {
                    0 => new H3COptions(
                            userName,
                            password,
                            deviceName,
                            LoadOptionalOption<H3CMd5ChallengeMethod>(json.RootElement, "Md5Method", i => i is 0 or 1)),
                    1 => new RuijieOptions(
                            userName,
                            password,
                            deviceName,
                            LoadOptionalOption<RuijieGroupcastMode>(json.RootElement, "GroupcastMode", i => i is >= 0 and < 3),
                            LoadOptionalOption<RuijieDhcpMode>(json.RootElement, "DhcpMode", i => i is >= 0 and < 4)),
                    _ => throw new NotSupportedException("Not supported authentication type.")
                };
            }
            else
            {
                throw new InvalidDataException("Invalid options [missing fields].");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args, SurfOptions options) =>
            Host.CreateDefaultBuilder(args)
#if WINDOWS
                .UseWindowsService()
#elif LINUX
                .UseSystemd()
#endif
                .ConfigureServices(services =>
                {
                    services.AddSingleton(options);

                    if (options is H3COptions)
                    {
                        services.AddHostedService<H3CEapAuth>();
                    }
                    else
                    {
                        services.AddHostedService<RuijieEapAuth>();
                    }
                });


        static void PrintUsage(bool invalid = false)
        {
            if (invalid)
            {
                Console.WriteLine("Invalid arguments.");
                Console.WriteLine();
            }

            Console.WriteLine("Usage: SysuSurf [command] [options...]");
            Console.WriteLine("Commands:");
            Console.WriteLine("  ls");
            Console.WriteLine("      List all available network devices.");
            Console.WriteLine("  auth [config.json file]");
            Console.WriteLine("      Authenticate network with specified config.");
            Console.WriteLine("  help");
            Console.WriteLine("      Show help message.");
            Console.WriteLine("  version");
            Console.WriteLine("      Show SysuSurf version.");
        }

        static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage(false);
                return 1;
            }

            switch (args[0])
            {
                case "ls":
                    var devices = LibPcapLiveDeviceList.Instance;
                    Console.WriteLine(devices.Count > 0 ? $"Available devices: \nDevice Name (Device Description) \n{devices.Select(i => $"{i.Name} ({i.Description})").Aggregate((a, n) => $"{a}\n{n}")}" : "No available network devices.");
                    break;
                case "auth":
                    if (args.Length < 2)
                    {
                        PrintUsage(true);
                        var options = await LoadOptions(args[1]);
                        await CreateHostBuilder(args[1..], options).Build().RunAsync();
                        return 1;
                    }
                    break;
                case "help":
                    PrintUsage(false);
                    break;
                case "version":
                    Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
                    break;
                default:
                    PrintUsage(true);
                    return 1;
            }
            return 0;
        }
    }
}
