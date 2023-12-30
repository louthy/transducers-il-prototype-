namespace LanguageExt;

public readonly struct Transducer<A, B>
{
    readonly Instr Instruction;
    readonly Instrs? Instructions;

    /// <summary>
    /// Construct single
    /// </summary>
    internal Transducer(in Instr instruction)
    {
        Instruction  = instruction;
        Instructions = null;
    }

    /// <summary>
    /// Construct many
    /// </summary>
    Transducer(in Instrs instructions)
    {
        Instruction  = default;
        Instructions = instructions;
    }

    /// <summary>
    /// Compose
    /// </summary>
    public Transducer<A, C> Compose<C>(in Transducer<B, C> rhs) =>
        (Instruction.Tag, rhs.Instruction.Tag) switch
        {
            (null, null) when Instructions is not null && rhs.Instructions is not null =>
                new(Instructions.Value.Add(rhs.Instructions.Value)),

            (null, _) when Instructions is not null =>
                new(Instructions.Value.Add(rhs.Instruction)),

            (_, null) when rhs.Instructions is not null =>
                throw new NotImplementedException(),
                //new(rhs.Instructions.Value.Cons(Instruction)),

            _ => new(new Instrs(Instruction, rhs.Instruction))
        };

    /// <summary>
    /// Invoke the transducer with a reducer
    /// </summary>
    /// <param name="initialState"></param>
    /// <param name="value"></param>
    /// <typeparam name="S"></typeparam>
    /// <returns></returns>
    public S Invoke<S>(S initialState, A value, Func<S, B, S> reducer)
    {
        if (Instructions is null)
        {
            return new Instrs(Instruction).Transform(initialState, value, reducer);
        }
        else
        {
            return Instructions.Value.Transform(initialState, value, reducer);
        }
    }
    
    /// <summary>
    /// ToString
    /// </summary>
    public override string ToString() =>
        Instructions?.ToString() ?? Instruction.ToString();
}

public static class Transducer
{
    public static Transducer<A, A> identity<A>() =>
        new (Instr.Identity);

    public static Transducer<A, C> compose<A, B, C>(in Transducer<A, B> lhs, in Transducer<B, C> rhs) =>
        lhs.Compose(rhs);

    public static Transducer<A, B> map<A, B>(Func<A, B> f) =>
        new(Instr.Map(f));
    
    public static Transducer<A, B> bind<Env, A, B>(Func<A, Transducer<Env, B>> f) =>
        new(Instr.Bind(f));
}