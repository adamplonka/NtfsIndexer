using System.Runtime.InteropServices;

namespace NtfsIndexer;

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
        FileName = Marshal.PtrToStringUni(IntPtr.Add(p, offset + FileNameOffset), FileNameLength / sizeof(char));
    }
}