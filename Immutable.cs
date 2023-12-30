using System.Buffers;

namespace LanguageExt;

public class Immutable
{
    volatile int immutable;

    public static Immutable No => 
        new ();

    public static readonly Immutable Yes =
        new() { immutable = 1 };

    public bool Flip =>
        Interlocked.CompareExchange(ref immutable, 1, 0) == 0;
}