using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Serilog.RequestAndResponseLogging
{
    /// <summary>
    /// Extends <see cref="IApplicationBuilder"/> with methods for configuring Serilog features.
    /// </summary>
    public static class SerilogApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for streamlined request logging. Instead of writing HTTP request information
        /// like method, path, timing, status code and exception details
        /// in several events, this middleware collects information during the request (including from
        /// <see cref="IDiagnosticContext"/>), and writes a single event at request completion. Add this
        /// in <c>Startup.cs</c> before any handlers whose activities should be logged.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="messageTemplate">The message template to use when logging request completion
        /// events. The default is
        /// <c>"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"</c>. The
        /// template can contain any of the placeholders from the default template, names of properties
        /// added by ASP.NET Core, and names of properties added to the <see cref="IDiagnosticContext"/>.
        /// </param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseSerilogRequestAndResponseLogging(this IApplicationBuilder app, string messageTemplate) => app.UseSerilogRequestAndResponseLogging(opts => opts.MessageTemplate = messageTemplate);

        /// <summary>
        /// Adds middleware for streamlined request logging. Instead of writing HTTP request information
        /// like method, path, timing, status code and exception details
        /// in several events, this middleware collects information during the request (including from
        /// <see cref="IDiagnosticContext"/>), and writes a single event at request completion. Add this
        /// in <c>Startup.cs</c> before any handlers whose activities should be logged.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="configureOptions">A <see cref="System.Action{T}" /> to configure the provided <see cref="RequestAndResponseLoggingOptions" />.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseSerilogRequestAndResponseLogging(this IApplicationBuilder app, Action<RequestAndResponseLoggingOptions> configureOptions = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var opts = app.ApplicationServices.GetService<IOptions<RequestAndResponseLoggingOptions>>()?.Value ?? new RequestAndResponseLoggingOptions();

            configureOptions?.Invoke(opts);

            if (opts.MessageTemplate == null)
                throw new ArgumentException($"{nameof(opts.MessageTemplate)} cannot be null.");
            if (opts.GetLevel == null)
                throw new ArgumentException($"{nameof(opts.GetLevel)} cannot be null.");

            return app.UseMiddleware<RequestAndResponseLoggingMiddleware>(opts);
        }
    }
}
