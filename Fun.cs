using System.Collections.Concurrent;

namespace LanguageExt;

public abstract record Fun
{
    public abstract object? Invoke(object? value);

    public static Fun<A, B> New<A, B>(Func<A, B> function) =>
        Fun<A, B>.cache.GetOrAdd(function, new Fun<A, B>(function));
}

public record Fun<A, B>(Func<A, B> Function) : Fun
{
    internal static ConcurrentDictionary<Func<A, B>, Fun<A, B>> cache = new();

    public override object? Invoke(object? value) =>
        value switch
        {
            A x => Function(x),
            _   => throw new ArgumentException("invalid argument type for Fun")
        };
}