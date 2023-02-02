using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.System.Ioctl;
using Windows.Win32.Storage.FileSystem;
using NtfsIndexer;
using System.Runtime.Versioning;

namespace Windows.Win32.System.Ioctl
{
    internal partial struct USN_RECORD_V2 : IUsnRecord
    {
        uint IUsnRecord.RecordLength => RecordLength;
        FileAttributes IUsnRecord.FileAttributes => (FileAttributes)FileAttributes;
        ushort IUsnRecord.FileNameOffset => FileNameOffset;
        ushort IUsnRecord.FileNameLength => FileNameLength;
        FILE_ID_TYPE IUsnRecord.FileIdType => FILE_ID_TYPE.FileIdType;
        FILE_ID_DESCRIPTOR._Anonymous_e__Union IUsnRecord.FileIdentifier => new() { FileId = (long)FileReferenceNumber };
        FILE_ID_DESCRIPTOR._Anonymous_e__Union IUsnRecord.ParentFileIdentifier => new() { FileId = (long)ParentFileReferenceNumber };
    }

    internal partial struct USN_RECORD_V3 : IUsnRecord
    {
        uint IUsnRecord.RecordLength => RecordLength;
        FileAttributes IUsnRecord.FileAttributes => (FileAttributes)FileAttributes;
        ushort IUsnRecord.FileNameOffset => FileNameOffset;
        ushort IUsnRecord.FileNameLength => FileNameLength;
        FILE_ID_TYPE IUsnRecord.FileIdType => FILE_ID_TYPE.ExtendedFileIdType;
        FILE_ID_DESCRIPTOR._Anonymous_e__Union IUsnRecord.FileIdentifier => new() { ExtendedFileId = FileReferenceNumber };
        FILE_ID_DESCRIPTOR._Anonymous_e__Union IUsnRecord.ParentFileIdentifier => new() { ExtendedFileId = ParentFileReferenceNumber };
    }
}

namespace NtfsIndexer
{
    internal static partial class Win32
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

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetFileInformationByHandleEx(SafeHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, out FILE_ID_INFO lpFileInformation, uint dwBufferSize);

    }
}