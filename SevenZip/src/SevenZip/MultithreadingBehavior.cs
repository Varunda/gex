namespace SevenZip;

/// <summary>
/// The <see cref="MultithreadingBehavior"/> can be used to configure whether to use multithreading
/// or not. Multithreading is not supported by all formats and primarily used for compression.
/// 
/// The default is <see cref="MultithreadingBehavior.Enabled"/>, which lets 7z decide how many threads
/// to create. Use <see cref="Disabled"/> to disabled multithreading and
/// <see cref="WithExplicitThreadCount(int)"/> to set the amount of threads to use manually.
/// </summary>
public readonly struct MultithreadingBehavior
{
    private readonly uint? _value;

    private MultithreadingBehavior(uint? value) => _value = value;

    /// <summary>
    /// Gets the instance which is used to disable multithreading.
    /// </summary>
    public static MultithreadingBehavior Disabled { get; } = new MultithreadingBehavior(null);

    /// <summary>
    /// Gets the instance which is used to enable multithreading and let 7z decide
    /// how many threads to use.
    /// </summary>
    public static MultithreadingBehavior Enabled { get; } = new MultithreadingBehavior(0);

    /// <summary>
    /// Creates a new instance of the <see cref="MultithreadingBehavior"/> struct with an
    /// explicit amount of threads to use.
    /// </summary>
    /// <param name="value">
    /// The number of threads 7z may use. A value of zero lets 7z decide how many threads to use.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="MultithreadingBehavior"/> struct.
    /// </returns>
    public static MultithreadingBehavior WithExplicitThreadCount(uint value) => new MultithreadingBehavior(value);

    internal uint? Value => _value;
}