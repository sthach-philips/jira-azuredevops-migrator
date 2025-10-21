using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Migration.Common.Log;
using System;

using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace JiraExport
{
    class Program
    {
        static int Main(string[] args)
        {
            VersionInfo.PrintInfoMessage("Jira Exporter");

            var host = CreateHostBuilder(args).Build();

            try
            {
                var cmd = host.Services.GetRequiredService<JiraCommandLine>();
                return cmd.Run();
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogCritical(ex, "Application stopped due to an unexpected exception");
                Logger.Log(ex, "Application stopped due to an unexpected exception", Migration.Common.Log.LogLevel.Critical);
                return -1;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(MsLogLevel.Information);
                    });
                    
                    services.AddSingleton<JiraCommandLine>(provider => 
                        new JiraCommandLine(args));
                });
    }
}
