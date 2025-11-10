using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace PCL.Core.Net;

public class HttpResponseException:HttpRequestException{
    
    public HttpResponseHandler? Response {get;init;}
    
    public Dictionary<string,string> Headers {get;init;} = new();

    public new HttpStatusCode StatusCode {get;init;}

    public new HttpRequestError HttpRequestError {get;init;}

    public HttpResponseException(){}

    public HttpResponseException(string message):base(message){}

    public HttpResponseException(string message,Exception innerException):base(message,innerException){}

    public HttpResponseException(HttpStatusCode statusCode,string message,Exception innerException):base(message,innerException,statusCode){}

    public HttpResponseException(HttpStatusCode statusCode,string message):base(message){ StatusCode = statusCode; }

    public HttpResponseException(HttpRequestError error,string message):base(message){
        HttpRequestError = error;
    }

    public HttpResponseException(HttpResponseHandler response,HttpStatusCode statusCode,string message):base(message){
        StatusCode = statusCode;
        foreach(var kvp in response.GetRaw().Headers){
            Headers.Add(kvp.Key,string.Join(";",kvp.Value));
        }
        Response = response;
    }
}
