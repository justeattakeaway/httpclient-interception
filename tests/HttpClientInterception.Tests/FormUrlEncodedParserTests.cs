// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace JustEat.HttpClientInterception;

public static class FormUrlEncodedParserTests
{
    public static TheoryData<string> EdgeCases() =>
    [
        null,
        string.Empty,
        "?",
        "??",
        "&",
        "&&",
        "=",
        "==",
        "?=",
        "a",
        "?a",
        "a=",
        "=a",
        "a=b",
        "?a=b",
        "a=b&c=d",
        "a=b&&c=d",
        "&a=b&",
        "a=b=c",
        "a&b=c",
        "a=b&c",
        "a&b&c",
        "a=1&a=2",
        "a=1&A=2",
        "a=1&a=2&a=3&a=4",
        "a=&a=",
        "a&a",
        "a=1&a",
        "a+b=c+d",
        "a%20b=c%20d",
        "a%2Bb=c%2Bd",
        "a%26b=c%3Dd",
        "%=%",
        "a=%",
        "a=%2",
        "a=%zz",
        "a=%E2%82%AC",
        "a=%E2%82",
        "a%20&b",
        "a+&b",
        "  a=b",
        "a  =b",
        " =b",
        "\ta=b& \r\nc=d",
        "a= b ",
        "Action=SetQueueAttributes&Attribute.Name=VisibilityTimeout&Attribute.Value=35",
        "{\"name\":\"value\"}",
        "[1,2,3]",
        "<xml a=\"b\" />",
        "ключ=значение",
        "%D0%BA%D0%BB%D1%8E%D1%87=%D0%B7%D0%BD%D0%B0%D1%87%D0%B5%D0%BD%D0%B8%D0%B5",
    ];

    [Theory]
    [MemberData(nameof(EdgeCases))]
    public static void Parse_Matches_QueryHelpers_For_Edge_Cases(string content)
    {
        // Act
        var actual = FormUrlEncodedParser.Parse(content);

        // Assert
        actual.ShouldBe(ParseWithQueryHelpers(content), ignoreOrder: true);
    }

    [Fact]
    public static void Parse_Matches_QueryHelpers_For_Random_Content()
    {
        // Arrange
        const string Alphabet = "aAbB1 +%=&?\t2E8z";

#pragma warning disable CA5394 // A seeded pseudo-random generator is used so failures are reproducible
        var random = new Random(42);
        var builder = new StringBuilder();

        for (int i = 0; i < 100_000; i++)
        {
            builder.Clear();

            int length = random.Next(0, 24);

            for (int j = 0; j < length; j++)
            {
                builder.Append(Alphabet[random.Next(Alphabet.Length)]);
            }
#pragma warning restore CA5394

            string content = builder.ToString();

            // Act
            var actual = FormUrlEncodedParser.Parse(content);

            // Assert
            actual.ShouldBe(ParseWithQueryHelpers(content), ignoreOrder: true, customMessage: $"Content: \"{content}\"");
        }
    }

    [Fact]
    public static void Parse_Matches_QueryHelpers_For_Many_Repeated_Values()
    {
        // Arrange
        string content = string.Join("&", Enumerable.Range(0, 10_000).Select((i) => i % 3 == 0 ? "a=" : $"A={i}"));

        // Act
        var actual = FormUrlEncodedParser.Parse(content);

        // Assert
        actual.ShouldBe(ParseWithQueryHelpers(content), ignoreOrder: true);
    }

    [Fact]
    public static void Parse_Keys_Are_Case_Insensitive()
    {
        // Act
        var actual = FormUrlEncodedParser.Parse("Foo=Bar");

        // Assert
        actual.ShouldContainKeyAndValue("foo", "Bar");
        actual.ShouldContainKeyAndValue("FOO", "Bar");
    }

    [Fact]
    public static void Parse_Joins_Multiple_Values_With_A_Comma()
    {
        // Act
        var actual = FormUrlEncodedParser.Parse("a=1&b=2&A=3");

        // Assert
        actual.ShouldContainKeyAndValue("a", "1,3");
        actual.ShouldContainKeyAndValue("b", "2");
    }

    [Theory]
    [InlineData("a=&a=", "")]
    [InlineData("a=1&a=", "1")]
    [InlineData("a=&a=1", "1")]
    [InlineData("a=1&a=&a=2", "1,2")]
    [InlineData("a=1&a", "1")]
    public static void Parse_Omits_Empty_Values_When_Joining_Multiple_Values(string content, string expected)
    {
        // Act
        var actual = FormUrlEncodedParser.Parse(content);

        // Assert
        actual.ShouldContainKeyAndValue("a", expected);
    }

    [Theory]
    [InlineData("", "a", "", false)]
    [InlineData("a", "a", "", true)]
    [InlineData("A=1", "a", "1", true)]
    [InlineData(" a=1", "a", "1", true)]
    [InlineData("a=1&a=&a=2", "a", "1,2", true)]
    [InlineData("a=1&a=2", "a", "1", false)]
    [InlineData("a=b+c%20d", "a", "b c d", true)]
    [InlineData("a%20b", "a b", "", false)]
    [InlineData("a%20b", "a%20b", "", true)]
    [InlineData("=1&a=2", "a", "2", true)]
    [InlineData(" =1&a=2", "a", "2", true)]
    [InlineData("&&a=1&", "a", "1", true)]
    public static async Task ForFormContent_Matches_Using_Form_Parsing_Rules(string body, string key, string value, bool shouldMatch)
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl("https://test.local/form")
            .ForFormContent(new Dictionary<string, string>() { [key] = value });

        var options = new HttpClientInterceptorOptions().Register(builder);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://test.local/form")
        {
            Content = new StringContent(body),
        };

        // Act
        using var response = await options.GetResponseAsync(request, TestContext.Current.CancellationToken);

        // Assert
        (response is not null).ShouldBe(shouldMatch);
    }

    private static Dictionary<string, string> ParseWithQueryHelpers(string content)
        => QueryHelpers.ParseQuery(content).ToDictionary((p) => p.Key, (p) => p.Value.ToString(), StringComparer.OrdinalIgnoreCase);
}
