using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NtfsIndexer;

public static class SafeStructureHandle
{
    public static SafeStructureHandle<T> Create<T>([DisallowNull] T obj)
    {
        return new SafeStructureHandle<T>(obj);
    }
}

public class SafeStructureHandle<T> : SafeHandleZeroOrMinusOneIsInvalid
{
    public int Size { get; }

    protected override bool ReleaseHandle()
    {
        Marshal.DestroyStructure(handle, typeof(T));
        Marshal.FreeHGlobal(handle);
        return true;
    }

    public SafeStructureHandle([DisallowNull] T structure) : base(true)
    {
        Size = Marshal.SizeOf(structure);
        var ptr = Marshal.AllocHGlobal(Size);
        Marshal.StructureToPtr(structure, ptr, true);
        SetHandle(ptr);
    }
}