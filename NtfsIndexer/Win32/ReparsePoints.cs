using System.Runtime.InteropServices;
using System.Text;

namespace NtfsIndexer;

public class ReparsePoints
{
    [StructLayout(LayoutKind.Sequential)]
    public struct REPARSE_GUID_DATA_BUFFER
    {
        /// <summary>
        /// Reparse point tag. Must be a Microsoft reparse point tag.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public ReparseTagType ReparseTag;

        public ushort ReparseDataLength;

        /// <summary>
        /// Reserved; do not use. 
        /// </summary>
        public ushort Reserved;

        public Guid ReparseGuid;

        /// <summary>
        /// A buffer containing the unicode-encoded path string. The path string contains
        /// the substitute name string and print name string.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
        public byte[] PathBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct REPARSE_DATA_BUFFER : IReparsePointData
    {
        /// <summary>
        /// Reparse point tag. Must be a Microsoft reparse point tag.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public ReparseTagType ReparseTag;

        /// <summary>
        /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
        /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength + 
        /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
        /// </summary>
        public ushort ReparseDataLength;

        /// <summary>
        /// Reserved; do not use. 
        /// </summary>
        public ushort Reserved;

        /// <summary>
        /// Offset, in bytes, of the substitute name string in the PathBuffer array.
        /// </summary>
        public ushort SubstituteNameOffset;

        /// <summary>
        /// Length, in bytes, of the substitute name string. If this string is null-terminated,
        /// SubstituteNameLength does not include space for the null character.
        /// </summary>
        public ushort SubstituteNameLength;

        /// <summary>
        /// Offset, in bytes, of the print name string in the PathBuffer array.
        /// </summary>
        public ushort PrintNameOffset;

        /// <summary>
        /// Length, in bytes, of the print name string. If this string is null-terminated,
        /// PrintNameLength does not include space for the null character. 
        /// </summary>
        public ushort PrintNameLength;

        /// <summary>
        /// A buffer containing the unicode-encoded path string. The path string contains
        /// the substitute name string and print name string.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
        public byte[] PathBuffer;

        public string SubstituteName =>
            Encoding.Unicode.GetString(PathBuffer,
                SubstituteNameOffset, SubstituteNameLength);

        public string PrintName =>
            Encoding.Unicode.GetString(PathBuffer,
                PrintNameOffset, PrintNameLength);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct REPARSE_DATA_SYMLINK_BUFFER : IReparsePointData
    {
        /// <summary>
        /// Reparse point tag. Must be a Microsoft reparse point tag.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public ReparseTagType ReparseTag;

        /// <summary>
        /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
        /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength + 
        /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
        /// </summary>
        public ushort ReparseDataLength;

        /// <summary>
        /// Reserved; do not use. 
        /// </summary>
        public ushort Reserved;

        /// <summary>
        /// Offset, in bytes, of the substitute name string in the PathBuffer array.
        /// </summary>
        public ushort SubstituteNameOffset;

        /// <summary>
        /// Length, in bytes, of the substitute name string. If this string is null-terminated,
        /// SubstituteNameLength does not include space for the null character.
        /// </summary>
        public ushort SubstituteNameLength;

        /// <summary>
        /// Offset, in bytes, of the print name string in the PathBuffer array.
        /// </summary>
        public ushort PrintNameOffset;

        /// <summary>
        /// Length, in bytes, of the print name string. If this string is null-terminated,
        /// PrintNameLength does not include space for the null character. 
        /// </summary>
        public ushort PrintNameLength;

        /// <summary>
        /// Symbolic links flags
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public SymLinkFlags Flags;

        /// <summary>
        /// A buffer containing the unicode-encoded path string. The path string contains
        /// the substitute name string and print name string.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
        public byte[] PathBuffer;

        public string SubstituteName =>
            Encoding.Unicode.GetString(PathBuffer,
                SubstituteNameOffset, SubstituteNameLength);

        public string PrintName =>
            Encoding.Unicode.GetString(PathBuffer,
                PrintNameOffset, PrintNameLength);
    }
}

public interface IReparsePointData
{
    string SubstituteName { get; }
    string PrintName { get; }
}

[Flags]
public enum SymLinkFlags : uint
{
    Absolute = 0,
    Relative = 1
}