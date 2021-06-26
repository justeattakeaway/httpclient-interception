// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace SampleApp.Tests
{
    [CollectionDefinition(Name)]
    public sealed class HttpServerCollection : ICollectionFixture<HttpServerFixture>
    {
        public const string Name = "HTTP server";
    }
}
