using System.Runtime.InteropServices;

namespace find_sharp;

internal partial class Interop
{
    [DllImport("libc.so.6")]
    public static extern IntPtr opendir(string name);

    [DllImport("libc.so.6")]
    public static extern IntPtr readdir(IntPtr dirp);

    [DllImport("libc.so.6")]
    public static extern int closedir(IntPtr dirp);
}

[StructLayout(LayoutKind.Sequential)]
public struct Dirent
{
    public ulong d_ino;
    public long d_off;
    public ushort d_reclen;
    public byte d_type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string d_name;
}
