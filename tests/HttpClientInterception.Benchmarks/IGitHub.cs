// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Refit;

namespace JustEat.HttpClientInterception
{
    [Headers("Accept: application/vnd.github.v3+json", "User-Agent: JustEat-HttpClientInterception-Benchmarks")]
    internal interface IGitHub
    {
        [Get("/orgs/{organization}")]
        Task<Organization> GetOrganizationAsync(string organization);
    }
}
