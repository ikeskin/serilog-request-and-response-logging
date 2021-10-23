using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IO;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Parsing;

namespace Serilog.RequestAndResponseLogging
{
    /// <summary>
    /// 
    /// </summary>
    internal class RequestAndResponseLoggingMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly DiagnosticContext _diagnosticContext;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly MessageTemplate _messageTemplate;
        private readonly Action<IDiagnosticContext, HttpContext> _enrichDiagnosticContext;
        private readonly Func<HttpContext, double, Exception, LogEventLevel> _getLevel;
        private readonly ILogger _logger;
        private readonly RequestAndResponseLoggingOptions _options;




        // ReSharper disable once UseArrayEmptyMethod
        private static readonly LogEventProperty[] NoProperties = new LogEventProperty[0];

        public RequestAndResponseLoggingMiddleware(RequestDelegate next, DiagnosticContext diagnosticContext, RequestAndResponseLoggingOptions options)
        {

            if (options == null) throw new ArgumentNullException(nameof(options));
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _diagnosticContext = diagnosticContext ?? throw new ArgumentNullException(nameof(diagnosticContext));

            _options = options;
            _getLevel = options.GetLevel;
            _enrichDiagnosticContext = options.EnrichDiagnosticContext;
            _messageTemplate = new MessageTemplateParser().Parse(options.MessageTemplate);
            _logger = options.Logger?.ForContext<RequestAndResponseLoggingMiddleware>();
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();






        }


        // ReSharper disable once UnusedMember.Global
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var start = Stopwatch.GetTimestamp();

            var collector = _diagnosticContext.BeginCollection();
            try
            {
                var path = GetPath(httpContext);

                if (_options.IgnoredPaths.Contains(path))
                    return;



                httpContext.Request.EnableBuffering();

                await using var requestStream = _recyclableMemoryStreamManager.GetStream();
                await httpContext.Request.Body.CopyToAsync(requestStream);


                string body = ReadStreamInChunks(requestStream);

                httpContext.Request.Body.Position = 0;


                _diagnosticContext.Set("body", body);

                await _next(httpContext);
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                var statusCode = httpContext.Response.StatusCode;
                LogCompletion(httpContext, collector, statusCode, elapsedMs, null);
            }
            catch (Exception ex)
                // Never caught, because `LogCompletion()` returns false. This ensures e.g. the developer exception page is still
                // shown, although it does also mean we see a duplicate "unhandled exception" event from ASP.NET Core.
                when (LogCompletion(httpContext, collector, 500, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex))
            {
            }
            finally
            {
                collector.Dispose();
            }
        }

        bool LogCompletion(HttpContext httpContext, DiagnosticContextCollector collector, int statusCode, double elapsedMs, Exception ex)
        {
            var logger = _logger ?? Log.ForContext<RequestAndResponseLoggingMiddleware>();
            var level = _getLevel(httpContext, elapsedMs, ex);

            if (!logger.IsEnabled(level)) return false;

            // Enrich diagnostic context
            _enrichDiagnosticContext?.Invoke(_diagnosticContext, httpContext);

            if (!collector.TryComplete(out var collectedProperties))
                collectedProperties = NoProperties;

            // Last-in (correctly) wins...
            var properties = collectedProperties.Concat(new[]
            {
                new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
                new LogEventProperty("RequestPath", new ScalarValue(GetPath(httpContext))),
                new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
                new LogEventProperty("RequestId",new ScalarValue(httpContext.TraceIdentifier)),
            });


            properties = properties.Concat(GetRequestHeaders(httpContext.Request));



            var evt = new LogEvent(DateTimeOffset.Now, level, ex, _messageTemplate, properties);
            logger.Write(evt);

            return false;
        }

        static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }


        static string GetPath(HttpContext httpContext)
        {
            /*
                In some cases, like when running integration tests with WebApplicationFactory<T>
                the RawTarget returns an empty string instead of null, in that case we can't use
                ?? as fallback.
            */
            var requestPath = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
            if (string.IsNullOrEmpty(requestPath))
                requestPath = httpContext.Request.Path.ToString();

            return requestPath;
        }

        private IEnumerable<LogEventProperty> GetRequestHeaders(HttpRequest request)
        {
            foreach (var header in request.Headers)
            {
                var key = _options.HeaderOptions.Prefix + header.Key;

                if (_options.HeaderOptions.Include.Contains(header.Key))
                    yield return new LogEventProperty(key, new ScalarValue(header.Value));

                if (_options.HeaderOptions.Exclude.Contains(header.Key))
                    continue;


                if (_options.HeaderOptions.LogAll)
                    yield return new LogEventProperty(key, new ScalarValue(header.Value));
            }
        }

        private static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;

            stream.Seek(0, SeekOrigin.Begin);

            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);

            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;

            do
            {
                readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);

            return textWriter.ToString();
        }
    }
}
