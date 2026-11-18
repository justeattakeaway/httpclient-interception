// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

#nullable enable

namespace JustEat.HttpClientInterception;

/// <summary>
/// A class containing methods for parsing URL-encoded form content. This class cannot be inherited.
/// </summary>
/// <remarks>
/// The parsing behaviour matches <c>QueryHelpers.ParseQuery()</c> from the
/// <c>Microsoft.AspNetCore.WebUtilities</c> NuGet package, including its quirks,
/// so that form content matching behaves the same as when that package was used.
/// </remarks>
internal static class FormUrlEncodedParser
{
    /// <summary>
    /// Parses the specified URL-encoded form content into its keys and values.
    /// </summary>
    /// <param name="content">The URL-encoded content to parse, with or without a leading <c>?</c>.</param>
    /// <returns>
    /// A dictionary of the parsed keys, which are case-insensitive, and their values. If a key
    /// appears more than once, its values are joined with a comma in the order they appear.
    /// </returns>
    /// <remarks>
    /// Content that is not well-formed is parsed without error, so the result for any content
    /// that is not a form (for example JSON) will contain keys that do not match the caller's input.
    /// </remarks>
    internal static Dictionary<string, string> Parse(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(content))
        {
            return result;
        }

        int index = content[0] == '?' ? 1 : 0;
        int length = content.Length;

        while (index < length)
        {
#pragma warning disable CA1307 // The overloads accepting a StringComparison are not available for all target frameworks
            int delimiter = content.IndexOf('&', index);

            if (delimiter == -1)
            {
                delimiter = length;
            }

            int separator = content.IndexOf('=', index, delimiter - index);
#pragma warning restore CA1307

            if (separator != -1)
            {
                // Leading whitespace is trimmed from the key, but not trailing whitespace
                while (index < separator && char.IsWhiteSpace(content[index]))
                {
                    index++;
                }

                string key = Decode(content.Substring(index, separator - index));
                string value = Decode(content.Substring(separator + 1, delimiter - separator - 1));

                Append(result, key, value);
            }
            else if (delimiter > index)
            {
                // A key without a value is used as-is without being decoded
                Append(result, content.Substring(index, delimiter - index), string.Empty);
            }

            index = delimiter + 1;
        }

        return result;
    }

    private static string Decode(string value)
        => Uri.UnescapeDataString(value.Replace('+', ' '));

    private static void Append(Dictionary<string, string> result, string key, string value)
    {
        // Multiple values are joined in the same way as StringValues.ToString(),
        // which joins them with a comma but omits any values that are empty.
        if (!result.TryGetValue(key, out string? existing) || existing.Length == 0)
        {
            result[key] = value;
        }
        else if (value.Length > 0)
        {
            result[key] = existing + "," + value;
        }
    }
}
