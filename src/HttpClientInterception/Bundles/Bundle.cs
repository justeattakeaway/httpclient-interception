// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace JustEat.HttpClientInterception.Bundles
{
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
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the bundle's comment.
        /// </summary>
        [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the bundle version.
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets the items in the bundle.
        /// </summary>
        [JsonProperty("items")]
        public IList<BundleItem>? Items { get; set; }
    }
}
