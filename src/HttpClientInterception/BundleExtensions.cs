// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using JustEat.HttpClientInterception.Bundles;

namespace JustEat.HttpClientInterception
{
    /// <summary>
    /// A class containing extension methods for registering HTTP request bundles. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BundleExtensions
    {
        /// <summary>
        /// Registers a bundle of HTTP request interceptions from a specified JSON file.
        /// </summary>
        /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to register the bundle with.</param>
        /// <param name="path">The path of the JSON file containing the serialized bundle.</param>
        /// <returns>
        /// The value specified by <paramref name="options"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> or <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The version of the serialized bundle is not supported.
        /// </exception>
        public static HttpClientInterceptorOptions RegisterBundle(this HttpClientInterceptorOptions options, string path)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var bundle = BundleFactory.Create(path);

            if (bundle.Version != 1)
            {
                throw new NotSupportedException($"HTTP request interception bundles of version {bundle.Version} are not supported.");
            }

            var builders = new List<HttpRequestInterceptionBuilder>(bundle.Items?.Count ?? 0);

            if (bundle.Items != null)
            {
                foreach (var item in bundle.Items)
                {
                    if (item != null)
                    {
                        builders.Add(BundleItemConverter.FromItem(item));
                    }
                }
            }

            foreach (var builder in builders)
            {
                builder.RegisterWith(options);
            }

            return options;
        }
    }
}
