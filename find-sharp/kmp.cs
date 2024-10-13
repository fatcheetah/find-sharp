namespace find_sharp;

public static class KMP
{
    public static int[] ComputeLPSArray(string pattern)
    {
        int[] lps = new int[pattern.Length];
        int length = 0;
        int i = 1;
        lps[0] = 0;

        while (i < pattern.Length)
            if (pattern[i] == pattern[length])
            {
                length++;
                lps[i] = length;
                i++;
            }
            else
            {
                if (length != 0)
                    length = lps[length - 1];
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
        return KMPSearch(text: path, pattern: input);
    }
}