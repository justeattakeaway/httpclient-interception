// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace JustEat.HttpClientInterception.Bundles;

/// <summary>
/// A class representing a serialized HTTP request bundle item. This class cannot be inherited.
/// </summary>
#pragma warning disable CA1812
internal sealed class Bundle
#pragma warning restore CA1812
{
    /// <summary>
    /// Gets or sets the bundle's Id.
    /// </summary>
    [JsonPropertyName("id")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the bundle's comment.
    /// </summary>
    [JsonPropertyName("comment")]
#if NET6_0_OR_GREATER
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the bundle version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the items in the bundle.
    /// </summary>
    [JsonPropertyName("items")]
    public IList<BundleItem>? Items { get; set; }
}
