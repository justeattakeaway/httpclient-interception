// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

#pragma warning disable CA1812

using BenchmarkDotNet.Running;
using JustEat.HttpClientInterception;

BenchmarkRunner.Run<InterceptionBenchmarks>(args: args);
