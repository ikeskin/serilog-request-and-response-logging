using System;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace Serilog.RequestAndResponseLogging
{
    public class RequestAndResponseLoggingOptions
    {

        const string DefaultRequestCompletionMessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        static LogEventLevel DefaultGetLevel(HttpContext ctx, double _, Exception ex) => ex != null ? LogEventLevel.Error : ctx.Response.StatusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;

        /// <summary>
        /// Gets or sets the message template. The default value is
        /// <c>"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"</c>. The
        /// template can contain any of the placeholders from the default template, names of properties
        /// added by ASP.NET Core, and names of properties added to the <see cref="IDiagnosticContext"/>.
        /// </summary>
        /// <value>
        /// The message template.
        /// </value>
        public string MessageTemplate { get; set; }

        /// <summary>
        /// A function returning the <see cref="LogEventLevel"/> based on the <see cref="HttpContext"/>, the number of
        /// elapsed milliseconds required for handling the request, and an <see cref="Exception" /> if one was thrown.
        /// The default behavior returns <see cref="LogEventLevel.Error"/> when the response status code is greater than 499 or if the
        /// <see cref="Exception"/> is not null.
        /// </summary>
        /// <value>
        /// A function returning the <see cref="LogEventLevel"/>.
        /// </value>
        public Func<HttpContext, double, Exception, LogEventLevel> GetLevel { get; set; }

        /// <summary>
        /// A callback that can be used to set additional properties on the request completion event.
        /// </summary>
        public Action<IDiagnosticContext, HttpContext> EnrichDiagnosticContext { get; set; }

        /// <summary>
        /// The logger through which request completion events will be logged. The default is to use the
        /// static <see cref="Log"/> class.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] IgnoredPaths { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public HeaderLoggingOption HeaderOptions { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RequestAndResponseLoggingOptions()
        {
            GetLevel = DefaultGetLevel;
            MessageTemplate = DefaultRequestCompletionMessageTemplate;
            IgnoredPaths = Array.Empty<string>();

        }
    }
}
