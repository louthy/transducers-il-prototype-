using System.Runtime.CompilerServices;

namespace LanguageExt;

public readonly struct Slice<A>
{
    const int DefaultSize = 32;
    const int DefaultBegin = 6;
    
    readonly int begin;
    readonly int length;
    readonly A[] array;
    readonly int immutable;

    public static Slice<A> Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => new (new A[DefaultSize], DefaultBegin, 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    Slice(A[] array, int begin, int length, int immutable)
    {
        this.begin     = begin;
        this.length    = length;
        this.array     = array;
        this.immutable = immutable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Span<A> AsSpan() =>
        new (array, begin, length);

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => length;
    }

    public A this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => index >= 0 && index < length
                   ? array[begin + index]
                   : throw new ArgumentOutOfRangeException();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        set
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException();
            }
            array[begin + index] = value;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Slice<A> Add(A value)
    {
        var response = AddRequest(1);
        response.Requested[0] = value;
        return response.All;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Slice<A> Cons(A value)
    {
        var response = ConsRequest(1);
        response.Requested[0] = value;
        return response.All;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Slice<A> Add(in Slice<A> values)
    {
        var response = AddRequest(values.length);
        var from = values.AsSpan();
        var to = response.Requested.AsSpan();
        from.CopyTo(to);
        return response.All;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Slice<A> Cons(in Slice<A> values)
    {
        var response = ConsRequest(values.length);
        var from = values.AsSpan();
        var to = response.Requested.AsSpan();
        from.CopyTo(to);
        return response.All;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Slice<A> Add(A[] values)
    {
        var response = AddRequest(values.Length);
        var to = response.Requested.AsSpan();
        values.CopyTo(to);
        return response.All;
    }    

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Slice<A> Cons(A[] values)
    {
        var response = ConsRequest(values.Length);
        var to = response.Requested.AsSpan();
        values.CopyTo(to);
        return response.All;
    }    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Response<A> AddRequest(int count)
    {
        if (Interlocked.CompareExchange(ref Unsafe.AsRef(immutable), 1, 0) == 0)
        {
            var narray = array;
            var end = begin + length; 
            
            if (end + count >= array.Length)
            {
                // Grow
                var size = Size.Find(array.Length + count);
                narray = new A[size];
                Array.Copy(array, begin, narray, begin, length);
            }

            var all = new Slice<A>(narray, begin, length + count, 0);
            var req = new Slice<A>(narray, end, count, 1);
            return new Response<A>(all, req);
        }
        else
        {
            // Clone
            var narray = new A[array.Length];
            Array.Copy(array, begin, narray, begin, length);
            return new Slice<A>(narray, begin, length, 0).AddRequest(count);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Response<A> ConsRequest(int count)
    {
        if (Interlocked.CompareExchange(ref Unsafe.AsRef(immutable), 1, 0) == 0)
        {
            var narray = array;
            var nbegin = begin   - count;
            var nlength = length + count;
            
            if (nbegin < 0)
            {
                // Grow
                var size = Size.Find(array.Length + count);
                narray = new A[size];
                var half = size >> 1;
                nbegin = half - count;
                Array.Copy(array, begin, narray, half, length);
                
                var all = new Slice<A>(narray, nbegin, nlength, 0);
                var req = new Slice<A>(narray, nbegin, count, 1);
                return new Response<A>(all, req);
            }
            else
            {
                var all = new Slice<A>(narray, nbegin, nlength, 0);
                var req = new Slice<A>(narray, nbegin, count, 1);
                return new Response<A>(all, req);
            }
        }
        else
        {
            // Clone
            var narray = new A[array.Length];
            Array.Copy(array, begin, narray, begin, length);
            return new Slice<A>(narray, begin, length, 0).ConsRequest(count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void CopyTo(Slice<A> rhs) =>
        AsSpan().CopyTo(rhs.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void CopyTo(Span<A> rhs) =>
        AsSpan().CopyTo(rhs);
}

public readonly ref struct Response<A>
{
    public readonly Slice<A> All;
    public readonly Slice<A> Requested;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal Response(Slice<A> all, Slice<A> requested)
    {
        All       = all;
        Requested = requested;
    }
}
