using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace find_sharp;

internal static class Program
{
    private const byte DtDir = 4;
    private static readonly Channel<(ReadOnlyMemory<char>,ReadOnlyMemory<char>)> PathChannel = Channel.CreateUnbounded<(ReadOnlyMemory<char>, ReadOnlyMemory<char>)>();

    private static async Task Main(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string? searchInput = args.Length > 0
            ? args[0]
            : null;

        if (searchInput == null)
            return;

        var traverse = Task.Run(async () =>
            await TraverseFileTreeAsync(rootDirectory)
        );
        var filterAndProcessTask = Task.Run(async () =>
            await FilterAndProcessPaths(searchInput)
        );
        
        await traverse;
        PathChannel.Writer.Complete();
        await filterAndProcessTask;
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
                Task task = Task.Run(async () => {
                    await TraverseDirectoryAsync(currentDirectory, directories);
                });
                tasks.Add(task);
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
        Dirent dirent = new() {
            d_name = null
        };
        
        while ((entry = Interop.readdir(dirp)) != IntPtr.Zero)
        {
            Marshal.PtrToStructure(entry, dirent);

            PathChannel.Writer.TryWrite((currentDirectory.AsMemory(), dirent!.d_name.AsMemory()));

            if (dirent.d_type == DtDir && dirent.d_name != "." && dirent.d_name != ".." && dirent.d_name != null)
                    directories.Enqueue(Path.Combine(currentDirectory, dirent.d_name));
        }

        Interop.closedir(dirp);
        return Task.CompletedTask;
    }

    private static async Task FilterAndProcessPaths(string search)
    {
        await foreach ((ReadOnlyMemory<char>? dir, ReadOnlyMemory<char>? path) in PathChannel.Reader.ReadAllAsync())
        {
            if (VSearch.SubStringMatcher(path, search)) Console.WriteLine($"{dir}/{path}");
        }
    }
}