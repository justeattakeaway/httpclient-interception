// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Refit;

namespace SampleApp.Services
{
    public interface IGitHub
    {
        [Get("/orgs/{organization}/repos?per_page={count}")]
        Task<ICollection<Repository>> GetRepositoriesAsync(string organization, int? count = null);
    }
}
