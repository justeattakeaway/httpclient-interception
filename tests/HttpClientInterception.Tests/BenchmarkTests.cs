// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace JustEat.HttpClientInterception;

public static class BenchmarkTests
{
    [Fact]
    public static async Task Benchmarks_Do_Not_Throw_An_Exception()
    {
        // Arrange
        var target = new InterceptionBenchmarks();

        // Act and Assert
        await Should.NotThrowAsync(target.GetBytes);
        await Should.NotThrowAsync(target.GetHtml);
        await Should.NotThrowAsync(target.GetJsonDocument);
        await Should.NotThrowAsync(target.GetJsonObject);
        await Should.NotThrowAsync(target.GetJsonObjectSourceGenerator);
        await Should.NotThrowAsync(target.GetJsonObjectWithRefit);
        await Should.NotThrowAsync(target.GetStream);
    }
}
