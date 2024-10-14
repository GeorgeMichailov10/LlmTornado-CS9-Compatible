﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado
{

/// <summary>
///     A base object for any OpenAI API endpoint, encompassing common functionality
/// </summary>
public abstract class EndpointBase
{
    private static string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
    internal static readonly JsonSerializerSettings NullSettings = new() { NullValueHandling = NullValueHandling.Ignore };
    private static TimeSpan endpointTimeout = TimeSpan.FromSeconds(600);
    private static readonly Lazy<Dictionary<LLmProviders, Lazy<HttpClient>>> EndpointClients = new Lazy<Dictionary<LLmProviders, Lazy<HttpClient>>>(() =>
    {
        Dictionary<LLmProviders, Lazy<HttpClient>> dict = new Dictionary<LLmProviders, Lazy<HttpClient>>((int)LLmProviders.Length + 1);

        foreach (LLmProviders provider in Enum.GetValues<LLmProviders>())
        {
            dict.Add(provider, new Lazy<HttpClient>(() =>
            {
                HttpClient client = TornadoConfig.CreateClient is null ? new HttpClient(new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = 10000,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(3)
                })
                {
                    Timeout = endpointTimeout,
                    DefaultRequestVersion = HttpVersion.Version20
                } : TornadoConfig.CreateClient.Invoke(provider);

                return client;
            }));
        }
        
        return dict;
    });

    /// <summary>
    ///     Constructor of the api endpoint base, to be called from the contructor of any devived classes.  Rather than
    ///     instantiating any endpoint yourself, access it through an instance of <see cref="TornadoApi" />.
    /// </summary>
    /// <param name="api"></param>
    internal EndpointBase(TornadoApi api)
    {
        Api = api;
    }

    internal string GetUrl(IEndpointProvider provider, string? url = null)
    {
        return provider.ApiUrl(Endpoint, url);
    }

    /// <summary>
    ///     The internal reference to the API, mostly used for authentication
    /// </summary>
    internal TornadoApi Api { get; }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  Must be overriden in a derived class.
    /// </summary>
    protected abstract CapabilityEndpoints Endpoint { get; }

    /// <summary>
    ///     Gets the timeout for all http requests
    /// </summary>
    /// <returns></returns>
    public static int GetRequestsTimeout()
    {
        return (int)endpointTimeout.TotalSeconds;
    }

    /// <summary>
    ///     Sets the timeout for all http requests.
    ///     This is not thread safe! Use only in app startup logic.
    /// </summary>
    /// <returns></returns>
    public static void SetRequestsTimeout(int seconds)
    {
        foreach (KeyValuePair<LLmProviders, Lazy<HttpClient>> x in EndpointClients.Value)
        {
            x.Value.Value.Timeout = TimeSpan.FromSeconds(seconds);
        }
        
        endpointTimeout = TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    ///     Sets the user agent header used in all http requests
    /// </summary>
    /// <param name="ua">User agent</param>
    public static void SetUserAgent(string ua)
    {
        userAgent = ua;
    }

    /// <summary>
    ///     Gets the user agent header used in all http requests
    /// </summary>
    public static string GetUserAgent()
    {
        return userAgent;
    }

    /// <summary>
    ///     Gets an HTTPClient with the appropriate authorization and other headers set
    /// </summary>
    /// <returns>The fully initialized HttpClient</returns>
    /// <exception cref="AuthenticationException">
    ///     Thrown if there is no valid authentication.  Please refer to
    ///     <see href="https://github.com/OkGoDoIt/OpenAI-API-dotnet#authentication" /> for details.
    /// </exception>
    private static HttpClient GetClient(LLmProviders provider)
    {
        return EndpointClients.Value[provider].Value;
    }

    /// <summary>
    ///     Formats a human-readable error message relating to calling the API and parsing the response
    /// </summary>
    /// <param name="resultAsString">The full content returned in the http response</param>
    /// <param name="response">The http response object itself</param>
    /// <param name="name">The name of the endpoint being used</param>
    /// <param name="description">Additional details about the endpoint of this request (optional)</param>
    /// <param name="input">Additional details about the endpoint of this request (optional)</param>
    /// <returns>A human-readable string error message.</returns>
    private static string GetErrorMessage(string? resultAsString, HttpResponseMessage response, string name, string description, HttpCallRequest input)
    {
        return $"Error at {name} ({description}) with HTTP status code: {response.StatusCode}. Content: {resultAsString ?? "<no content>"}. Request: {JsonConvert.SerializeObject(input.Headers)}";
    }

    private static void SetRequestContent(HttpRequestMessage msg, object? payload)
    {
        if (payload is not null)
        {
            switch (payload)
            {
                case HttpContent hData:
                {
                    msg.Content = hData;
                    break;
                }
                case string str:
                {
                    StringContent stringContent = new(str, Encoding.UTF8, "application/json");
                    msg.Content = stringContent;
                    break;
                }
                default:
                {
                    string jsonContent = JsonConvert.SerializeObject(payload, NullSettings);
                    StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                    msg.Content = stringContent;
                    break;
                }
            }
        }
    }

    /// <summary>
    ///     Sends an HTTP request and returns the response.  Does not do any parsing, but does do error handling.
    /// </summary>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="verb">
    ///     (optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get" />".  If omitted, then
    ///     "GET" is assumed.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="streaming">
    ///     (optional) If true, streams the response. Otherwise waits for the entire response before
    ///     returning.
    /// </param>
    /// <param name="ct">(optional) A cancellation token.</param>
    /// <param name="provider"></param>
    /// <param name="endpoint"></param>
    /// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
    /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
    private async Task<HttpResponseMessage> HttpRequestRaw(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, bool streaming = false, CancellationToken? ct = null)
    {
        url ??= url?.StartsWith("http") ?? false ? url : provider.ApiUrl(endpoint, url);
        verb ??= HttpMethod.Get;

        HttpClient client = GetClient(provider.Provider);
        using HttpRequestMessage req = provider.OutboundMessage(url, verb, postData, streaming);

        SetRequestContent(req, postData);
        
        try
        {
            HttpResponseMessage response = await client.SendAsync(req, streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct ?? CancellationToken.None).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            string resultAsString;

            try
            {
                resultAsString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                resultAsString = $"Additionally, the following error was thrown when attempting to read the response content: {e}";
            }

            ProviderAuthentication? auth = Api.GetProvider(provider.Provider).Auth;

            if (auth is not null)
            {
                if (auth.ApiKey is not null)
                {
                    resultAsString = resultAsString.Replace(auth.ApiKey, "[API KEY REDACTED FOR SECURITY]");
                }

                if (auth.Organization is not null)
                {
                    resultAsString = resultAsString.Replace(auth.Organization, "[ORGANIZATION REDACTED FOR SECURITY]");
                }
            }
            
            HttpCallRequest httpRequest = new HttpCallRequest
            {
                Method = req.Method,
                Url = req.RequestUri?.AbsolutePath ?? string.Empty,
                Headers = req.Headers.ToDictionary(pair => pair.Key, pair => pair.Value)
            };

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AuthenticationException($"The API provider rejected your authorization, most likely due to an invalid API Key. Check your API key and/or other authentication requirements of the service. Full API response follows: {resultAsString}"),
                HttpStatusCode.InternalServerError => new HttpRequestException($"The API provider had an internal server error. Please retry your request. Server response: {GetErrorMessage(resultAsString, response, Endpoint.ToString(), url, httpRequest)}"),
                _ => new HttpRequestException(GetErrorMessage(resultAsString, response, Endpoint.ToString(), url, httpRequest))
            };
        }
        catch (Exception e)
        {
            throw e switch
            {
                HttpRequestException => new HttpRequestException($"An error occured when trying to contact {verb.Method} {url}. Message: {e.Message}. {(e.InnerException is not null ? $" Inner exception: {e.InnerException.Message}" : string.Empty)}"),
                TaskCanceledException => new TaskCanceledException($"An error occured when trying to contact {verb.Method} {url}. The operation timed out. Consider increasing the timeout in TornadoApi config. {(e.InnerException is not null ? $" Inner exception: {e.InnerException.Message}" : string.Empty)}"),
                _ => new Exception($"An error occured when trying to contact {verb.Method} {url}. Exception type: {e.GetType()}. Message: {e.Message}. {(e.InnerException is not null ? $" Inner exception: {e.InnerException.Message}" : string.Empty)}")
            };
        }
    }

    private async Task<RestDataOrException<HttpResponseMessage>> HttpRequestRawWithAllCodes(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? content = null, bool streaming = false, CancellationToken? ct = null)
    {
        url ??= url?.StartsWith("http") ?? false ? url : provider.ApiUrl(endpoint, url);
        verb ??= HttpMethod.Get;

        HttpClient client = GetClient(provider.Provider);
        using HttpRequestMessage req = provider.OutboundMessage(url, verb, content, streaming);
        
        SetRequestContent(req, content);
        
        try
        {
            HttpResponseMessage result = await client.SendAsync(req, streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct ?? CancellationToken.None).ConfigureAwait(false);
            
            if (result.IsSuccessStatusCode)
            {
                return new RestDataOrException<HttpResponseMessage>(result, req);
            }

            string resultAsString;

            try
            {
                resultAsString = await result.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                resultAsString = $"Additionally, the following error was thrown when attempting to read the response content: {e}";
            }

            ProviderAuthentication? auth = Api.GetProvider(provider.Provider).Auth;

            if (auth is not null)
            {
                if (auth.ApiKey is not null)
                {
                    resultAsString = resultAsString.Replace(auth.ApiKey, "[API KEY REDACTED FOR SECURITY]");
                }

                if (auth.Organization is not null)
                {
                    resultAsString = resultAsString.Replace(auth.Organization, "[ORGANIZATION REDACTED FOR SECURITY]");
                }
            }
            
            return new RestDataOrException<HttpResponseMessage>(new Exception($"Http call failed. Code: {(int)result.StatusCode}, Message: {resultAsString}."), req);
        }
        catch (Exception e)
        {
            return new RestDataOrException<HttpResponseMessage>(e, req);
        }
    }

    /// <summary>
    ///     Sends an HTTP Get request and return the string content of the response without parsing, and does error handling.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="endpoint">(</param>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="ct">A cancellation token</param>
    /// <returns>The text string of the response, which is confirmed to be successful.</returns>
    /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
    internal async Task<string> HttpGetContent(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, CancellationToken? ct = null)
    {
        using HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, ct: ct).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Sends an HTTP Request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="provider">A concrete provider responsible for handling the request.</param>
    /// <param name="endpoint">Which endpoint will be used. Used to resolve routing in cases where the full url is not provided.</param>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request. If omitted, then
    ///     <see cref="url" /> will be used.
    /// </param>
    /// <param name="verb">
    ///     (optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get" />".  If omitted, then
    ///     "GET" is assumed.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="ct">(optional) A cancellation token</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    private async Task<T?> HttpRequest<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken? ct = null) where T : ApiResultBase
    {
        using HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, verb, postData, false, ct).ConfigureAwait(false);
        string resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        T? res = provider.InboundMessage<T>(resultAsString, postData?.ToString());
        
        if (res is not null)
        {
            provider.ParseInboundHeaders(res, response);
        }

        return res;
    }

    private async Task<HttpCallResult<T>> HttpRequestRaw<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken? ct = null)
    {
        RestDataOrException<HttpResponseMessage> response = await HttpRequestRawWithAllCodes(provider, endpoint, url, verb, postData, false, ct).ConfigureAwait(false);

        if (response.Exception is not null)
        {
            return new HttpCallResult<T>(HttpStatusCode.ServiceUnavailable, null, default, false, response)
            {
                Exception = response.Exception
            };
        }

        if (response.Data is null)
        {
            return new HttpCallResult<T>(response.Data?.StatusCode ?? HttpStatusCode.Found, null, default, false, response)
            {
                Exception = new Exception("Data is null")
            };
        }
        
        string resultAsString = await response.Data.Content.ReadAsStringAsync().ConfigureAwait(false);
        HttpCallResult<T> result = new(response.Data.StatusCode, resultAsString, default, response.Data.IsSuccessStatusCode, response);

        if (response.Data.IsSuccessStatusCode)
        {
            result.Data = provider.InboundMessage<T>(resultAsString, postData?.ToString());
        }

        response.Data?.Dispose();
        return result;
    }

    private async Task<StreamResponse?> HttpRequestStream(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken ct = default)
    {
        HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, verb, postData, ct: ct).ConfigureAwait(false);
        Stream resultAsStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        StreamResponse res = new()
        {
            Headers = new ApiResultBase(),
            Stream = resultAsStream,
            Response = response
        };
        
        provider.ParseInboundHeaders(res.Headers, response);
        return res;
    }

    /// <summary>
    ///     Sends an HTTP Get request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="provider"></param>
    /// <param name="endpoint">(</param>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="ct">A cancellation token</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<T?> HttpGet<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, CancellationToken? ct = null) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Get, ct: ct);
    }

    internal Task<HttpCallResult<T>> HttpGetRaw<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, CancellationToken? ct = null, bool allowNon200Codes = false)
    {
        return HttpRequestRaw<T>(provider, endpoint, url, HttpMethod.Get, ct: ct);
    }

    internal Task<T?> HttpPost1<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP Post request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="provider"></param>
    /// <param name="endpoint">(</param>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="ct">(optional) Cancellation token.</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<HttpCallResult<T>> HttpPost<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default) where T : ApiResultBase
    {
        return HttpRequestRaw<T>(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    internal Task<HttpCallResult<T>> HttpPostRaw<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default, bool allowNon200Codes = false)
    {
        return HttpRequestRaw<T>(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    internal Task<StreamResponse?> HttpPostStream(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken ct = default)
    {
        return HttpRequestStream(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP Delete request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="provider"></param>
    /// <param name="endpoint">(</param>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<T?> HttpDelete<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Delete, postData, ct);
    }

    internal Task<HttpCallResult<T>> HttpAtomic<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, HttpMethod method, string? url = null, object? postData = null, CancellationToken? ct = default, bool allowNon200Codes = false)
    {
        return HttpRequestRaw<T>(provider, endpoint, url, method, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP Put request and does initial parsing
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="endpoint">(</param>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<T?> HttpPut<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken ct = default) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Put, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP request and handles a streaming response.  Does basic line splitting and error handling.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="endpoint">(</param>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request. If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="verb">
    ///     (optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get" />".  If omitted, then
    ///     "GET" is assumed.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="requestRef">(optional) A container for JSON-encoded outbound request.</param>
    /// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
    /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
    protected async IAsyncEnumerable<T> HttpStreamingRequest<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, Ref<string>? requestRef = null, CancellationToken token = default) where T : ApiResultBase
    {
        using HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, verb, postData, true).ConfigureAwait(false);
        await using Stream stream = await response.Content.ReadAsStreamAsync(token);
        using StreamReader reader = new(stream);

        await foreach (T? x in provider.InboundStream<T>(reader).WithCancellation(token))
        {
            if (x is null)
            {
                continue;
            }

            x.Provider = provider;
            yield return x;
        }
    }
    
    protected async Task<TornadoStreamRequest> HttpStreamingRequestData(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken token = default)
    {
        RestDataOrException<HttpResponseMessage> response = await HttpRequestRawWithAllCodes(provider, endpoint, url, verb, postData, true, token).ConfigureAwait(false);

        if (response.Exception is not null || response.Data is null)
        {
            return new TornadoStreamRequest
            {
                Exception = response.Exception ?? new Exception("HttpStreamingRequestData returned no data and no exception"),
                CallRequest = response.HttpRequest,
                CallResponse = response.HttpResult
            };
        }
        
        Stream stream = await response.Data.Content.ReadAsStreamAsync(token);
        StreamReader reader = new(stream);

        return new TornadoStreamRequest
        {
            CallRequest = response.HttpRequest,
            Response = response.Data,
            Stream = stream,
            StreamReader = reader,
            CallResponse = response.HttpResult
        };
    }
}
}