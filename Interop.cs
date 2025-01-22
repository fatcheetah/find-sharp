using System.Runtime.InteropServices;

namespace find_sharp;

internal partial class Interop
{
    [LibraryImport("libc.so.6", EntryPoint = "scandir",
        StringMarshalling = StringMarshalling.Utf8
    )]
    public static partial int ScanDirectory(string dirp,
        out IntPtr namelist,
        IntPtr filter,
        IntPtr compar);


    [LibraryImport("libc.so.6", EntryPoint = "free")]
    public static partial void Free(IntPtr ptr);
}

[StructLayout(LayoutKind.Sequential)]
struct Dirent
{
    public ulong d_ino;
    public long d_off;
    public ushort d_reclen;
    public byte d_type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public required string? d_name;
    
    public static readonly int DNameOffset = Marshal.OffsetOf<Dirent>("d_name").ToInt32();
    public static readonly int DTypeOffset = Marshal.OffsetOf<Dirent>("d_type").ToInt32();
}