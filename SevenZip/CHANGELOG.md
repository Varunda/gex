# SevenZipSharp Changelog

## [2.3.0] - 2026-05-27

### Changed

- Added .NET 10 as target framework and removed .NET 6 as target framework.
- `ArchiveReader` and `ArchiveWriter` now both provide a property named `UncompressedArchiveSize`,
   which returns the total size of the archive content after it has been extracted.
- `InMemoryArchiveRepacker` and the `BulkExtract` extension now support two new flags / options:
  - `DisposeArchiveEntryStreamAfterUse` specifies whether streams should be disposed automatically after use.
  - `UseFileStreamForOversizedArchiveEntries` specifies that archive entries exceeding the supported size of
    .NET arrays will use a temporary file to process the entry instead of throwing an exception. This option
    is only available in .NET Core.
  - These new flags are disabled by default to preserve the known behavior.
- `InMemoryArchiveRepacker` can now also transfer directory entries.
  - To omit any entry, the given filter callback can now return `null`.
- `BulkExtract` now provides an overload accepting a generic state parameter, which is passed to the
  `consumeExtractResults` callback.
- The `ArchiveConfig` contains a delegate to customize the method to use for creating temporary files. By
  default, `Path.GetTempFileName` is used.

## [2.2.0] - 2025-10-10

### Fixed

- Removing entries from an archive led to incorrect index values when the total amount of
  archive entries changed. This sometimes resulted in an exception within the 7z library.

## [2.1.1] - 2025-02-27

### Changed

- Encrypted archives are now also supported on Linux. The PasswordProvider interfaces now pass a
  native pointer instead of a marshalled string which allows allocating the string the way the
  used implementation needs it, based on the current OS.
- An exception is now thrown when applying the `CompressProperties` fails.
- An empty string can now also be specified as password to use encryption.

## [2.1.0] - 2025-02-18

### Changed

- Interop interface methods which are being invoked from the native library and which used a managed array as parameter
  (like `byte[]`) now use raw pointers to avoid a memory allocation per interop call. 
  This can result in a significantly reduced garbage collector pressure especially when compressing several
  files and therefore improves the overall performance.
- The 7z multithreading option can be configured via the `MultithreadingBehavior` property of the `CompressProperties` class.

### Fixed

- In some cases, the properties configured by the `CompressProperties` class were not applied correctly. This
  should now be resolved.

## [2.0.1] - 2025-01-16

### Changed

- Added a `EncryptionMethod` property to the `CompressProperties` class to simplify creating encrypted archives.
- The `ApplyArchiveEntryTimestampsToFileStreams` flag no longer updates a files' timestamp when the archive entry reports a timestamp of `DateTime.MinValue`.

### Fixed

- The .NET8 build now correctly applies custom compression properties. This operation failed due to a missing `GeneratedComInterface`
  attribute.

## [2.0.0] - 2025-01-14

### Changed

- The major version has been changed to 2 due to changes in the public API of `ArchiveReader`, `IProgressDelete`, `IArchiveReaderDelegate` and `IArchiveWriterDelegate`
  and a behavioral change in the error handling during extract or compress operations (see `ArchiveConfig.IgnoreOperationErrors`).
- Unified and slightly changed the public interface of `ArchiveReader` so that it now has a single public `Extract`
  method while all overloads were moved an an extension class.
- By default, errors which occur during compression or extraction now cause an exception of type `ArchiveOperationException`.
  This behavior can be controlled with the `ArchiveConfig.IgnoreOperationErrors` property, which defaults to `false`.
- The delegate interfaces `IProgressDelete`, `IArchiveReaderDelegate` and `IArchiveWriterDelegate` have been
  extended with another parameter (`IExtractContext` or `ICompressContext`), which can be used to provide
  more information about the current operation.

## [1.0.3] - 2025-01-07

### Fixed

- Fixed a NullReferenceException caused by calling `ArchiveReader.CanExtractEntries`.

## [1.0.2] - 2024-12-10

### Changed

- Added a guard to prevent the [directory traversal attack (CVE-2024-48510)](https://github.com/advisories/GHSA-xhg6-9j5j-w4vf).

## [1.0.1] - 2024-11-06

### Changed

- The generated assembly now always has a strong-name.

## [1.0.0] - 2024-05-03

### Changed

- Initial release of the .NET SevenZip library.
