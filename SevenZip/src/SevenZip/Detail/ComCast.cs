namespace SevenZip.Detail;

/// <summary>
/// The <see cref="ComCast"/> class has the purpose to express an intentional cast between 
/// two unrelated types which is expected to succeed. 
/// This is comparable to a call to <c>IUnknown.QueryInterface</c>, where a COM(-like)
/// object may return any other COM-interface it supports.
/// </summary>
internal static class ComCast
{
    /// <summary>
    /// Performs a cast from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
    /// </summary>
    /// <typeparam name="TFrom">The original type of <paramref name="interface"/>.</typeparam>
    /// <typeparam name="TTo">The desired type.</typeparam>
    /// <param name="interface">The instance to cast.</param>
    /// <returns>
    /// The resulting instance of type <typeparamref name="TTo"/>, if <paramref name="interface"/>
    /// supports that type. Otherwise, <c>null</c> is being returned.
    /// </returns>
    public static TTo As<TFrom, TTo>(TFrom @interface) where TTo : class => @interface as TTo;
}
