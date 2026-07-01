namespace Wbmcs.Model;

public static class StringSearchEngine
{
    public static IOrderedEnumerable<T> FuzzyLev<T>(IEnumerable<T> src, Func<T, string> keyGen, string search)
    {
        search = search.ToLower();
        var len = search.Length;
        return src
            .Where(x =>
            {
                var key = keyGen(x);
                return LevDistanceIgnoreCaseB(search, key) <= Math.Abs(key.Length - len);
            })
            .OrderBy(x => LevDistanceIgnoreCaseB(search, keyGen(x)));
    }

    private static int LevDistanceIgnoreCaseB(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        Span<int> previousDistance = stackalloc int[b.Length + 1];

        for (var j = 0; j < b.Length + 1; j++) previousDistance[j] = j;

        Span<int> currentDistance = stackalloc int[b.Length + 1];
        for (var i = 1; i < a.Length + 1; i++)
        {
            currentDistance[0] = i;

            for (var j = 1; j < b.Length + 1; j++)
            {
                var d1 = previousDistance[j] + 1;
                var d2 = currentDistance[j - 1] + 1;
                var d3 = previousDistance[j - 1];
                if (a[i - 1] != char.ToLowerInvariant(b[j - 1])) d3 += 1;
                d2 = d1 < d2 ? d1 : d2;
                currentDistance[j] = d3 < d2 ? d3 : d2;
            }

            //switching instead of allocating new for current
            var temp = currentDistance;
            currentDistance = previousDistance;
            previousDistance = temp;
        }

        return previousDistance[b.Length];
    }
}