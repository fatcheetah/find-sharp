using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace find_sharp;

internal static class Program
{
    private const byte DtDir = 4;

    private static readonly Channel<(ReadOnlyMemory<char>, ReadOnlyMemory<char>)> PathChannel =
        Channel.CreateUnbounded<(ReadOnlyMemory<char>, ReadOnlyMemory<char>)>();


    private static async Task Main(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string? searchInput = args.Length > 0
            ? args[0]
            : null;

        if (searchInput == null)
            return;

        Task traverse = TraverseFileTreeAsync(rootDirectory);
        Task filterAndProcessTask = FilterAndProcessPaths(searchInput);

        await traverse;
        PathChannel.Writer.Complete();
        await filterAndProcessTask;
    }


    private static async Task TraverseFileTreeAsync(string rootDirectory)
    {
        ConcurrentQueue<ReadOnlyMemory<char>> directories = new();
        directories.Enqueue(rootDirectory.AsMemory());

        List<Task> tasks = new();

        while (!directories.IsEmpty)
        {
            while (directories.TryDequeue(out ReadOnlyMemory<char> currentDirectory))
            {
                Task task = Task.Run(async () => { await TraverseDirectoryAsync(currentDirectory, ref directories); });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
        }
    }


    private static Task TraverseDirectoryAsync(ReadOnlyMemory<char> currentDirectory,
        ref ConcurrentQueue<ReadOnlyMemory<char>> directories)
    {
        IntPtr dirp = Interop.OpenDirectory(currentDirectory.ToString());

        if (dirp == IntPtr.Zero)
            return Task.CompletedTask;

        IntPtr entry;
        Span<char> pathBuffer = GC.AllocateArray<char>(512, true);

        while ((entry = Interop.ReadDirectory(dirp)) != IntPtr.Zero)
        {
            IntPtr dNamePtr = entry+Marshal.OffsetOf<Dirent>("d_name").ToInt32();
            string dName = Marshal.PtrToStringAnsi(dNamePtr) ?? string.Empty;
            byte dType = Marshal.ReadByte(entry, Marshal.OffsetOf<Dirent>("d_type").ToInt32());
            PathChannel.Writer.TryWrite((currentDirectory, dName.AsMemory()));

            if (dType != DtDir || dName == "." || dName == "..") continue;

            currentDirectory.Span.CopyTo(pathBuffer);
            pathBuffer[currentDirectory.Length] = Path.DirectorySeparatorChar;
            dName.AsSpan().CopyTo(pathBuffer.Slice(currentDirectory.Length+1));
            directories.Enqueue(pathBuffer.Slice(0, currentDirectory.Length+1+dName.Length).ToArray());
        }

        Interop.CloseDirectory(dirp);

        return Task.CompletedTask;
    }


    private static async Task FilterAndProcessPaths(string search)
    {
        await foreach ((ReadOnlyMemory<char>? dir, ReadOnlyMemory<char>? path) in PathChannel.Reader.ReadAllAsync())
        {
            if (!VSearch.SubStringMatcher(path, search, out int colorStart)) continue;

            await Console.Out.WriteAsync($"{dir}");
            await Console.Out.WriteAsync('/');
            await Console.Out.WriteAsync(path.Value.Slice(0, colorStart));
            await Console.Out.WriteAsync("\u001b[31m");
            await Console.Out.WriteAsync(path.Value.Slice(colorStart, search.Length));
            await Console.Out.WriteAsync("\u001b[0m");
            await Console.Out.WriteLineAsync(path.Value.Slice(colorStart+search.Length));
        }

        await Console.Out.FlushAsync();
    }
}