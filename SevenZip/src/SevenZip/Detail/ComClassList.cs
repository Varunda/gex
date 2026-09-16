using System;
using System.Collections.Generic;

namespace SevenZip.Detail;

/// <summary>
/// A collection of known class UUIDs for archive writer and reader objects.
/// </summary>
/// <remarks>
/// The UUIDs can be found in the original 7Z code:
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Guid.txt#L164
/// </remarks>
internal static class ComClassList
{
    private static readonly IDictionary<ArchiveFormat, Guid> _map = new Dictionary<ArchiveFormat, Guid>()
    {
        {
            ArchiveFormat.SevenZip,
            new Guid("23170f69-40c1-278a-1000-000110070000")
        },
        {
            ArchiveFormat.Arj,
            new Guid("23170f69-40c1-278a-1000-000110040000")
        },
        {
            ArchiveFormat.BZip2,
            new Guid("23170f69-40c1-278a-1000-000110020000")
        },
        {
            ArchiveFormat.Cab,
            new Guid("23170f69-40c1-278a-1000-000110080000")
        },
        {
            ArchiveFormat.Chm,
            new Guid("23170f69-40c1-278a-1000-000110e90000")
        },
        {
            ArchiveFormat.Compound,
            new Guid("23170f69-40c1-278a-1000-000110e50000")
        },
        {
            ArchiveFormat.Cpio,
            new Guid("23170f69-40c1-278a-1000-000110ed0000")
        },
        {
            ArchiveFormat.Deb,
            new Guid("23170f69-40c1-278a-1000-000110ec0000")
        },
        {
            ArchiveFormat.GZip,
            new Guid("23170f69-40c1-278a-1000-000110ef0000")
        },
        {
            ArchiveFormat.Iso,
            new Guid("23170f69-40c1-278a-1000-000110e70000")
        },
        {
            ArchiveFormat.Lzh,
            new Guid("23170f69-40c1-278a-1000-000110060000")
        },
        {
            ArchiveFormat.Lzma,
            new Guid("23170f69-40c1-278a-1000-0001100a0000")
        },
        {
            ArchiveFormat.Nsis,
            new Guid("23170f69-40c1-278a-1000-000110090000")
        },
        {
            ArchiveFormat.Rar,
            new Guid("23170f69-40c1-278a-1000-000110030000")
        },
        {
            ArchiveFormat.Rpm,
            new Guid("23170f69-40c1-278a-1000-000110eb0000")
        },
        {
            ArchiveFormat.Split,
            new Guid("23170f69-40c1-278a-1000-000110ea0000")
        },
        {
            ArchiveFormat.Tar,
            new Guid("23170f69-40c1-278a-1000-000110ee0000")
        },
        {
            ArchiveFormat.Wim,
            new Guid("23170f69-40c1-278a-1000-000110e60000")
        },
        {
            ArchiveFormat.Lzw,
            new Guid("23170f69-40c1-278a-1000-000110050000")
        },
        {
            ArchiveFormat.Zip,
            new Guid("23170f69-40c1-278a-1000-000110010000")
        },
        {
            ArchiveFormat.Xar,
            new Guid("23170f69-40c1-278a-1000-000110E10000")
        },
        {
            ArchiveFormat.Hfs,
            new Guid("23170f69-40c1-278a-1000-000110E30000")
        },
        {
            ArchiveFormat.Dmg,
            new Guid("23170f69-40c1-278a-1000-000110E40000")
        },
        {
            ArchiveFormat.Xz,
            new Guid("23170f69-40c1-278a-1000-0001100C0000")
        },
        {
            ArchiveFormat.Mslz,
            new Guid("23170f69-40c1-278a-1000-000110D50000")
        },
        {
            ArchiveFormat.Pe,
            new Guid("23170f69-40c1-278a-1000-000110DD0000")
        },
        {
            ArchiveFormat.Elf,
            new Guid("23170f69-40c1-278a-1000-000110DE0000")
        },
        {
            ArchiveFormat.Swf,
            new Guid("23170f69-40c1-278a-1000-000110D70000")
        },
        {
            ArchiveFormat.Vhd,
            new Guid("23170f69-40c1-278a-1000-000110DC0000")
        }
    };

    /// <summary>
    /// Gets the <see cref="Guid"/> to use when creating an archive reader or writer interface
    /// for a given format.
    /// </summary>
    /// <param name="format">
    /// The <see cref="ArchiveFormat"/> to create an COM instance for.
    /// </param>
    /// <returns>
    /// The corresponding UUID.
    /// </returns>
    public static Guid GetClassId(ArchiveFormat format) => _map[format];
}