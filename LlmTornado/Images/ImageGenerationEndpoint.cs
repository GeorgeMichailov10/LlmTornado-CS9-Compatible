﻿using System.Threading.Tasks;
using LlmTornado.Code;

namespace LlmTornado.Images
{

/// <summary>
///     Given a prompt, the model will generate a new image.
/// </summary>
public class ImageGenerationEndpoint : EndpointBase
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="TornadoApi" /> as <see cref="TornadoApi.ImageGenerations" />.
	/// </summary>
	/// <param name="api"></param>
	internal ImageGenerationEndpoint(TornadoApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "image".
	/// </summary>
	protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.ImageGeneration;
	
	/// <summary>
	///     Ask the API to Creates an image given a prompt.
	/// </summary>
	/// <param name="input">A text description of the desired image(s)</param>
	/// <returns>Asynchronously returns the image result. Look in its <see cref="Data.Url" /> </returns>
	public Task<ImageResult?> CreateImageAsync(string input)
    {
        ImageGenerationRequest req = new(input);
        return CreateImageAsync(req);
    }

	/// <summary>
	///     Ask the API to Creates an image given a prompt.
	/// </summary>
	/// <param name="request">Request to be send</param>
	/// <returns>Asynchronously returns the image result. Look in its <see cref="Data.Url" /> </returns>
	public Task<ImageResult?> CreateImageAsync(ImageGenerationRequest request)
    {
        return HttpPost1<ImageResult>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, postData: request);
    }
}
}