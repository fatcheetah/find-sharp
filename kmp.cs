using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace find_sharp;

public static class VSearch
{
    public static bool SignalSubStringMatcher(string path, string input)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(input) || input.Length > path.Length)
            return false;

        if (Avx2.IsSupported && path.Length >= Vector256<byte>.Count)
            return Avx2SubStringMatcher(path, input);
        if (Sse2.IsSupported && path.Length >= Vector128<byte>.Count)
            return Sse2SubStringMatcher(path, input);
        
        return FallbackSubStringMatcher(path, input);
    }

    private static unsafe bool Avx2SubStringMatcher(string path, string input)
    {
        fixed (char* pPath = path, pInput = input)
        {
            byte* bPath = (byte*)pPath;
            byte* bInput = (byte*)pInput;
            int pathLength = path.Length * 2;
            int inputLength = input.Length * 2;

            for (int i = 0; i <= pathLength - inputLength; i += 2)
                if (CompareMemory(bPath + i, bInput, inputLength))
                    return true;
        }

        return false;
    }

    private static unsafe bool Sse2SubStringMatcher(string path, string input)
    {
        fixed (char* pPath = path, pInput = input)
        {
            byte* bPath = (byte*)pPath;
            byte* bInput = (byte*)pInput;
            int pathLength = path.Length * 2;
            int inputLength = input.Length * 2;

            for (int i = 0; i <= pathLength - inputLength; i += 2)
                if (CompareMemory(bPath + i, bInput, inputLength))
                    return true;
        }

        return false;
    }

    private static bool FallbackSubStringMatcher(string path, string input)
    {
        return path.Contains(input, StringComparison.Ordinal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool CompareMemory(byte* p1, byte* p2, int length)
    {
        int i;
        for (i = 0; i <= length - sizeof(long); i += sizeof(long))
            if (*(long*)(p1 + i) != *(long*)(p2 + i))
                return false;
        for (; i < length; i++)
            if (*(p1 + i) != *(p2 + i))
                return false;
        return true;
    }
}