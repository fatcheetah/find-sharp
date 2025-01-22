namespace find_sharp;

public static class VSearch
{
    private static ReadOnlyMemory<char>? _inputCached;
    private static bool? _hasUpperChar;


    public static bool SubStringMatcher(ReadOnlyMemory<char>? path, string input, out int pos)
    {
        pos = 0;

        if (input.Length > path?.Length
            || path.HasValue == false
            || string.IsNullOrEmpty(input))
            return false;

        _hasUpperChar ??= input.Any(char.IsUpper);
        _inputCached ??= input.AsMemory();

        ReadOnlySpan<char> pathSpan = path.Value.Span;
        ReadOnlySpan<char> inputSpan = _inputCached.Value.Span;

        pos = SliceSubstringSearch(pathSpan, inputSpan);

        return pos != -1;
    }


    private static int SliceSubstringSearch(ReadOnlySpan<char> pathSpan, ReadOnlySpan<char> inputSpan)
    {
        return _hasUpperChar!.Value
            ? pathSpan.IndexOf(inputSpan)
            : pathSpan.IndexOf(inputSpan, StringComparison.OrdinalIgnoreCase);
    }
}