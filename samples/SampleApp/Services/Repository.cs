// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace SampleApp.Services
{
    public class Repository
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
