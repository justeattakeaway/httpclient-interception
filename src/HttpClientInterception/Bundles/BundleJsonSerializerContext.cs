// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;

namespace JustEat.HttpClientInterception.Bundles;

[JsonSerializable(typeof(Bundle))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class BundleJsonSerializerContext : JsonSerializerContext
{
}
#endif
