using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Shirhatti.WebLogger.Test
{
    public class TestFixture<TStartup> : IDisposable where TStartup : class
    {
        private readonly TestServer _server;
        private readonly IHost _host;

        public TestFixture()
        {
            var builder = new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders()
                           .AddWebLogger();
                })
                .ConfigureWebHostDefaults(webHost =>
                {
                    webHost
                        .UseTestServer()
                        .UseStartup<TStartup>();
                });
            _host = builder.Start();
            _server = _host.GetTestServer();
            
            Logger = _host.Services.GetRequiredService<ILogger<TestFixture<TStartup>>>();
            
            Client = new HttpClient();
            Client.BaseAddress = new Uri("http://localhost");
        }

        public ILogger Logger { get; }
        public HttpClient Client { get; }

        public void Dispose()
        {
            _host.Dispose();
            Client.Dispose();
        }

        public static IDisposable GetTestContext()
        {
            return new TestFixture<TStartup>();
        }
    }
}
