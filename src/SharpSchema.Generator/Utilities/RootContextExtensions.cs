using System.Runtime.CompilerServices;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class RootContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LeafSyntaxVisitor LeafSyntaxVisitor(this RootContext rootContext, GeneratorOptions options)
    {
        return new LeafSyntaxVisitor(rootContext, options);
    }
}
