namespace SevenZip;

/// <summary>
/// The delegate represents a method returning a full path to a temporary file,
/// which can be used to store temporary data.
/// </summary>
/// <returns>The full path to a temporary file.</returns>
public delegate string GetTempFileName();
