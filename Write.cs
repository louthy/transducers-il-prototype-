namespace LanguageExt;

/*
internal readonly ref struct Write
{
    public readonly byte[] Instructions;
    public readonly List<object?> Parameters;
    readonly int ibegin;
    readonly int ilength;
    readonly int pbegin;
    readonly int plength;

    public Write(
        byte[] instructions, 
        List<object?> parameters,
        int ibegin, int ilength,
        int pbegin, int plength
    )
    {
        Instructions = instructions;
        Parameters   = parameters;
        this.ibegin  = ibegin;
        this.ilength = ilength;
        this.pbegin  = pbegin;
        this.plength = plength;
    }

    public Span<byte> InstructionsSpan => 
        new(Instructions, ibegin, ilength);

    public int ILBegin =>
        ibegin;

    public int ILEnd =>
        ibegin + ilength;

    public int ILLength =>
        ilength;
    
    public Span<object?> ParametersSpan =>
        CollectionsMarshal.AsSpan(Parameters).Slice(pbegin, plength);

    public int ParametersBegin =>
        pbegin;

    public int ParametersEnd =>
        pbegin + plength;

    public int ParametersLength =>
        plength;
}
*/
