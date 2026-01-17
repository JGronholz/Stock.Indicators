namespace Skender.Stock.Indicators;

/// <inheritdoc cref="IStreamHub{TIn, TOut}"/>
public abstract class ChainHub<TIn, TOut>(
    IStreamObservable<TIn> provider
) : StreamHub<TIn, TOut>(provider), IChainProvider<TOut>  // likely has to be concrete type
     where TIn : IReusable
     where TOut : IReusable
{
    /// <summary>
    /// Converts a static list of input values to a list of indicator results using the hub's parameters.
    /// </summary>
    /// <param name="input">The list of input values.</param>
    /// <returns>A list of indicator results.</returns>
    public abstract IReadOnlyList<TOut> AsStaticSeries(IReadOnlyList<TIn> input);
}
