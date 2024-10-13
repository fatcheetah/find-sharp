using System.Diagnostics;

[AttributeUsage(AttributeTargets.Method)]
public class ExecutionTimeAttribute : Attribute
{
    public void MeasureExecutionTime(Action method)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        method();
        stopwatch.Stop();
        Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }
}