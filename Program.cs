// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
            var help = new StringBuilder();
            help.AppendLine("Usage: SysuSurf [command] [options...]");
            help.AppendLine("Commands:");
            help.AppendLine("  ls");
            help.AppendLine("      List all available network devices.");
            help.AppendLine("  auth [config.json file]");
            help.AppendLine("      Authenticate network with specified config.");
            help.AppendLine("  help");
            help.AppendLine("      Show help message.");
            help.AppendLine("  version");
            help.AppendLine("      Show SysuSurf version.");

            if (invalid)
            {
                throw new InvalidOperationException($"Invalid arguments.\n{help}");
            }
            else
            {
                Console.WriteLine(help);
            }
        }

        static async Task<int> Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    PrintUsage(true);
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
                        }
                        else
                        {
                            var options = await LoadOptions(args[1]);
                            await CreateHostBuilder(args[2..], options).Build().RunAsync();
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
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            return 0;
        }
    }
}
