using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Storage.FileSystem;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace NtfsIndexer;

public class Win32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern SafeFileHandle OpenFileById(SafeFileHandle volumeHandle,
        ref FILE_ID_DESCRIPTOR lpFileId,
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        uint lpSecurityAttributes,
        uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetFileInformationByHandle(SafeFileHandle hFile,
        out BY_HANDLE_FILE_INFORMATION lpFileInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer, int nInBufferSize,
            out USN_JOURNAL_DATA_V2 lpOutBuffer, int nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer, int nInBufferSize,
        out ReparsePoints.REPARSE_DATA_BUFFER lpOutBuffer, int nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(SafeFileHandle hDevice,
        uint dwIoControlCode,
        SafeHandle lpInBuffer, int nInBufferSize,
        IntPtr lpOutBuffer, int nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer, int nInBufferSize,
        IntPtr lpOutBuffer, int nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll")]
    public static extern void ZeroMemory(IntPtr ptr, int size);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public FILETIME CreationTime;
        public FILETIME LastAccessTime;
        public FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USN_JOURNAL_DATA_V2
    {
        public ulong UsnJournalID;
        public long FirstUsn;
        public long NextUsn;
        public long LowestValidUsn;
        public long MaxUsn;
        public ulong MaximumSize;
        public ulong AllocationDelta;
        public ushort MinSupportedMajorVersion;
        public ushort MaxSupportedMajorVersion;
        public uint Flags;
        public ulong RangeTrackChunkSize;
        public long RangeTrackFileSizeThreshold;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct MFT_ENUM_DATA_V1
    {
        public ulong StartFileReferenceNumber;
        public long LowUsn;
        public long HighUsn;
        public ushort MinMajorVersion;
        public ushort MaxMajorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_USN_JOURNAL_DATA
    {
        public ulong MaximumSize;
        public ulong AllocationDelta;
    }


    public enum FILE_ID_TYPE
    {
        FileIdType = 0,
        ObjectIdType = 1,
        ExtendedFileIdType = 2
    }

    public struct UsnRecord
    {
        public uint RecordLength;
        public short MajorVersion;
        public short MinorVersion;
        public long FileReferenceNumber;
        public long ParentFileReferenceNumber;
        public FileAttributes FileAttributes;

        public int FileNameLength;
        public int FileNameOffset;
        public string FileName;

        private const int FR_OFFSET = 8;
        private const int PFR_OFFSET = 16;
        private const int FA_OFFSET = 52;
        private const int FNL_OFFSET = 56;
        private const int FN_OFFSET = 58;

        public UsnRecord(IntPtr p, int offset)
        {
            RecordLength = (uint)Marshal.ReadInt32(p, offset);
            MajorVersion = Marshal.ReadInt16(p, offset + 4);
            MinorVersion = Marshal.ReadInt16(p, offset + 6);
            FileReferenceNumber = Marshal.ReadInt64(p, offset + FR_OFFSET);
            ParentFileReferenceNumber = Marshal.ReadInt64(p, offset + PFR_OFFSET);
            FileAttributes = (FileAttributes)Marshal.ReadInt32(p, offset + FA_OFFSET);
            FileNameLength = Marshal.ReadInt16(p, offset + FNL_OFFSET);
            FileNameOffset = Marshal.ReadInt16(p, offset + FN_OFFSET);
            FileName = Marshal.PtrToStringUni(new IntPtr(p.ToInt64() + offset + FileNameOffset), FileNameLength / sizeof(char));
        }
    }
}