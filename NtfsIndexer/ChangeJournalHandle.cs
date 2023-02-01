using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ChangeJournal;

public class ChangeJournalHandle : SafeHandleMinusOneIsInvalid
{

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(
        [MarshalAs(UnmanagedType.LPWStr)]
        string FileName,
        int DesiredAccess,
        FileShare ShareMode,
        IntPtr SecurityAttributes,
        int CreationDisposition,
        int FlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetVolumeInformationByHandleW(
        IntPtr hFile,
        StringBuilder lpVolumeNameBuffer,
        int nVolumeNameSize,
        out int lpVolumeSerialNumber,
        out int
            lpMaximumComponentLength,
        out int lpFileSystemFlags,
        StringBuilder lpFileSystemNameBuffer,
        int nFileSystemNameSize
    );



    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DeviceIoControl(
        IntPtr hDevice,
        int dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        IntPtr lpOverlapped
    );

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool CloseHandle(
        IntPtr handle);


    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr OpenFileById(
        IntPtr hFile,
        ref FILE_ID_DESCRIPTOR lpFileID,
        int dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        int dwFlags
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetFinalPathNameByHandleW(
        IntPtr hFile,
        StringBuilder lpszFilePath,
        int cchFilePath,
        int dwFlags
    );

    [StructLayout(LayoutKind.Explicit)]
    public struct FILE_ID_DESCRIPTOR
    {
        [FieldOffset(0)]
        public int Size;
        [FieldOffset(4)]
        public int Type;
        [FieldOffset(8)]
        public long FileId;
        [FieldOffset(8)]
        public Guid ObjectId;
        [FieldOffset(8)]
        public Guid ExtendedFileId; //Use for ReFS; need to use v3 structures or later instead of v2 as done in this sample
    }

    public static int CTL_CODE(int DeviceType, int Function, int Method, int Access)
    {
        return (DeviceType << 16) | (Access << 14) | (Function << 2) | Method;
    }

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            if (createdJournal)
            {
                TryDeleteCurrentJournal();
            }
            StopListening(10);//this may cause a delay
            return CloseHandle(handle);
        }
        return false;
    }

    public const int FILE_DEVICE_FILE_SYSTEM = 0x00000009;
    public const int METHOD_BUFFERED = 0;
    public const int METHOD_IN_DIRECT = 1;
    public const int METHOD_OUT_DIRECT = 2;
    public const int METHOD_NEITHER = 3;
    public const int FILE_ANY_ACCESS = 0;

    public static int FSCTL_READ_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 46, METHOD_NEITHER, FILE_ANY_ACCESS);
    public static int FSCTL_ENUM_USN_DATA = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 44, METHOD_NEITHER, FILE_ANY_ACCESS);
    public static int FSCTL_CREATE_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 57, METHOD_NEITHER, FILE_ANY_ACCESS);
    public static int FSCTL_READ_FILE_USN_DATA = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 58, METHOD_NEITHER, FILE_ANY_ACCESS);
    public static int FSCTL_QUERY_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 61, METHOD_BUFFERED, FILE_ANY_ACCESS);
    public static int FSCTL_DELETE_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 62, METHOD_BUFFERED, FILE_ANY_ACCESS);
    public static int FSCTL_WRITE_USN_REASON = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 180, METHOD_BUFFERED, FILE_ANY_ACCESS);
    public static int FSCTL_USN_TRACK_MODIFIED_RANGES = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 189, METHOD_BUFFERED, FILE_ANY_ACCESS);

    [StructLayout(LayoutKind.Sequential)]
    public struct USN
    {
        public long Usn;

        public static implicit operator USN(long usn)
        {
            return new USN { Usn = usn };
        }

        public static implicit operator long(USN usn)
        {
            return usn.Usn;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MFT_ENUM_DATA_V0
    {
        public USN Low;
        public USN High;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MFT_ENUM_DATA_V1
    {
        public long StartFileReferenceNumber;
        public USN Low;
        public USN High;
        public short MinMajorVersion;
        public short MaxMajorVersion;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_USN_JOURNAL_DATA
    {
        public long MaximumSize;
        public long AllocationDelta;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct READ_USN_JOURNAL_DATA_V0
    {
        public USN StartUsn;
        public int ReasonMask;
        public int ReturnOnlyOnClose;
        public long Timeout;
        public long BytesToWaitFor;
        public long UsnJournalId;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct READ_USN_JOURNAL_DATA_V1
    {
        public USN StartUsn;
        public int ReasonMask;
        public int ReturnOnlyOnClose;
        public long Timeout;
        public long BytesToWaitFor;
        public long UsnJournalId;
        public short MinMajorVersion;
        public short MaxMajorVersion;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct USN_TRACK_MODIFIED_RANGES
    {
        public int Flags;
        public int Unused;
        public long ChunkSize;
        public long FileSizeThreshold;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RANGE_TRACK_OUTPUT
    {
        public USN Usn;
    }

    public const int FLAG_USN_TRACK_MODIFIED_RANGES_ENABLE = 0x00000001;

    public class UsnRecordV2WithName
    {
        public USN_RECORD_V3 Record { get; set; }
        public string Filename { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RECORD_V2
    {
        public int RecordLength;
        public short MajorVersion;
        public short MinorVersion;
        public long FileReferenceNumber;
        public long ParentFileReferenceNumber;
        USN Usn;
        public long TimeStamp;
        public int Reason;
        public int SourceInfo;
        public int SecurityId;
        public int FileAttributes;
        public short FileNameLength;
        public short FileNameOffset;
        //WCHAR FileName[1];

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RECORD_V3
    {
        public int RecordLength;
        public short MajorVersion;
        public short MinorVersion;
        public Guid FileReferenceNumber;
        public Guid ParentFileReferenceNumber;
        USN Usn;
        public long TimeStamp;
        public int Reason;
        public int SourceInfo;
        public int SecurityId;
        public int FileAttributes;
        public short FileNameLength;
        public short FileNameOffset;
        //WCHAR FileName[1];

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RECORD_COMMON_HEADER
    {
        public int RecordLength;
        public short MajorVersion;
        public short MinorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RECORD_EXTENT
    {
        public long Offset;
        public long Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_RECORD_V4
    {
        public USN_RECORD_COMMON_HEADER Header;
        public Guid FileReferenceNumber;
        public Guid ParentFileReferenceNumber;
        public USN Usn;
        public int Reason;
        public int SourceInfo;
        public int RemainingExtents;
        public short NumberOfExtents;
        public short ExtentSize;
        public USN_RECORD_EXTENT Extents; //Extents[1]
    }


    public const int USN_PAGE_SIZE = 0x1000;
    public const int USN_REASON_DATA_OVERWRITE = 0x00000001;
    public const int USN_REASON_DATA_EXTEND = 0x00000002;
    public const int USN_REASON_DATA_TRUNCATION = 0x00000004;
    public const int USN_REASON_NAMED_DATA_OVERWRITE = 0x00000010;
    public const int USN_REASON_NAMED_DATA_EXTEND = 0x00000020;
    public const int USN_REASON_NAMED_DATA_TRUNCATION = 0x00000040;
    public const int USN_REASON_FILE_CREATE = 0x00000100;
    public const int USN_REASON_FILE_DELETE = 0x00000200;
    public const int USN_REASON_EA_CHANGE = 0x00000400;
    public const int USN_REASON_SECURITY_CHANGE = 0x00000800;
    public const int USN_REASON_RENAME_OLD_NAME = 0x00001000;
    public const int USN_REASON_RENAME_NEW_NAME = 0x00002000;
    public const int USN_REASON_INDEXABLE_CHANGE = 0x00004000;
    public const int USN_REASON_BASIC_INFO_CHANGE = 0x00008000;
    public const int USN_REASON_HARD_LINK_CHANGE = 0x00010000;
    public const int USN_REASON_COMPRESSION_CHANGE = 0x00020000;
    public const int USN_REASON_ENCRYPTION_CHANGE = 0x00040000;
    public const int USN_REASON_OBJECT_ID_CHANGE = 0x00080000;
    public const int USN_REASON_REPARSE_POINT_CHANGE = 0x00100000;
    public const int USN_REASON_STREAM_CHANGE = 0x00200000;
    public const int USN_REASON_TRANSACTED_CHANGE = 0x00400000;
    public const int USN_REASON_INTEGRITY_CHANGE = 0x00800000;
    public const uint USN_REASON_CLOSE = 0x80000000;

    [Flags]
    public enum UsnReasonType
    {
        USN_REASON_DATA_OVERWRITE = 0x00000001,
        USN_REASON_DATA_EXTEND = 0x00000002,
        USN_REASON_DATA_TRUNCATION = 0x00000004,
        USN_REASON_NAMED_DATA_OVERWRITE = 0x00000010,
        USN_REASON_NAMED_DATA_EXTEND = 0x00000020,
        USN_REASON_NAMED_DATA_TRUNCATION = 0x00000040,
        USN_REASON_FILE_CREATE = 0x00000100,
        USN_REASON_FILE_DELETE = 0x00000200,
        USN_REASON_EA_CHANGE = 0x00000400,
        USN_REASON_SECURITY_CHANGE = 0x00000800,
        USN_REASON_RENAME_OLD_NAME = 0x00001000,
        USN_REASON_RENAME_NEW_NAME = 0x00002000,
        USN_REASON_INDEXABLE_CHANGE = 0x00004000,
        USN_REASON_BASIC_INFO_CHANGE = 0x00008000,
        USN_REASON_HARD_LINK_CHANGE = 0x00010000,
        USN_REASON_COMPRESSION_CHANGE = 0x00020000,
        USN_REASON_ENCRYPTION_CHANGE = 0x00040000,
        USN_REASON_OBJECT_ID_CHANGE = 0x00080000,
        USN_REASON_REPARSE_POINT_CHANGE = 0x00100000,
        USN_REASON_STREAM_CHANGE = 0x00200000,
        USN_REASON_TRANSACTED_CHANGE = 0x00400000,
        USN_REASON_INTEGRITY_CHANGE = 0x00800000,
        USN_REASON_CLOSE = unchecked((int)0x80000000)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_JOURNAL_DATA_V0
    {
        public long UsnJournalID;
        public USN FirstUsn;
        public USN NextUsn;
        public USN LowestValidUsn;
        public USN MaxUsn;
        public long MaximumSize;
        public long AllocationDelta;

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct USN_JOURNAL_DATA_V1
    {
        public long UsnJournalID;
        public USN FirstUsn;
        public USN NextUsn;
        public USN LowestValidUsn;
        public USN MaxUsn;
        public long MaximumSize;
        public long AllocationDelta;
        public short MinSupportedMajorVersion;
        public short MaxSupportedMajorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USN_JOURNAL_DATA_V2
    {
        public long UsnJournalID;
        public USN FirstUsn;
        public USN NextUsn;
        public USN LowestValidUsn;
        public USN MaxUsn;
        public long MaximumSize;
        public long AllocationDelta;
        public short MinSupportedMajorVersion;
        public short MaxSupportedMajorVersion;
        public int Flags;
        public long RangeTrackChunkSize;
        public long RangeTrackFileSizeThreshold;
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct DELETE_USN_JOURNAL_DATA
    {
        public long UsnJournalID;
        public int DeleteFlags;
    }

    public int USN_DELETE_FLAG_DELETE = 0x00000001;
    public int USN_DELETE_FLAG_NOTIFY = 0x00000002;
    public int USN_DELETE_VALID_FLAGS = 0x00000003;

    public UsnReasonType EventTriggerMask
    {
        get
        {
            return (UsnReasonType)rdata.ReasonMask;
        }
        set
        {
            rdata.ReasonMask = (int)value;
        }
    }

    public long Timeout
    {
        get
        {
            return rdata.Timeout;
        }
        set
        {
            rdata.Timeout = value;
        }
    }

    public bool TriggerOnCloseOnly
    {
        get
        {
            return rdata.ReturnOnlyOnClose != 0;
        }
        set
        {
            rdata.ReturnOnlyOnClose = value ? 1 : 0;
        }
    }
    private ReaderWriterLockSlim readBufferLock = new ReaderWriterLockSlim();

    private const int DefaultBufferSize = 8192;
    private int readBufferSize;

    //This could hang if there is a long timeout value
    public int ReadBufferSize
    {
        get
        {
            readBufferLock.EnterReadLock();
            try
            {
                return readBufferSize;
            }
            finally
            {
                readBufferLock.ExitReadLock();
            }
        }
        set => AllocateBuffer(value);
    }

    private void AllocateBuffer(int value = DefaultBufferSize)
    {
        readBufferLock.EnterWriteLock();
        try
        {
            if (value > 0)
            {
                readBuffer = readBuffer == IntPtr.Zero 
                    ? Marshal.AllocHGlobal(value) 
                    : Marshal.ReAllocHGlobal(readBuffer, (IntPtr)value);
            }
            readBufferSize = value;

        }
        finally
        {
            readBufferLock.ExitWriteLock();
        }
    }

    public event Action<ChangeJournalHandle, UsnRecordV2WithName> OnChange;
    public event Action<ChangeJournalHandle, Exception> OnError;

    private bool shouldRun;

    private Thread thread;

    public ChangeJournalHandle(string path) : base(true)
    {
        //TODO:Handle taking non-volume paths
        handle = CreateFileW(path, unchecked((int)(0x80000000 | 0x40000000)),
            FileShare.ReadWrite, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (IsInvalid)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }

    public bool TryCreateJournal(long maxSize = (1024 ^ 2) * 500, long allocationDelta = 8192)
    {
        var data = new CREATE_USN_JOURNAL_DATA();
        data.AllocationDelta = allocationDelta;
        data.MaximumSize = maxSize;
        var size = Marshal.SizeOf(data);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            int bufSizeOut;
            var result = DeviceIoControl(handle, FSCTL_CREATE_USN_JOURNAL, buffer, size, IntPtr.Zero, 0, out bufSizeOut, IntPtr.Zero);
            if (result == 0)
            {
                ReportLastError();
                return false;
            }
            createdJournal = true;
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
    public void CreateJournal(long maxSize = 1024 * 1024 * 500, long allocationDelta = 8192)
    {
        var data = new CREATE_USN_JOURNAL_DATA();
        data.AllocationDelta = allocationDelta;
        data.MaximumSize = maxSize;
        var size = Marshal.SizeOf(data);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = DeviceIoControl(handle, FSCTL_CREATE_USN_JOURNAL, buffer, size, IntPtr.Zero, 0, out _, IntPtr.Zero);
            if (result == 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
    public bool TryDeleteCurrentJournal()
    {
        var data = new USN_JOURNAL_DATA_V0();
        var size = Marshal.SizeOf(data);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = DeviceIoControl(handle, FSCTL_QUERY_USN_JOURNAL, IntPtr.Zero, 0, buffer, size, out _, IntPtr.Zero);
            if (result == 0)
            {
                ReportLastError();
                return false;
            }
            data = Marshal.PtrToStructure<USN_JOURNAL_DATA_V0>(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        var d = new DELETE_USN_JOURNAL_DATA
        {
            UsnJournalID = data.UsnJournalID,
            DeleteFlags = 3
        };
        size = Marshal.SizeOf(d);
        buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(d, buffer, false);
            if (DeviceIoControl(handle, FSCTL_DELETE_USN_JOURNAL, buffer, size, IntPtr.Zero, 0, out size, IntPtr.Zero) == 0)
            {
                ReportLastError();
                return false;
            }
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
    public bool TryDeleteJournal(long UsnJournalID)
    {
        //Note that overloads would be needed for different versions of the structure
        var d = new DELETE_USN_JOURNAL_DATA
        {
            UsnJournalID = UsnJournalID,
            DeleteFlags = 3
        };
        var size = Marshal.SizeOf(d);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(d, buffer, false);
            if (DeviceIoControl(handle, FSCTL_DELETE_USN_JOURNAL, buffer, size, IntPtr.Zero, 0, out size, IntPtr.Zero) == 0)
            {
                ReportLastError();
                return false;
            }
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

    }
    public void DeleteAllJournals()
    {
        try
        {
            while (true)
            {
                var data = new USN_JOURNAL_DATA_V0();
                var size = Marshal.SizeOf(data);
                var buffer = Marshal.AllocHGlobal(size);
                try
                {
                    var result = DeviceIoControl(handle, FSCTL_QUERY_USN_JOURNAL, IntPtr.Zero, 0, buffer, size, out _, IntPtr.Zero);
                    if (result == 0)
                    {
                        ReportLastError();
                        break;
                    }
                    data = Marshal.PtrToStructure<USN_JOURNAL_DATA_V0>(buffer);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                var d = new DELETE_USN_JOURNAL_DATA
                {
                    UsnJournalID = data.UsnJournalID,
                    DeleteFlags = 3
                };
                size = Marshal.SizeOf(d);
                buffer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(d, buffer, false);
                    if (DeviceIoControl(handle, FSCTL_DELETE_USN_JOURNAL, buffer, size, IntPtr.Zero, 0, out size, IntPtr.Zero) == 0)
                    {
                        ReportLastError();
                        break;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            ReportException(ex);
        }
    }

    public void StopListening(int timeout = int.MaxValue)
    {
        if (shouldRun)
        {
            shouldRun = false;
            if (thread != null)
            {
                if (!thread.Join(timeout))
                {
                    thread.Abort();
                }
                thread = null;
            }
        }
    }

    public string GetNameForId(long id)
    {
        try
        {
            var fid = new FILE_ID_DESCRIPTOR {FileId = id};
            fid.Size = Marshal.SizeOf(fid);
            var h = OpenFileById(handle, ref fid, 0x80, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, 0);
            if (h == new IntPtr(-1))
            {
                //Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return id.ToString();
            }
            var size = 1024;
            var sb = new StringBuilder(size);
            if (GetFinalPathNameByHandleW(h, sb, size, 0) == 0)
            {
                var hr = Marshal.GetHRForLastWin32Error();
                CloseHandle(h);
                Marshal.ThrowExceptionForHR(hr);
            }
            CloseHandle(h);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            ReportException(ex);
            return id.ToString("X");
        }

    }
    private READ_USN_JOURNAL_DATA_V1 rdata = new READ_USN_JOURNAL_DATA_V1 { ReasonMask = unchecked((int)0xFFFFFFFF) };
    private IntPtr readBuffer;
    private bool createdJournal;

    IEnumerable<UsnRecordV2WithName> ListenProc()
    {
        var data = new USN_JOURNAL_DATA_V2();
        var size = Marshal.SizeOf(data);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            int outSize;
            var result = DeviceIoControl(handle, FSCTL_QUERY_USN_JOURNAL, IntPtr.Zero, 0, buffer, size, out outSize,
                IntPtr.Zero);
            if (result == 0)
            {
                if (TryCreateJournal())
                {
                    result = DeviceIoControl(handle, FSCTL_QUERY_USN_JOURNAL, IntPtr.Zero, 0, buffer, size,
                        out outSize, IntPtr.Zero);
                }
                if (result == 0) ReportLastError();
            }
            if (result != 0) data = Marshal.PtrToStructure<USN_JOURNAL_DATA_V2>(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        rdata.UsnJournalId = data.UsnJournalID;
        rdata.StartUsn = 0;
        rdata.ReasonMask = (int)(UsnReasonType.USN_REASON_FILE_CREATE | UsnReasonType.USN_REASON_FILE_DELETE
                           | UsnReasonType.USN_REASON_RENAME_OLD_NAME | UsnReasonType.USN_REASON_RENAME_NEW_NAME
                           | UsnReasonType.USN_REASON_REPARSE_POINT_CHANGE
                           | UsnReasonType.USN_REASON_HARD_LINK_CHANGE);
    //rdata.ReturnOnlyOnClose = 1;

        rdata.MinMajorVersion = 2;
        rdata.MaxMajorVersion = 3;
        var rsize = Marshal.SizeOf(typeof(USN_RECORD_V3));
        size = Marshal.SizeOf(rdata);
        buffer = Marshal.AllocHGlobal(size);
        if (readBuffer == IntPtr.Zero)
        {
            AllocateBuffer();
        }
        var usize = Marshal.SizeOf(typeof(USN));
        try
        {
            while (true)
            {
                /*if (readBufferSize >= 1024)
                {
                    rdata.BytesToWaitFor = readBufferSize;
                }
                else
                {*/
                //Returns immediately
                rdata.BytesToWaitFor = 0;
                rdata.Timeout = 0;
                //}
                Marshal.StructureToPtr(rdata, buffer, false);

                readBufferLock.EnterReadLock();
                try
                {
                    var result = DeviceIoControl(handle, FSCTL_READ_USN_JOURNAL, buffer, size, readBuffer, readBufferSize, out var outSize, IntPtr.Zero);
                    if (result > 0 && outSize > usize)
                    {
                        var usn = Marshal.PtrToStructure<USN>(readBuffer);
                        if (outSize == usize)
                        {
                            ReportLastError();
                            yield break;
                        }
                        rdata.StartUsn = usn;
                        var retbytes = outSize - usize;
                        var record = IntPtr.Add(readBuffer, usize);
                        while (retbytes > 0)
                        {
                            var r = Marshal.PtrToStructure<USN_RECORD_V3>(record);
                            var r2 = new UsnRecordV2WithName
                            {
                                Record = r,
                                Filename = Marshal.PtrToStringUni(record + r.FileNameOffset, r.FileNameLength / 2)
                            };
                            yield return r2;
                            record += r.RecordLength;
                            retbytes -= r.RecordLength;
                        }
                    }
                    else
                    {
                        ReportLastError();
                        yield break;
                    }
                }
                finally
                {
                    readBufferLock.ExitReadLock();
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    void ReportChange(UsnRecordV2WithName record)
    {
        if (OnChange != null)
        {
            OnChange(this, record);
        }
    }

    void ReportLastError()
    {
        ReportException(Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
    }

    void ReportException(Exception ex)
    {
        if (OnError != null)
        {
            OnError(this, ex);
        }
    }



    private static void Cjh_OnError(ChangeJournalHandle arg1, Exception arg2)
    {
        Console.WriteLine("Error:/t{0}", arg2);
    }

    private static void Cjh_OnChange(ChangeJournalHandle arg1, UsnRecordV2WithName arg2)
    {
        //Note that it would be typically faster in the long run to build a dictionary 
        //of directory names by IDs and reset it whenever the change journal resets 
        //instead of looking up the directory each time
        //Also, note that if the directory is deleted before OpenFileById is called in GetNameById, it's going to fail with an out of range error
        //Console.Write(arg1.GetNameForId(arg2.Record.ParentFileReferenceNumber));
        Console.Write("\\");
        Console.Write(arg2.Filename);
        Console.Write(":\t");
        Console.WriteLine(((UsnReasonType)arg2.Record.Reason).ToString());
    }
    public static List<UsnRecordV2WithName> Run()
    {
        var pathToVolumeToMonitor = @"\\?\C:";
        //This will filter to show only files that are deleted or created
        var reasonsToMonitor = UsnReasonType.USN_REASON_FILE_CREATE | UsnReasonType.USN_REASON_FILE_DELETE;
        using (var cjh = new ChangeJournalHandle(pathToVolumeToMonitor))
        {
            //                cjh.OnChange += Cjh_OnChange;
            //cjh.OnError += Cjh_OnError;
            //cjh.EventTriggerMask = reasonsToMonitor;

            return cjh.ListenProc().ToList();

            //                cjh.OnChange -= Cjh_OnChange;
            //cjh.OnError -= Cjh_OnError;
        }
    }
}