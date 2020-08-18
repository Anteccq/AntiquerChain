using System;
using System.Threading.Tasks;
using AntiquerChain.Formatter;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Utf8Json;
using Utf8Json.Resolvers;

namespace AntiquerChain
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CompositeResolver.RegisterAndSetAsDefault(new IJsonFormatter[] { new IPEndPointFormatter() }, new[] { StandardResolver.Default });
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<AntiquerChain>(args);
        }
    }
}