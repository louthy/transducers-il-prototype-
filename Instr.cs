namespace LanguageExt;


internal readonly struct Instr
{
    public readonly string Tag;
    public readonly object? Arg1;
    public readonly object? Arg2;
    public readonly object? Arg3;

    static Instr()
    {
        Identity = new (InstrTag.Identity);
    }
    
    Instr(string tag)
    {
        Tag  = tag;
        Arg1 = null;
        Arg2 = null;
        Arg3 = null;
    }

    Instr(string tag, object arg1)
    {
        Tag  = tag;
        Arg1 = arg1;
        Arg2 = null;
        Arg3 = null;
    }

    Instr(string tag, object arg1, object arg2, object arg3)
    {
        Tag  = tag;
        Arg1 = arg1;
        Arg2 = arg2;
        Arg3 = arg3;
    }

    public static readonly Instr Identity;

    public static Instr Map<A, B>(Func<A, B> f) =>
        new (InstrTag.MapF, f);

    public static Instr Bind<Env, A, B>(Func<A, Transducer<Env, B>> f) =>
        new (InstrTag.BindF, f);

    public override string ToString() =>
        "instruction";
}
