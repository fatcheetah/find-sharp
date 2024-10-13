using System.Diagnostics;

[AttributeUsage(AttributeTargets.Method)]
public class ExecutionTimeAttribute : Attribute
{
    public async Task MeasureExecutionTimeAsync(Func<Task> method)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await method();
        stopwatch.Stop();
        Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }
}
