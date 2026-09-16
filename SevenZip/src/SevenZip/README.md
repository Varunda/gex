# SevenZip

The SevenZip library for .NET provides classes to create, modify and extract different kinds
of archives by using the native 7z library from Igor Pavlov. Additionally, it contains the
original SDK code which allows compressing and decompressing binary data using the LZMA
method.

The library is written in C# and supports .NET 4.8 and .NET6 on Windows, and .NET 8
on Windows and Linux.

## License information

MIT

The following binaries embedded in the NuGet package are licensed under the 
[GNU LGPL, GNU LGPL with 'unRAR license restrictions' and BSD 3-clause license](https://github.com/exocad/SevenZipSharpSimple/tree/main/runtimes/LICENSE):

- 7z.dll
- 7z.so

The files located in `src/SevenZip/CoreSdk` were published under [public domain](https://github.com/exocad/SevenZipSharpSimple/tree/main/src/SevenZip/CoreSdk/LICENSE).
All other files are licensed with the [MIT license](https://github.com/exocad/SevenZipSharpSimple/tree/main/LICENSE).

### References

- Original [SDK code](https://7-zip.org/sdk.html) for LZMA compression.
- The UUIDs for the COM interfaces were taken from the [7-Zip C/C++ library](https://github.com/mcmilk/7-Zip/tree/master).
- The format detection is inspired by the original [SevenZipSharp](https://github.com/tomap/SevenZipSharp/tree/master) library,
  which has been archived, but has some forks which are still being maintained.
  - Most signatures can be obtained from [Wikipedia - List of file signatures](https://en.wikipedia.org/wiki/List_of_file_signatures).

## Limitations

- Multi-part archives are not supported. If there is a requirement, this may be added in
  a future release.
- Creation of executable archives is not supported.
- Not all format specific compression options like encryption can be configured via properties. However, it is possible
  to pass the command line options like "he" and/or "em" (for Zip archives only) to the `CompressProperties` class.
  The parameters can be found in [this manual](https://7zip.bugaco.com/7zip/MANUAL/cmdline/switches/method.htm).

## Linux Support

The .NET 8 builds of `SevenZip` can run under Linux. The native 7z Linux
library was build from the original repository at https://github.com/mcmilk/7-Zip
by using the following commands:

```bash
git clone https://github.com/mcmilk/7-Zip.git
cd 7-zip/CPP/7zip/Bundles/Format7zF
make -f makefile.gcc
```

The different bundles and the formats they support are explained here:
https://github.com/mcmilk/7-Zip/blob/master/DOC/readme.txt.

## Referencing the library

### Project reference

Clone the project into any location and add a reference to it.

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\SevenZip\SevenZip.csproj" />
</ItemGroup>
```

The native 7z library is part of this repository, but not linked directly by the `SevenZip`
project. This can be done by importing the `Native7zReferences.target` file in a `.csproj`, or with
the following snippet:

```xml
  <ItemGroup>
    <None Include="..\..\runtimes\win\native\7z.dll" Link="7z.dll" CopyToOutputDirectory="PreserveNewest" />
    <None Include="..\..\runtimes\linux\native\7z.so" Link="7z.so" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

The path to the native library can however be specified by setting the `ArchiveConfig.NativeLibraryPath`
property, which is then passed to an `ArchiveReader` or `ArchiveWriter`.

#### NuGet reference

> A public package is not available. It is currently only hosted on an internal server.

To reference the package, add the following snippet to the `.csproj` file. When using
central package management, omit the `Version` attribute.

```xml
<ItemGroup>
  <PackageReference Include="Exocad.SevenZip" Version="1.0.0" />
</ItemGroup>
```

The NuGet package includes the native libraries, a separate reference is not required.

## Usage

### Extracting an archive

The `ArchiveReader` can extract specific files only or an entire archive. It operates
with `System.IO.Stream`, which allows extracting to memory, files or anything else that 
can be represented as stream.

A stream to use for extraction is obtained by invoking a callback passed
to the `Extract` method, which has to return the stream to use for a given entry.

The easiest way to extract an entire archive is to use the `ExtractAll` method:

```csharp
using SevenZip

using var reader = new ArchiveReader("archive.7z");

reader.ExtractAll(targetDir, ArchiveFlags.ApplyArchiveEntryTimestampsToFileStreams);
```

To extract a single entry (or specific entries), the `Extract` methods can be used
with the indices of the entries to extract. The index of an entry can be found by 
iterating over the `ArchiveReader.Entries` property.

The following snippet shows how to extract a set of entries:

```csharp
using SevenZip

bool CanExtract(ArchiveEntry entry)
{
  return ...
}

using var reader = new ArchiveReader("archive.7z");

var indices = reader.Entries
  .Where(entry => CanExtract(entry))
  .Select(entry => entry.Index);

reader.Extract(
  new IndexList(indices),
  entry => File.Open(Path.Join(baseDir, entry.Path), FileMode.Create, FileAccess.Write),
  ArchiveFlags.ApplyArchiveEntryTimestampsToFileStreams |
    ArchiveFlags.CloseArchiveEntryStreamAfterExtraction);
```

### Creating or modifying an archive

The `ArchiveWriter` class can be used to create or update an archive. If the class is initialized
with a constructor expecting an `ArchiveFormat` parameter, a new archive is created - even if an
archive already exists at the given location!

To update an existing archive, choose a constructor not expecting the `ArchiveFormat`. The `ArchiveWriter`
will then detect the format from the existing archive (or throws an exception if it can't).

When adding new files to an archive an archive-path must be specified indicating the full
path the new entry should have within the archive.

```csharp
using SevenZip;
using SevenZip.Extensions;

// Create a new archive (or override an existing one)
using var writer = new ArchiveWriter(ArchiveFormat.SevenZip, "archive.7z");

// Add "./directory/filename.txt" as "archive/filename.txt" to the archive.
writer.AddFile("archive/filename.txt", "./directory/filename.txt");

// Add all files from "otherdir" to the archive, using a specific naming strategy.
writer.AddDirectory("./otherdir", NamingStrategy.RelativeToTopDirectoryInclusive);

// Write the changes
writer.Compress();
```

To modify or remove existing entries, use the `ArchiveWriter.ExistingEntries` property to
search for the index of the entry to update or delete. `ArchiveWriter.ReplaceEntry`
and `ArchiveWriter.DeleteEntry` can then be used to prepare the changes.

To actually write the changes, call `ArchiveWriter.Compress`.

```csharp
using SevenZip;
using SevenZip.Extensions;

// Create a new archive (or override an existing one)
using var writer = new ArchiveWriter("archive.7z");

// Remove entry with index 10.
writer.RemoveEntry(10);

// Replace entry 11 with the contents of another file.
writer.ReplaceEntry(11, "./contents/anotherfile.txt");

// Write the changes
writer.Compress();
```
| Important |
| :--- |
| If an existing archive is modified, a local copy of the original data is stored in a temporary file which will be deleted again afterwards when calling `Compress`. This is required since simultaneous read and write operations are not supported. |

Once `Compress` has been called, the `ExistingEntries` properties is being updated. In most scenarios,
the method should only be called once and the `ArchiveWriter` should then be disposed again. However,
for streams supporting both read and write operations, it may be called multiple times.

