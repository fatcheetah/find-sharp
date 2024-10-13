using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace find_sharp;

internal class Program
{
    private const byte DtDir = 4;
    private static readonly Channel<string> PathChannel = Channel.CreateUnbounded<string>();
    private static readonly Channel<string> FilteredChannel = Channel.CreateUnbounded<string>();
    private static readonly SemaphoreSlim Semaphore = new(16);

    private static async Task Main(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string? searchInput = args.Length > 0 ? args[0] : null;

        if (searchInput == null)
            return;

        Task matchTask = Task.Run(() => ProcessMatchBuffer(searchInput));
        Task filterTask = Task.Run(() => FilterPaths(searchInput));

        try
        {
            await TraverseFileTreeAsync(rootDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
        }
        finally
        {
            PathChannel.Writer.Complete();
            await filterTask;
            FilteredChannel.Writer.Complete();
            await matchTask;
        }
    }

    private static async Task TraverseFileTreeAsync(string rootDirectory)
    {
        ConcurrentQueue<string> directories = new();
        directories.Enqueue(rootDirectory);

        List<Task> tasks = new();

        while (!directories.IsEmpty)
        {
            while (directories.TryDequeue(out string? currentDirectory))
            {
                await Semaphore.WaitAsync();
                tasks.Add(Task.Run(async () => {
                    try
                    {
                        await TraverseDirectoryAsync(currentDirectory, directories);
                    }
                    finally
                    {
                        Semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
        }
    }

    private static Task TraverseDirectoryAsync(string currentDirectory, ConcurrentQueue<string> directories)
    {
        IntPtr dirp = Interop.opendir(currentDirectory);
        if (dirp == IntPtr.Zero)
            return Task.CompletedTask;

        IntPtr entry;
        while ((entry = Interop.readdir(dirp)) != IntPtr.Zero)
        {
            Dirent dir = Marshal.PtrToStructure<Dirent>(entry);
            string path = Path.Combine(currentDirectory, dir.d_name);

            PathChannel.Writer.TryWrite(path);

            if (dir.d_type == DtDir && dir.d_name != "." && dir.d_name != "..")
                directories.Enqueue(path);
        }

        Interop.closedir(dirp);
        return Task.CompletedTask;
    }

    private static async Task FilterPaths(string search)
    {
        await foreach (string path in PathChannel.Reader.ReadAllAsync())
        {
            string lastSegment = Path.GetFileName(path);
            if (KMP.FuzzyMatch(lastSegment, search))
            {
                FilteredChannel.Writer.TryWrite(path);
            }
        }
    }

    private static async Task ProcessMatchBuffer(string search)
    {
        await foreach (string path in FilteredChannel.Reader.ReadAllAsync())
        {
            Console.WriteLine($"{path}");
        }
    }
}