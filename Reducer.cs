namespace LanguageExt;

public interface Reducer<A, S>
{
    TResult<S> Run(in S stateValue, in A value);
}
