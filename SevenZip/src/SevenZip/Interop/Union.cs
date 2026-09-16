using System;
using System.Runtime.InteropServices;
using SevenZip.Detail;

namespace SevenZip.Interop;

/// <summary>
/// Representation of a union that can store different value types.
/// </summary>
/// <remarks>
/// The struct has a size of 20 bytes since the native structure
/// additionally contains space to store an array pointer and its length,
/// which is not used here.
/// Unfortunately the size of this structure varies between the Windows and
/// the Linux version of the 7z library. On Windows, even 24 bytes are used
/// now. In most scenarios the size is of less importance since only single
/// elements are being transferred when calling native 7z functions.
/// But when passing an array of <see cref="Union"/> instances, which is 
/// required to apply archive properties (See <see cref="IArchiveProperties"/>),
/// the per element-size must be taken into account.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 20)]
internal readonly struct Union
{
    [FieldOffset(0)]
    private readonly ushort _type;
        
    [FieldOffset(8)]
    private readonly IntPtr _pointer;
        
    [FieldOffset(8)]
    private readonly int _int32;
        
    [FieldOffset(8)]
    private readonly long _int64;
        
    [FieldOffset(8)]
    private readonly uint _uint32;
        
    [FieldOffset(8)]
    private readonly ulong _uint64;

    /// <summary>
    /// Gets the size the native structure occupies on the current platform,
    /// in bytes.
    /// </summary>
    public static int Size
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return 24;
            };

            return 20;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Union"/> struct with all members
    /// set to zero.
    /// </summary>
    public Union() => (_type, _pointer, _int32, _int64, _uint32, _uint64) = (0, IntPtr.Zero, 0, 0, 0, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="Union"/> struct with a given <paramref name="type"/>
    /// and a pointer.
    /// </summary>
    /// <param name="type">The type the <see cref="Union"/> represents.</param>
    /// <param name="pointer">The value to store.</param>
    private Union(VarEnum type, IntPtr pointer) : this() => (_type, _pointer) = ((ushort)type, pointer);

    /// <summary>
    /// Initializes a new instance of the <see cref="Union"/> struct with a given <paramref name="type"/>
    /// and a 32 bit signed integer value.
    /// </summary>
    /// <param name="type">The type the <see cref="Union"/> represents.</param>
    /// <param name="value">The value to store.</param>
    private Union(VarEnum type, int value) : this() => (_type, _int32) = ((ushort)type, value);

    /// <summary>
    /// Initializes a new instance of the <see cref="Union"/> struct with a given <paramref name="type"/>
    /// and a 64 bit signed integer value.
    /// </summary>
    /// <param name="type">The type the <see cref="Union"/> represents.</param>
    /// <param name="value">The value to store.</param>
    private Union(VarEnum type, long value) : this() => (_type, _int64) = ((ushort)type, value);

    /// <summary>
    /// Initializes a new instance of the <see cref="Union"/> struct with a given <paramref name="type"/>
    /// and a 32 bit unsigned integer value.
    /// </summary>
    /// <param name="type">The type the <see cref="Union"/> represents.</param>
    /// <param name="value">The value to store.</param>
    private Union(VarEnum type, uint value) : this() => (_type, _uint32) = ((ushort)type, value);

    /// <summary>
    /// Initializes a new instance of the <see cref="Union"/> struct with a given <paramref name="type"/>
    /// and a 64 bit unsigned integer value.
    /// </summary>
    /// <param name="type">The type the <see cref="Union"/> represents.</param>
    /// <param name="value">The value to store.</param>
    private Union(VarEnum type, ulong value) : this() => (_type, _uint64) = ((ushort)type, value);
        
    /// <summary>
    /// Gets the union type.
    /// </summary>
    public VarEnum Type => (VarEnum)_type;

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_BOOL"/> and the given value.
    /// </summary>
    /// <param name="value">The value to assign to the union.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(bool value) => new Union(VarEnum.VT_BOOL, value ? 1ul : 0ul);

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_FILETIME"/> and the given value.
    /// </summary>
    /// <param name="value">The value to assign to the union, as local filetime.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(DateTime value) => new Union(VarEnum.VT_FILETIME, value.ToFileTime());

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_I4"/> and the given value.
    /// </summary>
    /// <param name="value">The value to assign to the union.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(int value) => new Union(VarEnum.VT_I4, value);

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_I8"/> and the given value.
    /// </summary>
    /// <param name="value">The value to assign to the union.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(long value) => new Union(VarEnum.VT_I8, value);

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_UI4"/> and the given value.
    /// </summary>
    /// <param name="value">The value to assign to the union.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(uint value) => new Union(VarEnum.VT_UI4, value);

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_UI8"/> and the given value.
    /// </summary>
    /// <param name="value">The value to assign to the union.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(ulong value) => new Union(VarEnum.VT_UI8, value);

    /// <summary>
    /// Creates a new <see cref="Union"/> with <see cref="VarEnum.VT_BSTR"/> and the given value.
    /// 
    /// This method allocates native memory which must be freed once no longer used.
    /// </summary>
    /// <param name="value">The value to assign to the union.</param>
    /// <returns>The initialized union.</returns>
    public static Union Create(string value) => new Union(
        VarEnum.VT_BSTR,
        StringMarshal.ManagedStringToBinaryString(value));

    /// <summary>
    /// Gets the underlying value as string. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_BSTR"/>, <see cref="VarEnum.VT_EMPTY"/>
    /// or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying value as managed string or <see cref="string.Empty"/>,
    /// if the value is not a string.</returns>
    public string AsString() => Type switch
    {
        VarEnum.VT_BSTR => StringMarshal.BinaryStringToManagedString(_pointer),
        _ => string.Empty
    };
        
    /// <summary>
    /// Gets the underlying value as <see cref="DateTime"/>. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_FILETIME"/>, <see cref="VarEnum.VT_EMPTY"/>
    /// or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying filetime as <see cref="DateTime"/>. If the <see cref="Type"/>
    /// is not compatible, <see cref="DateTime.Now"/> is being returned.</returns>
    public DateTime AsDateTime() => Type switch
    {
        VarEnum.VT_FILETIME or VarEnum.VT_I8 or VarEnum.VT_UI8 => _int64 > 0 ? DateTime.FromFileTime(_int64) : DateTime.MinValue,
        _ => DateTime.UtcNow
    };

    /// <summary>
    /// Gets the underlying value as 64 bit signed integer. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_I4"/>, <see cref="VarEnum.VT_I8"/>,
    /// <see cref="VarEnum.VT_UI4"/>, <see cref="VarEnum.VT_UI8"/>, <see cref="VarEnum.VT_EMPTY"/>
    /// or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying value as 64 bit signed integer or 0, if the underlying
    /// value is not a number.</returns>
    public long AsInt64() => Type switch
    {
        VarEnum.VT_EMPTY => 0L,
        VarEnum.VT_NULL => 0L,
        VarEnum.VT_I4 => _int32,
        VarEnum.VT_I8 => _int64,
        VarEnum.VT_UI4 => _uint32,
        VarEnum.VT_UI8 => (long) _uint64,
        _ => 0L
    };

    /// <summary>
    /// Gets the underlying value as 64 bit unsigned integer. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_I4"/>, <see cref="VarEnum.VT_I8"/>,
    /// <see cref="VarEnum.VT_UI4"/>, <see cref="VarEnum.VT_UI8"/>, <see cref="VarEnum.VT_EMPTY"/>
    /// or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying value as 64 bit unsigned integer or 0, if the underlying
    /// type is not a number.</returns>
    public ulong AsUInt64() => Type switch
    {
        VarEnum.VT_I4 => (ulong) _int32,
        VarEnum.VT_I8 => (ulong) _int64,
        VarEnum.VT_UI4 => _uint32,
        VarEnum.VT_UI8 => _uint64,
        _ => 0UL
    };

    /// <summary>
    /// Gets the underlying value as 32 bit unsigned integer. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_I4"/>, <see cref="VarEnum.VT_I8"/>,
    /// <see cref="VarEnum.VT_UI4"/>, <see cref="VarEnum.VT_UI8"/>, <see cref="VarEnum.VT_EMPTY"/>
    /// or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying value as 32 bit unsigned integer.</returns>
    public uint AsUInt32() => (uint)AsUInt64();

    /// <summary>
    /// Gets the underlying value as 32 bit signed integer. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_I4"/>, <see cref="VarEnum.VT_I8"/>,
    /// <see cref="VarEnum.VT_UI4"/>, <see cref="VarEnum.VT_UI8"/>, <see cref="VarEnum.VT_EMPTY"/>
    /// or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying value as 32 bit signed integer.</returns>
    public int AsInt32() => (int)AsInt64();
        
    /// <summary>
    /// Gets the underlying value as boolean value. This method must only be called if
    /// the type is one of <see cref="VarEnum.VT_BOOL"/>, <see cref="VarEnum.VT_I4"/>,
    /// <see cref="VarEnum.VT_I8"/>, <see cref="VarEnum.VT_UI4"/>, <see cref="VarEnum.VT_UI8"/>,
    /// <see cref="VarEnum.VT_EMPTY"/> or <see cref="VarEnum.VT_NULL"/>.
    /// </summary>
    /// <returns>The underlying value as boolean value.</returns>
    public bool AsBool() => _uint64 != 0ul;

    /// <summary>
    /// Frees unmanaged memory, if there is any.
    /// </summary>
    public void Free()
    {
        switch (Type)
        {
            case VarEnum.VT_BSTR:
                StringMarshal.BinaryStringFree(_pointer);
                break;
        }
    }
}