using System;
using System.Net.Http;

namespace PCL.Core.Net;

public class HttpResponseHandler(HttpResponseMessage responseMessage) : IDisposable
{
    public bool IsSuccess { get; } = responseMessage.IsSuccessStatusCode;

    public void Dispose()
    {
        responseMessage.Dispose();
        GC.SuppressFinalize(this);
    }
}