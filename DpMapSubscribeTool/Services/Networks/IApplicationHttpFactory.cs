using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Networks
{
    public interface IApplicationHttpFactory
    {
        CookieContainer Cookies { get; set; }

        ValueTask<HttpResponseMessage> SendAsync(string url,Action<HttpRequestMessage> customizeRequestCallback = default, CancellationToken cancellation = default);

        void ResetAll();
    }
}
