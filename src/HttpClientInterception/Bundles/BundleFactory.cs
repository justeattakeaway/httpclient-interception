// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace JustEat.HttpClientInterception.Bundles;

/// <summary>
/// A class that creates bundles from JSON files. This class cannot be inherited.
/// </summary>
internal static class BundleFactory
{
    /// <summary>
    /// Gets the JSON serializer settings to use.
    /// </summary>
    private static JsonSerializerOptions Settings { get; } =
#if NET7_0_OR_GREATER
        JsonSerializerOptions.Default;
#else
        new();
#endif

    /// <summary>
    /// Creates a <see cref="Bundle"/> from the specified JSON file.
    /// </summary>
    /// <param name="path">The path of the JSON file containing the bundle.</param>
    /// <returns>
    /// The <see cref="Bundle"/> deserialized from the file specified by <paramref name="path"/>.
    /// </returns>
    public static Bundle? Create(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Bundle>(json, Settings);
    }

    /// <summary>
    /// Creates a <see cref="Bundle"/> from the specified JSON file as an asynchronous operation.
    /// </summary>
    /// <param name="path">The path of the JSON file containing the bundle.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="ValueTask{Bundle}"/> representing the asynchronous operation which
    /// returns the bundle deserialized from the file specified by <paramref name="path"/>.
    /// </returns>
    public static async ValueTask<Bundle?> CreateAsync(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Bundle>(stream, Settings, cancellationToken).ConfigureAwait(false);
    }
}
