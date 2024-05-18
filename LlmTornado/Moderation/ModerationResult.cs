﻿using System.Collections.Generic;
using System.Linq;
using Argon;

namespace LlmTornado.Moderation;

/// <summary>
///     Represents a moderation result returned by the Moderations API
/// </summary>
public class ModerationResult : ApiResultBase
{
	/// <summary>
	///     List of results returned from the Moderations API request
	/// </summary>
	[JsonProperty("results")]
    public List<Result> Results { get; set; }

	/// <summary>
	///     The unique identifier associated with a moderation request
	///     Consists of the prefix "modr-" followed by a randomly generated alphanumeric string
	/// </summary>
	[JsonProperty("id")]
    public string Id { get; set; }

	/// <summary>
	///     Convenience function to return the highest confidence category for which the content was flagged, or null if no
	///     content flags
	/// </summary>
	/// <returns>the highest confidence category for which the content was flagged, or null if no content flags</returns>
	public override string ToString()
    {
        return Results?.First()?.MainContentFlag;
    }
}

/// <summary>
///     The result generated by the Moderations API request
/// </summary>
public class Result
{
	/// <summary>
	///     A series of categories that the content could be flagged for.  Values are bool's, indicating if the txt is flagged
	///     in that category
	/// </summary>
	[JsonProperty("categories")]
    public IDictionary<string, bool> Categories { get; set; }

	/// <summary>
	///     Confidence scores for the different category flags. Values are between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("category_scores")]
    public IDictionary<string, double> CategoryScores { get; set; }

	/// <summary>
	///     True if the text was flagged in any of the categories
	/// </summary>
	[JsonProperty("flagged")]
    public bool Flagged { get; set; }

	/// <summary>
	///     Returns a list of all categories for which the content was flagged, sorted from highest confidence to lowest
	/// </summary>
	public IList<string> FlaggedCategories
    {
        get { return Categories.Where(kv => kv.Value).OrderByDescending(kv => CategoryScores?[kv.Key]).Select(kv => kv.Key).ToList(); }
    }

	/// <summary>
	///     Returns the highest confidence category for which the content was flagged, or null if no content flags
	/// </summary>
	public string MainContentFlag => FlaggedCategories.FirstOrDefault();

	/// <summary>
	///     Returns the highest confidence flag score across all categories
	/// </summary>
	public double HighestFlagScore
    {
        get { return CategoryScores.OrderByDescending(kv => kv.Value).First().Value; }
    }
}

/// <summary>
///     Series of boolean values indiciating what the text is flagged for
/// </summary>
public class Categories
{
	/// <summary>
	///     If the text contains hate speech
	/// </summary>
	[JsonProperty("hate")]
    public bool Hate { get; set; }

	/// <summary>
	///     If the text contains hate / threatening speech
	/// </summary>
	[JsonProperty("hate/threatening")]
    public bool HateThreatening { get; set; }

	/// <summary>
	///     If the text contains content about self-harm
	/// </summary>
	[JsonProperty("self-harm")]
    public bool SelfHarm { get; set; }

	/// <summary>
	///     If the text contains sexual content
	/// </summary>
	[JsonProperty("sexual")]
    public bool Sexual { get; set; }

	/// <summary>
	///     If the text contains sexual content featuring minors
	/// </summary>
	[JsonProperty("sexual/minors")]
    public bool SexualMinors { get; set; }

	/// <summary>
	///     If the text contains violent content
	/// </summary>
	[JsonProperty("violence")]
    public bool Violence { get; set; }

	/// <summary>
	///     If the text contains violent and graphic content
	/// </summary>
	[JsonProperty("violence/graphic")]
    public bool ViolenceGraphic { get; set; }
}

/// <summary>
///     Confidence scores for the different category flags
/// </summary>
public class CategoryScores
{
	/// <summary>
	///     Confidence score indicating "hate" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("hate")]
    public double Hate { get; set; }

	/// <summary>
	///     Confidence score indicating "hate/threatening" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("hate/threatening")]
    public double HateThreatening { get; set; }

	/// <summary>
	///     Confidence score indicating "self-harm" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("self-harm")]
    public double SelfHarm { get; set; }

	/// <summary>
	///     Confidence score indicating "sexual" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("sexual")]
    public double Sexual { get; set; }

	/// <summary>
	///     Confidence score indicating "sexual/minors" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("sexual/minors")]
    public double SexualMinors { get; set; }

	/// <summary>
	///     Confidence score indicating "violence" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("violence")]
    public double Violence { get; set; }

	/// <summary>
	///     Confidence score indicating "violence/graphic" content is detected in the text
	///     A value between 0 and 1, where 0 indicates low confidence
	/// </summary>
	[JsonProperty("violence/graphic")]
    public double ViolenceGraphic { get; set; }
}