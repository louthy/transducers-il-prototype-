using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LanguageExt;

internal readonly struct Instrs
{
    readonly IL IL;
    readonly int StackSize; 
    
    public Instrs()
    {
        IL         = IL.Empty;
        StackSize  = 4;
    }    

    Instrs(IL il, int stackSize)
    {
        IL         = il;
        StackSize  = stackSize;
    }

    internal Instrs(in Instr one)
    {
        var i = new Instrs().Add(one);
        IL        = i.IL;
        StackSize = i.StackSize;
    }

    internal Instrs(in Instr lhs, in Instr rhs)
    {
        var i = new Instrs().Add(lhs).Add(rhs);
        IL        = i.IL;
        StackSize = i.StackSize;
    }

    public Instrs WriteMap<A, B>(Func<A, B> f) =>
        WriteMapUntyped(f);

    Instrs WriteMapUntyped(Delegate f)
    {
        // For memory: https://minidump.net/c-why-function-pointers-cant-be-used-on-instance-methods-8a99fc99b040
        // delegate*<int, string> fp;
        // var fp = f.Method.MethodHandle.GetFunctionPointer();
        
        var il = IL;
        il.EmitMap(f);
        return new Instrs(il, StackSize);
    }

    public Instrs WriteIdentity() =>
        this;

    public Instrs Add(Instr instr)
    {
        switch (instr.Tag)
        {
            case InstrTag.Identity:
                return WriteIdentity();
            
            case InstrTag.MapF:
                return WriteMapUntyped((Delegate?)instr.Arg1 ?? throw new ArgumentException());
            
            default:
                throw new NotSupportedException();
        }
    }

    public Instrs Add(Instrs instr) =>
        new (IL.Add(instr.IL), StackSize);

    public override string ToString() =>
        "instructions";

    public S Transform<S, A, B>(S state, A input, Func<S, B, S> reducer)
    {
        /*
        MethodInfo m = new DynamicMethod();
        dynIL.GetTokenFor(m.MethodHandle);
        */
        
        var args = new[] { typeof(object?[]), typeof(S), typeof(A) };
        var il = IL;
        il.EmitWrap();
        var method = new DynamicMethod("AnyName", typeof(S), args);
        
        var locals = SignatureHelper.GetLocalVarSigHelper();
        locals.AddArguments(args, null, null);
        
        var dynIL = method.GetDynamicILInfo();
        dynIL.SetLocalSignature(locals.GetSignature());
        dynIL.SetCode(il.Instructions.AsSpan().ToArray(), 4);
        var f = (Func<object?[], S, A, S>)method.CreateDelegate(typeof(Func<object?[], S, A, S>));
        return f(il.Parameters.AsSpan().ToArray(), state, input);
    }
}
