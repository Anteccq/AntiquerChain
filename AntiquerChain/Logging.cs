using System;
using System.Collections.Generic;
using System.Text;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace AntiquerChain
{
    public static class Logging
    {
        public static ILoggerFactory Factory { get; } = LoggerFactory.Create(builder => builder.ReplaceToSimpleConsole());
        public static ILogger Create<T>() =>
            Factory.CreateLogger<T>();
    }
}
