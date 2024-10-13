using System.Collections.Concurrent;
using System.IO.Pipes;
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
public struct dirent
{
    public ulong d_ino;
    public long d_off;
    public ushort d_reclen;
    public byte d_type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string d_name;
}


internal class Program
{
    private const byte DT_DIR = 4;
    private static ConcurrentQueue<string> matchBuffer = new();
    private static bool processing = true;

    private static void Main(string[] args)
    {
        string rootDirectory = args.Length > 0 ? args[0] : "/home/cream/fun";
        Console.WriteLine($"Starting with root directory: {rootDirectory}");

        Thread matchThread = new Thread(ProcessMatchBuffer);
        matchThread.Start();

        try
        {
            var executionTimeAttribute = new ExecutionTimeAttribute();
            executionTimeAttribute.MeasureExecutionTime(() => TraverseFileTree(rootDirectory));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
        }
        finally
        {
            processing = false;
            matchThread.Join();
        }
    }

    private static void TraverseFileTree(string rootDirectory)
    {
        ConcurrentQueue<string> directories = new();
        directories.Enqueue(rootDirectory);

        while (directories.TryDequeue(out string? currentDirectory))
        {
            IntPtr dirp = Interop.opendir(currentDirectory);
            if (dirp == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to open directory: {currentDirectory}");
                continue;
            }

            IntPtr entry;
            while ((entry = Interop.readdir(dirp)) != IntPtr.Zero)
            {
                dirent dir = Marshal.PtrToStructure<dirent>(entry);
                string path = Path.Combine(currentDirectory, dir.d_name);

                matchBuffer.Enqueue(path);

                if (dir.d_type == DT_DIR && dir.d_name != "." && dir.d_name != "..")
                {
                    directories.Enqueue(path);
                }
            }

            Interop.closedir(dirp);
        }
    }

    private static void ProcessMatchBuffer()
    {
        while (processing || !matchBuffer.IsEmpty)
        {
            if (matchBuffer.TryDequeue(out string? path))
            {
                if (KMP.FuzzyMatch(path, "http"))
                {
                    Console.WriteLine($"Found entry containing 'http': {path}");
                }
            }
            else
            {
                Thread.Sleep(100); // Avoid busy-waiting
            }
        }
    }
}