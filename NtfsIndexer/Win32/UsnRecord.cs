using System.Runtime.InteropServices;
using Windows.Win32.System.Ioctl;
using Windows.Win32.Storage.FileSystem;

namespace NtfsIndexer;

internal interface IUsnRecord
{
    uint RecordLength { get; }
    FileAttributes FileAttributes { get; }
    ushort FileNameOffset { get; }
    ushort FileNameLength { get; }
    FILE_ID_TYPE FileIdType { get; }
    FILE_ID_DESCRIPTOR._Anonymous_e__Union FileIdentifier { get; }
    FILE_ID_DESCRIPTOR._Anonymous_e__Union ParentFileIdentifier { get; }
}

internal class UsnRecord
{
    private static readonly uint DescriptorSize;

    public FileAttributes FileAttributes { get; init; }
    public uint RecordLength { get; init; }
    public string FileName { get; init; }
    public Guid Guid { get; init; }
    public Guid ParentGuid { get; init; }
    private FILE_ID_TYPE idType;

    static UsnRecord()
    {
        DescriptorSize = (uint)Marshal.SizeOf<FILE_ID_DESCRIPTOR>();
    }

    internal FILE_ID_DESCRIPTOR CreateFileIdDescriptor()
    {
        return new FILE_ID_DESCRIPTOR
        {
            Type = idType,
            dwSize = DescriptorSize,
            Anonymous =
            {
                ObjectId = Guid
            }
        };
    }

    public static UsnRecord Create(IntPtr p, int offset)
    {
        var commonHeader = Marshal.PtrToStructure<USN_RECORD_COMMON_HEADER>(p + offset);
        IUsnRecord usnRecord = commonHeader.MajorVersion switch
        {
            2 => Marshal.PtrToStructure<USN_RECORD_V2>(p + offset),
            3 => Marshal.PtrToStructure<USN_RECORD_V3>(p + offset),
            _ => throw new NotSupportedException($"USN_RECORD Version {commonHeader.MajorVersion}.{commonHeader.MinorVersion} not supported")
        };
        var fileName = Marshal.PtrToStringUni(IntPtr.Add(p, offset + usnRecord.FileNameOffset), usnRecord.FileNameLength / sizeof(char));
        return new UsnRecord(usnRecord, fileName);
    }

    public UsnRecord(IUsnRecord usnRecord, string fileName)
    {
        FileAttributes = usnRecord.FileAttributes;
        RecordLength = usnRecord.RecordLength;
        FileName = fileName;
        idType = usnRecord.FileIdType;
        Guid = usnRecord.FileIdentifier.ObjectId;
        ParentGuid = usnRecord.ParentFileIdentifier.ObjectId;
    }
}