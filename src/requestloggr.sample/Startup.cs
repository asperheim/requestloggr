using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace requestloggr.sample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            //loggerFactory.UseRequestLoggr(app);

            app.UseMvc();
            app.UseRequestLoggr();
        }
    }

    public static class RequestLoggrLoggerFactory
    {
        public static ILoggerFactory UseRequestLoggr(this ILoggerFactory loggerFactory, IApplicationBuilder app)
        {
            app.UseRequestLoggr();
            loggerFactory.AddProvider(new RequestLoggrProvider());

            return loggerFactory;
        }
    }

    public static class RequestLogger
    {
        public static IApplicationBuilder UseRequestLoggr(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggerMiddlewareDelegate>();
        }
    }

    public class RequestLoggrLogger : ILogger, IDisposable
    {
        private bool[] _enabledLogLevels;


        public RequestLoggrLogger()
        {
            _enabledLogLevels = new bool[Enum.GetNames(typeof(LogLevel)).Length];

            _enabledLogLevels[(int) LogLevel.Debug] = true;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _enabledLogLevels[logLevel.GetHashCode()];
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            // Uh oh..?
            return this;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var kuk = formatter.Invoke(state, exception);

            Debug.WriteLine("Anderslogger: " + kuk);
        }

        public void Dispose()
        {
            //Le disposé must (h)append (h)ere
        }
    }
    public class RequestLoggrProvider : ILoggerProvider, IDisposable
    {
        public static ILoggerProvider RequestLoggr()
        {
            return new RequestLoggrProvider();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RequestLoggrLogger();
        }
    }

    public class RequestLoggerMiddlewareDelegate
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<RequestLoggerMiddlewareDelegate> _logger;

        public RequestLoggerMiddlewareDelegate(RequestDelegate next, ILogger<RequestLoggerMiddlewareDelegate> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {

            _logger.LogInformation("Before the execution!");
            var before = new Stopwatch();
            await _next.Invoke(context);
            var elapsed = before.ElapsedMilliseconds;
            before.Stop();

            _logger.LogInformation("After the execution!");

            _logger.LogInformation("Statuscode: " + context.Response.StatusCode);
            _logger.LogInformation("Executiontime: " + elapsed);
        }
    }


}
