using System;
using System.Net.Http;
using LlmTornado.Code;

namespace LlmTornado
{

/// <summary>
///     Configuration of Tornado. Use this early, before constructing any <see cref="TornadoApi"/> instances. 
/// </summary>
public static class TornadoConfig
{
    /// <summary>
    ///     Tornado uses one <see cref="HttpClient"/> per <see cref="LLmProviders"/>, if you set this delegate instead of constructing the client
    ///     the default way, you are responsible for constructing and returning your own <see cref="HttpClient"/> instance.
    /// </summary>
    public static Func<LLmProviders, HttpClient>? CreateClient { get; set; }
}
}