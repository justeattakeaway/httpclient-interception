// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustEat.HttpClientInterception.Bundles;

/// <summary>
/// A class representing a serialized HTTP request bundle. This class cannot be inherited.
/// </summary>
#pragma warning disable CA1812
internal sealed class BundleItem
#pragma warning restore CA1812
{
    /// <summary>
    /// Gets or sets the optional Id of the item.
    /// </summary>
    [JsonPropertyName("id")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the optional comment for the item.
    /// </summary>
    [JsonPropertyName("comment")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the optional HTTP version for the item.
    /// </summary>
    [JsonPropertyName("version")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the default HTTP method for the item.
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the request URI for the item.
    /// </summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the optional priority for the item.
    /// </summary>
    [JsonPropertyName("priority")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public int? Priority { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code for the response for item.
    /// </summary>
    [JsonPropertyName("status")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the optional request headers for the item.
    /// </summary>
    [JsonPropertyName("requestHeaders")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public IDictionary<string, ICollection<string>>? RequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets the optional response headers for the item.
    /// </summary>
    [JsonPropertyName("responseHeaders")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public IDictionary<string, ICollection<string>>? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets or sets the optional content headers for the item.
    /// </summary>
    [JsonPropertyName("contentHeaders")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public IDictionary<string, ICollection<string>>? ContentHeaders { get; set; }

    /// <summary>
    /// Gets or sets the optional content format of the item.
    /// </summary>
    [JsonPropertyName("contentFormat")]
    public string? ContentFormat { get; set; }

    /// <summary>
    /// Gets or sets the content of the item as JSON.
    /// </summary>
    [JsonPropertyName("contentJson")]
    public JsonElement ContentJson { get; set; }

    /// <summary>
    /// Gets or sets the content of the item as a string.
    /// </summary>
    [JsonPropertyName("contentString")]
    public string? ContentString { get; set; }

    /// <summary>
    /// Gets or sets the optional templating values.
    /// </summary>
    [JsonPropertyName("templateValues")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public IDictionary<string, string>? TemplateValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore the URI's path.
    /// </summary>
    [JsonPropertyName("ignorePath")]
    public bool IgnorePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore the URI's query string.
    /// </summary>
    [JsonPropertyName("ignoreQuery")]
    public bool IgnoreQuery { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip the item.
    /// </summary>
    [JsonPropertyName("skip")]
    public bool Skip { get; set; }
}
