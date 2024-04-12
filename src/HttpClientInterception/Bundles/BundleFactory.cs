// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace JustEat.HttpClientInterception.Bundles;

/// <summary>
/// A class that creates bundles from JSON files. This class cannot be inherited.
/// </summary>
internal static class BundleFactory
{
#if !NET6_0_OR_GREATER
    /// <summary>
    /// Gets the JSON serializer settings to use.
    /// </summary>
    private static JsonSerializerOptions Settings { get; } = new();
#endif

    /// <summary>
    /// Creates a <see cref="Bundle"/> from the specified JSON stream.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> of JSON containing the bundle.</param>
    /// <returns>
    /// The <see cref="Bundle"/> deserialized from the stream specified by <paramref name="stream"/>.
    /// </returns>
    public static Bundle? Create(Stream stream)
    {
#if NET6_0_OR_GREATER
        return JsonSerializer.Deserialize(stream, BundleJsonSerializerContext.Default.Bundle);
#else
        string json;

        using (var reader = new StreamReader(stream))
        {
            json = reader.ReadToEnd();
        }

        return JsonSerializer.Deserialize<Bundle>(json, Settings);
#endif
    }

    /// <summary>
    /// Creates a <see cref="Bundle"/> from the specified JSON stream as an asynchronous operation.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> of JSON containing the bundle.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="ValueTask{Bundle}"/> representing the asynchronous operation which
    /// returns the bundle deserialized from the stream specified by <paramref name="stream"/>.
    /// </returns>
    public static async ValueTask<Bundle?> CreateAsync(Stream stream, CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        return await JsonSerializer.DeserializeAsync(stream, BundleJsonSerializerContext.Default.Bundle, cancellationToken).ConfigureAwait(false);
#else
        return await JsonSerializer.DeserializeAsync<Bundle>(stream, Settings, cancellationToken).ConfigureAwait(false);
#endif
    }
}
