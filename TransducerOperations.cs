using System;
using System.Reflection;
using System.Reflection.Emit;

namespace LanguageExt;

internal static partial class TransducerOperations
{
    public static B Map<A, B>(Func<A, B> f)
    {
        throw new NotImplementedException();

        /*
        Module module;
        ModuleBuilder moduleb = (ModuleBuilder)module;

        RuntimeModule

        ModuleBuilder moduleb;
        MethodBuilder methodb;

        var tb = new TypeBuilder();

        var domain = Thread.GetDomain();
        var asmName = "LanguageExt.Transducers.Live";
        var asmBuild =
        */

        /*var method = new DynamicMethod("map", typeof(B), new [] {typeof(A) });

        var info = method.GetDynamicILInfo();
        info.SetCode();

        var il = method.GetILGenerator();

        //il.Emit(OpCodes.Ldarg_0, );
        il.EmitCall(OpCodes.Callvirt, f.Method, null);

        var d = (Func<A, B>)method.CreateDelegate(typeof(Func<A, B>));
        return d;*/
    }

    public static S Bind<S, E, A, B>(TState state, S stateVlaue, in E env, in A value, Func<A, Transducer<E, B>> f)
    {
        var t = f(value);
        throw new NotImplementedException();
    }
}

