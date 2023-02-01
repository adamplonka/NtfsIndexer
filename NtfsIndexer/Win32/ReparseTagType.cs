using System.Runtime.CompilerServices;

namespace NtfsIndexer;

public enum ReparseTagType : uint
{
    IoReparseTagMountPoint = 0xA0000003,
    IoReparseTagHsm = 0xC0000004,
    IoReparseTagDriveExtender = 0x80000005,
    IoReparseTagHsm2 = 0x80000006,
    IoReparseTagSis = 0x80000007,
    IoReparseTagWim = 0x80000008,
    IoReparseTagCsv = 0x80000009,
    IoReparseTagDfs = 0x8000000A,
    IoReparseTagFilterManager = 0x8000000B,
    IoReparseTagSymlink = 0xA000000C,
    IoReparseTagIisCache = 0xA0000010,
    IoReparseTagDfsr = 0x80000012,
    IoReparseTagDedup = 0x80000013,
    IoReparseTagAppxstrm = 0xC0000014,
    IoReparseTagNfs = 0x80000014,
    IoReparseTagFilePlaceholder = 0x80000015,
    IoReparseTagDfm = 0x80000016,
    IoReparseTagWof = 0x80000017,
    IoReparseTagWci = 0x80000018,
    IoReparseTagGvfs = 0x9000001C,
    IoReparseTagLxSymlink = 0xA000001D
}

public static class ReparseTagTypeExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReparseTagMicrosoft(this ReparseTagType tag)
    {
        return ((uint)tag & 0x80000000) != 0;
    }


    /// <summary>
    /// Macro to determine whether a reparse point tag is a name surrogate
    ///</summary>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReparseTagNameSurrogate(this ReparseTagType tag)
    {
        return ((uint)tag & 0x20000000) != 0;
    }

    /// <summary>
    /// The following constant represents the bits that are valid to use in
    /// reparse tags.
    /// </summary>
    private const uint IO_REPARSE_TAG_VALID_VALUES = 0xF000FFFF;

    private const uint IO_REPARSE_TAG_RESERVED_RANGE = 1;

    /// <summary>
    /// Macro to determine whether a reparse tag is a valid tag.
    /// </summary>
    public static bool IsReparseTagValid(this ReparseTagType tag)
    {
        return ((uint)tag & ~IO_REPARSE_TAG_VALID_VALUES) == 0 &&
               (uint)tag > IO_REPARSE_TAG_RESERVED_RANGE;
    }
}