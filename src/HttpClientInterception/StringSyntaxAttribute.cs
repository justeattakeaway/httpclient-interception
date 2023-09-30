// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

//// Based on https://github.com/dotnet/runtime/blob/65067052e433eda400c5e7cc9f7b21c84640f901/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/StringSyntaxAttribute.cs

#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class StringSyntaxAttribute(string syntax, params object?[] arguments) : Attribute
{
    public const string Uri = nameof(Uri);

    public StringSyntaxAttribute(string syntax)
        : this(syntax, [])
    {
    }

    public string Syntax { get; } = syntax;

    public object?[] Arguments { get; } = arguments;
}
#endif
