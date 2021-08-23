// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using SampleApp.Extensions;
using SampleApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClients();

var app = builder.Build();

app.MapGet("/api/repos", async (IConfiguration config, IGitHub github, int? count) =>
{
    // Get the configured organization's repositories
    string organization = config["Organization"];

    ICollection<Repository> repositories = await github.GetRepositoriesAsync(organization, count ?? 100);

    // Return the repositories' names
    var names = new List<string>();

    foreach (var repository in repositories)
    {
        names.Add(repository.Name);
    }

    names.Sort();

    return Results.Json(names, new() { WriteIndented = true });
});

app.Run();
