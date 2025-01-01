using System.Runtime.InteropServices;

namespace find_sharp;

internal partial class Interop
{
    [LibraryImport("libc.so.6", EntryPoint = "opendir",
        StringMarshalling = StringMarshalling.Utf8
    )]
    public static partial IntPtr OpenDirectory(string name);

    [LibraryImport("libc.so.6", EntryPoint = "readdir")]
    public static partial IntPtr ReadDirectory(IntPtr dirp);

    [LibraryImport("libc.so.6", EntryPoint = "closedir")]
    public static partial int CloseDirectory(IntPtr dirp);
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
}