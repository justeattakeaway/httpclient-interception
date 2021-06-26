// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JustEat.HttpClientInterception.Bundles
{
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
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the optional comment for the item.
        /// </summary>
        [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the optional HTTP version for the item.
        /// </summary>
        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the default HTTP method for the item.
        /// </summary>
        [JsonProperty("method")]
        public string? Method { get; set; }

        /// <summary>
        /// Gets or sets the request URI for the item.
        /// </summary>
        [JsonProperty("uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets the optional priority for the item.
        /// </summary>
        [JsonProperty("priority", NullValueHandling = NullValueHandling.Ignore)]
        public int? Priority { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code for the response for item.
        /// </summary>
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the optional request headers for the item.
        /// </summary>
        [JsonProperty("requestHeaders", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, ICollection<string>>? RequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets the optional response headers for the item.
        /// </summary>
        [JsonProperty("responseHeaders", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, ICollection<string>>? ResponseHeaders { get; set; }

        /// <summary>
        /// Gets or sets the optional content headers for the item.
        /// </summary>
        [JsonProperty("contentHeaders", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, ICollection<string>>? ContentHeaders { get; set; }

        /// <summary>
        /// Gets or sets the optional content format of the item.
        /// </summary>
        [JsonProperty("contentFormat")]
        public string? ContentFormat { get; set; }

        /// <summary>
        /// Gets or sets the content of the item as JSON.
        /// </summary>
        [JsonProperty("contentJson")]
        public JToken? ContentJson { get; set; }

        /// <summary>
        /// Gets or sets the content of the item as a string.
        /// </summary>
        [JsonProperty("contentString")]
        public string? ContentString { get; set; }

        /// <summary>
        /// Gets or sets the optional templating values.
        /// </summary>
        [JsonProperty("templateValues", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string>? TemplateValues { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the URI's path.
        /// </summary>
        [JsonProperty("ignorePath", NullValueHandling = NullValueHandling.Ignore)]
        public bool IgnorePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the URI's query string.
        /// </summary>
        [JsonProperty("ignoreQuery", NullValueHandling = NullValueHandling.Ignore)]
        public bool IgnoreQuery { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip the item.
        /// </summary>
        [JsonProperty("skip", NullValueHandling = NullValueHandling.Ignore)]
        public bool Skip { get; set; }
    }
}
