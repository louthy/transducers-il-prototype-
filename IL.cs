using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace LanguageExt;

internal readonly struct IL
{
    public readonly Slice<byte> Instructions;
    public readonly Slice<object?> Parameters;

    public static IL Empty => new IL(Slice<byte>.Empty, Slice<object?>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    IL(in Slice<byte> instructions, in Slice<object?> parameters)
    {
        Instructions = instructions;
        Parameters   = parameters;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Add(in IL rhs)
    {
        var paramOffset = Parameters.Count;
        var nis = Instructions.AddRequest(rhs.Instructions.Count);
        var nps = Parameters.AddRequest(rhs.Parameters.Count);

        rhs.Instructions.CopyTo(nis.Requested);
        rhs.Parameters.CopyTo(nps.Requested);

        var length = nis.Requested.Count - 8; // 8 == the minimum size needed for a load-parameter operation
        for (var ix = 0; ix < length; ix++)
        {
            var code = nis.Requested[ix];
            if (code                  == 0x08 /* Ldloc_2 */    &&
                nis.Requested[ix + 1] == 0x20 /* Ldc_I4 */     &&
                nis.Requested[ix + 6] == 0x9A /* Ldelem_Ref */ &&
                nis.Requested[ix + 7] == 0x74 /* Castclass */ )
            {
                var slice = nis.Requested.AsSpan().Slice(ix + 2, 4);
                var pi = BinaryPrimitives.ReadInt32LittleEndian(slice);
                BinaryPrimitives.WriteInt32LittleEndian(slice, paramOffset + pi);
                ix += 7;
            }
        }
        return new IL(nis.All, nps.All);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Add1(byte value)
    {
        var il = Instructions.Add(value);
        return new(il, Parameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Add2(short value)
    {
        var il = Instructions.AddRequest(2);
        BinaryPrimitives.WriteInt16LittleEndian(il.Requested.AsSpan(), value);
        return new(il.All, Parameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Add4(int value)
    {
        var il = Instructions.AddRequest(4);
        BinaryPrimitives.WriteInt32LittleEndian(il.Requested.AsSpan(), value);
        return new(il.All, Parameters);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Cons1(byte value)
    {
        var il = Instructions.Cons(value);
        return new(il, Parameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Cons2(short value)
    {
        var il = Instructions.ConsRequest(2);
        BinaryPrimitives.WriteInt16LittleEndian(il.Requested.AsSpan(), value);
        return new(il.All, Parameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL Cons4(int value)
    {
        var il = Instructions.ConsRequest(4);
        BinaryPrimitives.WriteInt32LittleEndian(il.Requested.AsSpan(), value);
        return new(il.All, Parameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IL AddParameter(object? parameter) =>
        new(Instructions, Parameters.Add(parameter));
}

internal static class ILExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void EmitWrap(this ref IL il)
    {
        // Loc 0 = parameters object? array
        // Loc 1 = state in / state out
        // Loc 2 = value in / value out 

        il = il.Cons1((byte)OpCodes.Stloc_2.Value);
        il = il.Cons1((byte)OpCodes.Ldarg_2.Value);
        il = il.Cons1((byte)OpCodes.Stloc_1.Value);
        il = il.Cons1((byte)OpCodes.Ldarg_1.Value);
        il = il.Cons1((byte)OpCodes.Stloc_0.Value);
        il = il.Cons1((byte)OpCodes.Ldarg_0.Value);
        
        // TODO: Invoke the reducer with Loc_0 (State) and Loc_1 (Value)
        // TODO: Have a 3rd argument that collects disposables and clean them up here
        
        il.Emit(OpCodes.Ldloc_1);                       
        il.Emit(OpCodes.Ret);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void EmitMap(this ref IL il, Delegate f)
    {
        // TODO: Decide where `f` comes from - probably need to create a containing class that holds
        // TODO: the parameters of the transducers

        var pix = il.Parameters.Count;
        il = il.AddParameter(f);

        //il.EmitLoadParameter(pix, f.GetType()); // delegate instance 
        //il.Emit(OpCodes.Pop);
        
        if (f.Target is null)
        {
            Emit(ref il, OpCodes.Ldnull); // static
        }
        else
        {
            il.EmitLoadParameter(pix, f.GetType()); // delegate instance 
        }

        il.EmitLoadValue();                      // load Value
        il.EmitCall(OpCodes.Callvirt, f.Method); // Invoke
        il.EmitStoreValue();                     // store Value
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void EmitLoadValue(this ref IL il)
    {
        il.Emit(OpCodes.Ldloc);
        il = il.Add2(0x7175);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void EmitStoreValue(this ref IL il)
    {
        il.Emit(OpCodes.Stloc);
        il = il.Add2(0x7175);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void EmitLoadParameter(this ref IL il, int ix, Type type)
    {
        il.Emit(OpCodes.Ldloc_2);    // `parameters`
        il.Emit(OpCodes.Ldc_I4);     // parameters index
        il = il.Add4(ix);            // ^ note: this doesn't use `Emit(code, int)` to enforce the size of the constant  
        il.Emit(OpCodes.Ldelem_Ref); // `parameters[index]`
        il.EmitCast(type);           // (Type)x
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void Emit(this ref IL il, OpCode opCode) =>
        il.RawEmit(opCode);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void EmitCall(
        this ref IL il, 
        OpCode opcode, 
        MethodInfo methodInfo)
    {
        if (methodInfo.ReturnType == typeof(void)) throw new NotSupportedException("all functions must return a value");
        il.RawEmit(opcode);
        il = il.Add4(methodInfo.MetadataToken);
    }    

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void RawEmit(this ref IL il, OpCode opCode)
    {
        var size = opCode.Size;
        var value = opCode.Value;
        if (size == 1)
            il = il.Add1((byte)value);
        else
            il = il.Add2(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void Emit(this ref IL il, OpCode opCode, int arg)
    {
        if (opCode.Equals(OpCodes.Ldc_I4))
        {
            if (arg >= -1 && arg <= 8)
            {
                opCode = arg switch
                         {
                             -1 => OpCodes.Ldc_I4_M1,
                             0  => OpCodes.Ldc_I4_0,
                             1  => OpCodes.Ldc_I4_1,
                             2  => OpCodes.Ldc_I4_2,
                             3  => OpCodes.Ldc_I4_3,
                             4  => OpCodes.Ldc_I4_4,
                             5  => OpCodes.Ldc_I4_5,
                             6  => OpCodes.Ldc_I4_6,
                             7  => OpCodes.Ldc_I4_7,
                             _  => OpCodes.Ldc_I4_8,
                         };
                il.Emit(opCode);
                return;
            }

            if (arg >= -128 && arg <= 127)
            {
                byte arg1 = unchecked((byte)(sbyte)arg);
                il.Emit(OpCodes.Ldc_I4_S);
                il = il.Add1(arg1);
                return;
            }
        }
        else if (opCode.Equals(OpCodes.Ldarg))
        {
            if ((uint)arg <= 3)
            {
                il.Emit(arg switch
                        {
                            0 => OpCodes.Ldarg_0,
                            1 => OpCodes.Ldarg_1,
                            2 => OpCodes.Ldarg_2,
                            _ => OpCodes.Ldarg_3,
                        });
                return;
            }

            if ((uint)arg <= byte.MaxValue)
            {
                var arg1 = (byte)arg; 
                il.Emit(OpCodes.Ldarg_S);
                il = il.Add1(arg1);
                return;
            }

            if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
            {
                var arg2 = (short)arg;
                il.Emit(OpCodes.Ldarg);
                il = il.Add2(arg2);
                return;
            }
        }
        il.Emit(opCode);
        il = il.Add4(arg);
    }
  
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void EmitCast(this ref IL il, Type type) =>
      //il.Emit(OpCodes.Castclass, type.MetadataToken); // TODO REPLACE WITH SOMETHING THAT WORKS!!!
        il.Emit(OpCodes.Castclass, type.TypeHandle.Value); // TODO REPLACE WITH SOMETHING THAT WORKS!!!
}