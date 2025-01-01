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

        return pos > 0;
    }


    private static int SliceSubstringSearch(ReadOnlySpan<char> pathSpan, ReadOnlySpan<char> inputSpan)
    {
        int inputLength = inputSpan.Length;

        for (int i = 0; i <= pathSpan.Length-inputLength; i++)
            if (pathSpan.Slice(i, inputLength).Equals(inputSpan, _hasUpperChar!.Value
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase
                ))
                return i;

        return 0;
    }
}