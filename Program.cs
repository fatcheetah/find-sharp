using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
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
        await Console.Out.FlushAsync();
    }


    private static async Task TraverseFileTreeAsync(string rootDirectory)
    {
        ConcurrentStack<ReadOnlyMemory<char>> directories = new();
        directories.Push(rootDirectory.AsMemory());

        List<Task> tasks = new();
        SemaphoreSlim semaphore = new(Environment.ProcessorCount);

        while (!directories.IsEmpty)
        {
            await semaphore.WaitAsync();
            while (directories.TryPop(out ReadOnlyMemory<char> currentDirectory))
            {
                Task task = Task.Run(async () =>
                {
                    try
                    {
                        await TraverseDirectoryAsync(currentDirectory, directories);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
        }
    }


    private static readonly ArrayPool<char> BufferPool = ArrayPool<char>.Shared;


    private static async Task TraverseDirectoryAsync(ReadOnlyMemory<char> currentDirectory,
        ConcurrentStack<ReadOnlyMemory<char>> directories)
    {
        IntPtr dirp = Interop.OpenDirectory(currentDirectory.ToString());

        if (dirp == IntPtr.Zero)
            return;

        IntPtr entry;
        char[] pathBuffer = BufferPool.Rent(512);

        while ((entry = Interop.ReadDirectory(dirp)) != IntPtr.Zero)
        {
            IntPtr dNamePtr = entry+Dirent.DNameOffset;
            string dName = Marshal.PtrToStringAnsi(dNamePtr) ?? string.Empty;
            byte dType = Marshal.ReadByte(entry, Dirent.DTypeOffset);
            await PathChannel.Writer.WriteAsync((currentDirectory, dName.AsMemory()));

            if (dType != DtDir || dName == "." || dName == "..") continue;

            currentDirectory.Span.CopyTo(pathBuffer);
            pathBuffer[currentDirectory.Length] = Path.DirectorySeparatorChar;
            dName.AsSpan().CopyTo(pathBuffer.AsSpan(currentDirectory.Length+1));
            directories.Push(pathBuffer.AsSpan(0, currentDirectory.Length+1+dName.Length).ToArray());
        }

        BufferPool.Return(pathBuffer);

        if (Interop.CloseDirectory(dirp) != 0) 
            throw new ApplicationException($"Interop.CloseDirectory :: {dirp}");
    }


    private static async Task FilterAndProcessPaths(string search)
    {
        StringBuilder stringBuilder = new();

        await foreach ((ReadOnlyMemory<char>? dir, ReadOnlyMemory<char>? path) in PathChannel.Reader.ReadAllAsync())
        {
            if (!VSearch.SubStringMatcher(path, search, out int colorStart)) continue;

            stringBuilder.Clear();
            stringBuilder.Append(dir);
            stringBuilder.Append('/');
            stringBuilder.Append(path.Value.Slice(0, colorStart));

            if (!Console.IsOutputRedirected)
            {
                stringBuilder.Append("\u001b[31m");
                stringBuilder.Append(path.Value.Slice(colorStart, search.Length));
                stringBuilder.Append("\u001b[0m");
            }
            else
            {
                stringBuilder.Append(path.Value.Slice(colorStart, search.Length));
            }

            stringBuilder.Append(path.Value.Slice(colorStart+search.Length));
            await Console.Out.WriteLineAsync(stringBuilder);
        }
    }
}