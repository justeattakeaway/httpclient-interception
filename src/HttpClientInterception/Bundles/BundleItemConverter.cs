// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace JustEat.HttpClientInterception.Bundles
{
    /// <summary>
    /// A class that creates <see cref="HttpRequestInterceptionBuilder"/> instances from <see cref="BundleItem"/> instances. This class cannot be inherited.
    /// </summary>
    internal static class BundleItemConverter
    {
        public static HttpRequestInterceptionBuilder FromItem(
            BundleItem item,
            IEnumerable<KeyValuePair<string, string>> templateValues)
        {
            // Override the template values in the JSON with any user-specified values
            if (item.TemplateValues?.Count > 0)
            {
                foreach (var pair in templateValues)
                {
                    item.TemplateValues[pair.Key] = pair.Value;
                }
            }

            ValidateItem(item, out Uri? uri, out Version? version);

            var builder = new HttpRequestInterceptionBuilder().ForUri(uri!);

            if (item.Method != null)
            {
                builder.ForMethod(new System.Net.Http.HttpMethod(item.Method));
            }

            if (item.RequestHeaders?.Count > 0)
            {
                builder.ForRequestHeaders(TemplateHeaders(item.RequestHeaders, item.TemplateValues));
            }

            if (version != null)
            {
                builder.WithVersion(version);
            }

            if (!string.IsNullOrEmpty(item.Status))
            {
                if (!Enum.TryParse(item.Status, true, out HttpStatusCode httpStatusCode))
                {
                    throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has an invalid HTTP status code '{item.Status}' configured.");
                }

                builder.WithStatus(httpStatusCode);
            }

            if (item.ResponseHeaders?.Count > 0)
            {
                builder.WithResponseHeaders(TemplateHeaders(item.ResponseHeaders, item.TemplateValues));
            }

            if (item.ContentHeaders?.Count > 0)
            {
                builder.WithContentHeaders(TemplateHeaders(item.ContentHeaders, item.TemplateValues));
            }

            if (item.Priority.HasValue)
            {
                builder.HavingPriority(item.Priority.Value);
            }

            if (item.IgnorePath)
            {
                builder.IgnoringPath(ignorePath: true);
            }

            if (item.IgnoreQuery)
            {
                builder.IgnoringQuery(ignoreQuery: true);
            }

            return builder.SetContent(item);
        }

        private static HttpRequestInterceptionBuilder SetContent(this HttpRequestInterceptionBuilder builder, BundleItem item)
        {
            string content = GetRawContentString(item) ?? string.Empty;

            if (item.TemplateValues?.Count > 0)
            {
                content = TemplateString(content, item.TemplateValues);
            }

            return builder.WithContent(content);
        }

        private static string? GetRawContentString(BundleItem item)
        {
            switch (item.ContentFormat?.ToUpperInvariant())
            {
                case "BASE64":
                    byte[] decoded = Convert.FromBase64String(item.ContentString ?? string.Empty);
                    return Encoding.UTF8.GetString(decoded);

                case "JSON":
                    return item.ContentJson!.ToString();

                case null:
                case "":
                case "STRING":
                    return item.ContentString;

                default:
                    throw new NotSupportedException($"Content format '{item.ContentFormat}' for bundle item with Id '{item.Id}' is not supported.");
            }
        }

        private static void ValidateItem(BundleItem item, out Uri? uri, out Version? version)
        {
            version = null;

            if (item.Uri == null)
            {
                throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has no URI configured.");
            }

            string templated = TemplateString(item.Uri, item.TemplateValues);

            if (!Uri.TryCreate(templated, UriKind.Absolute, out uri))
            {
                throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has an invalid absolute URI '{item.Uri}' configured.");
            }

            if (item.Version != null && !Version.TryParse(item.Version, out version))
            {
                throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has an invalid version '{item.Version}' configured.");
            }
        }

        private static IDictionary<string, ICollection<string>> TemplateHeaders(
            IDictionary<string, ICollection<string>> headers,
            IDictionary<string, string>? parameters)
        {
            var result = headers;

            if (parameters?.Count > 0)
            {
                result = new Dictionary<string, ICollection<string>>(headers);

                foreach (var header in headers)
                {
                    if (header.Value?.Count > 0)
                    {
                        var copy = new List<string>(header.Value);

                        for (int i = 0; i < copy.Count; i++)
                        {
                            copy[i] = TemplateString(copy[i], parameters);
                        }

                        result[header.Key] = copy;
                    }
                }
            }

            return result;
        }

        private static string TemplateString(string content, IDictionary<string, string>? parameters)
        {
            if (parameters == null || parameters.Count < 1)
            {
                return content;
            }

            var builder = new StringBuilder(content);

            foreach (var pair in parameters)
            {
                // Template references use the format "${MyKey}"
                string key = $"${{{pair.Key}}}";
                builder.Replace(key, pair.Value);
            }

            return builder.ToString();
        }
    }
}
