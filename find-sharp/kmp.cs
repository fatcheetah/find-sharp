using System.Numerics;
using System.Text;

namespace find_sharp;

public static class KMP
{
    public static int[] ComputeLPSArray(string pattern)
    {
        int length = pattern.Length;
        int[] lps = new int[length];
        int len = 0;
        int i = 1;

        while (i < length)
            if (pattern[i] == pattern[len])
            {
                len++;
                lps[i] = len;
                i++;
            }
            else
            {
                if (len != 0)
                    len = lps[len - 1];
                else
                {
                    lps[i] = 0;
                    i++;
                }
            }
        return lps;
    }

    public static bool KMPSearch(string text, string pattern)
    {
        int[] lps = ComputeLPSArray(pattern);
        int i = 0;
        int j = 0;

        while (i < text.Length)
        {
            if (pattern[j] == text[i])
            {
                i++;
                j++;
            }

            if (j == pattern.Length)
                return true;
            else if (i < text.Length && pattern[j] != text[i])
            {
                if (j != 0)
                    j = lps[j - 1];
                else
                    i++;
            }
        }
        return false;
    }

    public static bool FuzzyMatch(string path, string input)
    {
        return KMPSearch(text: path.ToLower(), pattern: input.ToLower());
    }
}