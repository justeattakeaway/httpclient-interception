// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;

namespace JustEat.HttpClientInterception.Bundles
{
    /// <summary>
    /// A class that creates <see cref="HttpRequestInterceptionBuilder"/> instances from <see cref="BundleItem"/> instances. This class cannot be inherited.
    /// </summary>
    internal static class BundleItemConverter
    {
        public static HttpRequestInterceptionBuilder FromItem(BundleItem item)
        {
            ValidateItem(item, out Uri uri, out Version version);

            var builder = new HttpRequestInterceptionBuilder().ForUri(uri);

            if (item.Method != null)
            {
                builder.ForMethod(new System.Net.Http.HttpMethod(item.Method));
            }

            if (item.RequestHeaders?.Count > 0)
            {
                builder.ForRequestHeaders(item.RequestHeaders);
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
                builder.WithResponseHeaders(item.ResponseHeaders);
            }

            if (item.ContentHeaders?.Count > 0)
            {
                builder.WithContentHeaders(item.ContentHeaders);
            }

            if (item.Priority.HasValue)
            {
                builder.HavingPriority(item.Priority.Value);
            }

            return builder.SetContent(item);
        }

        private static HttpRequestInterceptionBuilder SetContent(this HttpRequestInterceptionBuilder builder, BundleItem item)
        {
            string content = GetContentString(item) ?? string.Empty;

            if (item.TemplateValues?.Count > 0)
            {
                // TODO Apply templating to the content
            }

            return builder.WithContent(content);
        }

        private static string GetContentString(BundleItem item)
        {
            switch (item.ContentFormat?.ToUpperInvariant())
            {
                case "BASE64":
                    byte[] decoded = Convert.FromBase64String(item.ContentString);
                    return Encoding.UTF8.GetString(decoded);

                case "JSON":
                    return item.ContentJson.ToString();

                case null:
                case "":
                case "STRING":
                    return item.ContentString;

                default:
                    throw new NotSupportedException($"Content format '{item.ContentFormat}' for bundle item with Id '{item.Id}' is not supported.");
            }
        }

        private static void ValidateItem(BundleItem item, out Uri uri, out Version version)
        {
            version = null;

            if (item.Uri == null)
            {
                throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has no URI configured.");
            }

            if (!Uri.TryCreate(item.Uri, UriKind.Absolute, out uri))
            {
                throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has an invalid absolute URI '{item.Uri}' configured.");
            }

            if (item.Version != null && !Version.TryParse(item.Version, out version))
            {
                throw new InvalidOperationException($"Bundle item with Id '{item.Id}' has an invalid version '{item.Version}' configured.");
            }
        }
    }
}
