// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

#if !NETFRAMEWORK
using System.Text.Json.Serialization;

namespace JustEat.HttpClientInterception;

[JsonSerializable(typeof(GitHubOrganization))]
internal sealed partial class GitHubJsonSerializerContext : JsonSerializerContext;
#endif
