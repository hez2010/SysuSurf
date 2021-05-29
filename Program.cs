// Licensed to hez2010 under one or more agreements.
// hez2010 licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SysuSurf.Eap;
using SysuSurf.Options;

namespace SysuSurf
{
    class Program
    {
        static TOption LoadOptionalOption<TOption>(JsonElement element, string propertyName, Predicate<int> validate) where TOption : struct
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.TryGetInt32(out var value) &&
                validate(value))
            {
                return (TOption)(object)value;
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

        static IHostBuilder CreateHostBuilder(string fileName, SurfOptions options) =>
            Host.CreateDefaultBuilder()
#if WINDOWS
                .UseWindowsService()
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

        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SysuSurf [options_file]");
                return;
            }

            var options = await LoadOptions(args[0]);

            await CreateHostBuilder(args[0], options).Build().RunAsync();
        }
    }
}
