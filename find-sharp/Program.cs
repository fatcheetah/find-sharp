using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace find_sharp;

internal class Program
{
    private const byte DtDir = 4;
    private static readonly Channel<string> PathChannel = Channel.CreateUnbounded<string>();

    private static async Task Main(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string? searchInput = args.Length > 0
            ? args[0]
            : null;

        if (searchInput == null)
            return;

        Task filterAndProcessTask = Task.Run(() => FilterAndProcessPaths(searchInput));

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
            await filterAndProcessTask;
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
                Task task = Task.Run(async () => { await TraverseDirectoryAsync(currentDirectory, directories); });
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

    private static async Task FilterAndProcessPaths(string search)
    {
        int countOfSlashes = search.Count(c => c == Path.DirectorySeparatorChar);

        await foreach (string path in PathChannel.Reader.ReadAllAsync())
        {
            if (path.EndsWith(".") || path.EndsWith(".."))
                continue;
            
            string searchPath = countOfSlashes == 0
                ? Path.GetFileName(path)
                : string.Join(Path.DirectorySeparatorChar,
                    values: path.Split(Path.DirectorySeparatorChar).TakeLast(countOfSlashes + 1));

            if (KMP.FuzzyMatch(searchPath, search))
                Console.WriteLine(path);
        }
    }
}