using DeltaKustoLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    /// <summary>
    /// Manages a pool of <see cref="HttpMessageHandler"/>.
    /// </summary>
    /// <remarks>
    /// Recommended pattern (cf
    /// <see cref="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests"/>
    /// ) uses <see cref="IHttpClientFactory" /> but that requires hosts.
    /// </remarks>
    public class SimpleHttpClientFactory
    {
        #region Inner Types
        private class TrackedHttpClient : HttpClient
        {
            private readonly System.Action _onDispose;

            public TrackedHttpClient(HttpMessageHandler handler, System.Action onDispose)
                : base(handler, false)
            {
                _onDispose = onDispose;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    _onDispose();
                }
            }
        }
        #endregion

        private readonly ITracer _tracer;
        private readonly ConcurrentStack<HttpClientHandler> _handlerStack =
            new ConcurrentStack<HttpClientHandler>();

        public SimpleHttpClientFactory(ITracer tracer)
        {
            _tracer = tracer;
        }

        public HttpClient CreateHttpClient()
        {
            HttpClientHandler? handler;

            _handlerStack.TryPop(out handler);

            if (handler == null)
            {
                _tracer.WriteLine(true, "Create HttpClientHandler");
                handler = new HttpClientHandler();
            }
            else
            {
                _tracer.WriteLine(true, "Get cached HttpClientHandler");
            }

            return new TrackedHttpClient(
                handler,
                () =>
                {   //  Push back in the stack when done
                    _handlerStack.Push(handler);
                    _tracer.WriteLine(true, "Recovered HttpClientHandler into cache");
                });
        }
    }
}