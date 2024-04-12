﻿// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using JustEat.HttpClientInterception.Bundles;

namespace JustEat.HttpClientInterception;

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
        => options.RegisterBundle(path, []);

    /// <summary>
    /// Registers a bundle of HTTP request interceptions from a specified JSON file.
    /// </summary>
    /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to register the bundle with.</param>
    /// <param name="path">The path of the JSON file containing the serialized bundle.</param>
    /// <param name="templateValues">The optional template values to specify.</param>
    /// <returns>
    /// The value specified by <paramref name="options"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/>, <paramref name="path"/> or <paramref name="templateValues"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The version of the serialized bundle is not supported.
    /// </exception>
    public static HttpClientInterceptorOptions RegisterBundle(
        this HttpClientInterceptorOptions options,
        string path,
        IEnumerable<KeyValuePair<string, string>> templateValues)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (templateValues is null)
        {
            throw new ArgumentNullException(nameof(templateValues));
        }

        using var stream = File.OpenRead(path);

        return options.RegisterBundleFromStream(stream, templateValues);
    }

    /// <summary>
    /// Registers a bundle of HTTP request interceptions from a specified JSON stream.
    /// </summary>
    /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to register the bundle with.</param>
    /// <param name="stream">A <see cref="Stream"/> of JSON containing the serialized bundle.</param>
    /// <param name="templateValues">The optional template values to specify.</param>
    /// <returns>
    /// The value specified by <paramref name="options"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="stream"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The version of the serialized bundle is not supported.
    /// </exception>
    public static HttpClientInterceptorOptions RegisterBundleFromStream(
        this HttpClientInterceptorOptions options,
        Stream stream,
        IEnumerable<KeyValuePair<string, string>>? templateValues = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        templateValues ??= [];

        var bundle = BundleFactory.Create(stream);

        return options.RegisterBundle(bundle, templateValues);
    }

    /// <summary>
    /// Registers a bundle of HTTP request interceptions from a specified JSON file.
    /// </summary>
    /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to register the bundle with.</param>
    /// <param name="path">The path of the JSON file containing the serialized bundle.</param>
    /// <param name="templateValues">The optional template values to specify.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// The value specified by <paramref name="options"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/>, <paramref name="path"/> or <paramref name="templateValues"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The version of the serialized bundle is not supported.
    /// </exception>
    public static async Task<HttpClientInterceptorOptions> RegisterBundleAsync(
        this HttpClientInterceptorOptions options,
        string path,
        IEnumerable<KeyValuePair<string, string>>? templateValues = default,
        CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        using var stream = File.OpenRead(path);

        return await options.RegisterBundleFromStreamAsync(stream, templateValues, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Registers a bundle of HTTP request interceptions from a specified stream.
    /// </summary>
    /// <param name="options">The <see cref="HttpClientInterceptorOptions"/> to register the bundle with.</param>
    /// <param name="stream">A <see cref="Stream"/> of JSON containing the serialized bundle.</param>
    /// <param name="templateValues">The optional template values to specify.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// The value specified by <paramref name="options"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="stream"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The version of the serialized bundle is not supported.
    /// </exception>
    public static async Task<HttpClientInterceptorOptions> RegisterBundleFromStreamAsync(
        this HttpClientInterceptorOptions options,
        Stream stream,
        IEnumerable<KeyValuePair<string, string>>? templateValues = default,
        CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        templateValues ??= [];

        var bundle = await BundleFactory.CreateAsync(stream, cancellationToken).ConfigureAwait(false);

        return options.RegisterBundle(bundle!, templateValues);
    }

    private static HttpClientInterceptorOptions RegisterBundle(
        this HttpClientInterceptorOptions options,
        Bundle? bundle,
        IEnumerable<KeyValuePair<string, string>> templateValues)
    {
        if (bundle is null)
        {
            throw new InvalidOperationException("No HTTP request interception bundle was deserialized.");
        }

        if (bundle.Version != 1)
        {
            throw new NotSupportedException($"HTTP request interception bundles of version {bundle.Version} are not supported.");
        }

        var builders = new List<HttpRequestInterceptionBuilder>(bundle.Items?.Count ?? 0);

        if (bundle.Items != null)
        {
            var templateValuesList = templateValues.ToList();

            foreach (var item in bundle.Items)
            {
                if (item is null || item.Skip)
                {
                    continue;
                }

                builders.Add(BundleItemConverter.FromItem(item, templateValuesList));
            }
        }

        foreach (var builder in builders)
        {
            builder.RegisterWith(options);
        }

        return options;
    }
}
