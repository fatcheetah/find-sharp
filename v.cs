namespace find_sharp;

public static class VSearch
{
    private static ReadOnlyMemory<char>? _inputCached;
    
    public static bool SubStringMatcher(ReadOnlyMemory<char>? path, string input)
    {
        if (input.Length > path?.Length 
            || path.HasValue == false 
            || string.IsNullOrEmpty(input)) 
            return false;

        _inputCached ??= input.AsMemory();

        ReadOnlySpan<char> pathSpan = path.Value.Span;
        ReadOnlySpan<char> inputSpan = _inputCached.Value.Span;

        return SliceSubstringSearch(pathSpan, inputSpan);
    }

    private static bool SliceSubstringSearch(ReadOnlySpan<char> pathSpan, ReadOnlySpan<char> inputSpan)
    {
        int inputLength = inputSpan.Length;

        for (int i = 0; i <= pathSpan.Length - inputLength; i++)
            if (pathSpan.Slice(i, inputLength).Equals(inputSpan, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}