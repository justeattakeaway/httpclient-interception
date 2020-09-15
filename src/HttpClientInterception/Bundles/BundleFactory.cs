// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json;

namespace JustEat.HttpClientInterception.Bundles
{
    /// <summary>
    /// A class that creates bundles from JSON files. This class cannot be inherited.
    /// </summary>
    internal static class BundleFactory
    {
        /// <summary>
        /// Gets the JSON serializer settings to use.
        /// </summary>
        private static JsonSerializerSettings Settings { get; } = new JsonSerializerSettings();

        /// <summary>
        /// Creates a <see cref="Bundle"/> from the specified JSON file.
        /// </summary>
        /// <param name="path">The path of the JSON file containing the bundle.</param>
        /// <returns>
        /// The <see cref="Bundle"/> deserialized from the file specified by <paramref name="path"/>.
        /// </returns>
        public static Bundle Create(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Bundle>(json, Settings) !;
        }
    }
}
