using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32;

namespace NtfsIndexer;

public class MftReader
{
    public IEnumerable<Win32.UsnRecord> EnumerateVolume(string drive)
    {
        using (var volumeRootHandle = GetRootHandle(drive))
        {
            CreateChangeJournal(volumeRootHandle);
            foreach (var entry in EnumerateFiles(volumeRootHandle))
                yield return entry;
        }
    }

    public long GetRootFrnEntry(string drive)
    {
        var driveRoot = @"\\.\" + drive + @"\";
        using (var hRoot = PInvoke.CreateFile(driveRoot,
            0,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
            null))
        {
            if (hRoot.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to get root frn entry");
            }

            if (Win32.GetFileInformationByHandle(hRoot, out var fi))
            {
                var indexRoot = ((long)fi.FileIndexHigh << 32) | fi.FileIndexLow;

                return indexRoot;
            }

            throw new Win32Exception(Marshal.GetLastWin32Error(),
                "GetFileInformationByHandle returned invalid handle");
        }
    }

    private SafeFileHandle GetRootHandle(string drive)
    {
        var vol = @"\\.\" + drive;
        var volumeRootHandle = PInvoke.CreateFile(vol,
            FILE_ACCESS_FLAGS.FILE_GENERIC_READ | FILE_ACCESS_FLAGS.FILE_GENERIC_WRITE,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
            null);

        if (volumeRootHandle.IsInvalid)
        {
            throw new IOException("CreateFile() returned invalid handle",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }

        return volumeRootHandle;
    }

    private IEnumerable<Win32.UsnRecord> EnumerateFiles(SafeFileHandle volumeRootHandle)
    {
        //var rootFrn = GetRootFrnEntry(drive);
        var symLinks = new List<Win32.UsnRecord>();

        /*var files = new Dictionary<ulong, Win32.FileNameAndParentFrn>
        {
            [rootFrn] = new Win32.FileNameAndParentFrn(drive, 0)
        };*/

        var ujd = SetupMFT_Enum_DataBuffer(volumeRootHandle);
        var med = new Win32.MFT_ENUM_DATA_V1
        {
            StartFileReferenceNumber = 0,
            LowUsn = 0,
            HighUsn = long.MaxValue,
            MinMajorVersion = 2,
            MaxMajorVersion = 2
        };

        var sizeOfBuffer = 0x1000 + sizeof(ulong);
        using (var medBuffer = SafeStructureHandle.Create(med))
        {
            var pData = Marshal.AllocHGlobal(sizeOfBuffer);
            try
            {
                while (Win32.DeviceIoControl(volumeRootHandle, PInvoke.FSCTL_ENUM_USN_DATA, medBuffer,
                    medBuffer.Size, pData, sizeOfBuffer, out uint outBytesReturned,
                    IntPtr.Zero))
                {
                    Marshal.WriteInt64(medBuffer.DangerousGetHandle(), Marshal.ReadInt64(pData));
                    var offset = sizeof(long);
                    while (offset < outBytesReturned)
                    {
                        var usn = new Win32.UsnRecord(pData, offset);

                        if (usn.FileAttributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            symLinks.Add(usn);
                        }

                        yield return usn;
                        offset += (int)usn.RecordLength;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        //    HandleSymLinks(volumeRootHandle, symLinks);
    }

    private void HandleSymLinks(SafeFileHandle volumeRootHandle, IEnumerable<Win32.UsnRecord> symLinks)
    {
        var types = new Dictionary<ReparseTagType, int>();

        var bufferSize = 0x3ff0;
        IntPtr buffer;
        buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            foreach (var usn in symLinks)
            {
                var fileDescriptor = new FILE_ID_DESCRIPTOR();
                fileDescriptor.Anonymous.FileId = usn.FileReferenceNumber;
                using (var sfh = PInvoke.OpenFileById(volumeRootHandle, fileDescriptor,
                    FILE_ACCESS_FLAGS.FILE_READ_ATTRIBUTES, FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE, null,
                    FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT |
                    FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_REPARSE_POINT))
                {
                    //var sizeMftEnumData = Marshal.SizeOf(typeof(Win32.REPARSE_DATA_BUFFER));

                    if (Win32.DeviceIoControl(sfh, PInvoke.FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0,
                        buffer, bufferSize, out var _, IntPtr.Zero))
                    {
                        var tagType = (ReparseTagType)Marshal.ReadInt32(buffer);
                        if (!tagType.IsReparseTagValid())
                        {
                            continue;
                        }

                        string dest = "";
                        if (tagType.IsReparseTagMicrosoft() && tagType.IsReparseTagNameSurrogate())
                        {
                            IReparsePointData? reparseData = null;

                            switch (tagType)
                            {
                                case ReparseTagType.IoReparseTagMountPoint:
                                    reparseData =
                                        Marshal.PtrToStructure<ReparsePoints.REPARSE_DATA_BUFFER>(buffer);
                                    break;

                                case ReparseTagType.IoReparseTagSymlink:
                                    reparseData = Marshal
                                        .PtrToStructure<ReparsePoints.REPARSE_DATA_SYMLINK_BUFFER>(buffer);
                                    break;

                                default:
                                    Debugger.Break();
                                    break;
                            }
                            dest = reparseData?.SubstituteName!;
                        }


                        Debug.WriteLine(tagType + " = " + usn.FileName + " <-> " + dest);
                    }
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// This function creates a journal on the volume. If it already exists this it will adjust the MaximumSize and AllocationDelta.
    /// </summary>
    private void CreateChangeJournal(SafeFileHandle changeJournalRootHandle)
    {
        ulong MaximumSize = 0x800000;
        ulong AllocationDelta = 0x10000;
        var cujd = new Win32.CREATE_USN_JOURNAL_DATA
        {
            MaximumSize = MaximumSize,
            AllocationDelta = AllocationDelta
        };

        using (var cujdBuffer = SafeStructureHandle.Create(cujd))
        {
            var fOk = Win32.DeviceIoControl(changeJournalRootHandle, PInvoke.FSCTL_CREATE_USN_JOURNAL,
                cujdBuffer, cujdBuffer.Size, IntPtr.Zero, 0, out uint _, IntPtr.Zero);
            if (!fOk)
            {
                throw new IOException("DeviceIoControl() returned false",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
    }

    private Win32.USN_JOURNAL_DATA_V2 SetupMFT_Enum_DataBuffer(SafeFileHandle changeJournalRootHandle)
    {
        var sizeMftEnumData = Marshal.SizeOf(typeof(Win32.USN_JOURNAL_DATA_V2));

        if (Win32.DeviceIoControl(changeJournalRootHandle,
            PInvoke.FSCTL_QUERY_USN_JOURNAL,   // IO Control Code  
            IntPtr.Zero,                // In Buffer  
            0,                          // In Buffer Size  
            out Win32.USN_JOURNAL_DATA_V2 ujd,                  // Out Buffer  
            sizeMftEnumData,  // Size Of Out Buffer  
            out _,          // Bytes Returned  
            IntPtr.Zero))               // lpOverlapped  
        {
            return ujd;
        }
        //throw new IOException("DeviceIoControl() returned false", new Win32Exception(Marshal.GetLastWin32Error()));
        return default(Win32.USN_JOURNAL_DATA_V2);
    }
}