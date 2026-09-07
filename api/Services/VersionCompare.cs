namespace UpdateCenter.Api.Services;

public static class VersionCompare
{
    public static int Compare(string? left, string? right)
    {
        var a = Parse(left);
        var b = Parse(right);
        var length = Math.Max(a.Length, b.Length);
        for (var i = 0; i < length; i++)
        {
            var av = i < a.Length ? a[i] : 0;
            var bv = i < b.Length ? b[i] : 0;
            if (av != bv)
            {
                return av.CompareTo(bv);
            }
        }
        return 0;
    }

    public static bool IsLessThan(string? current, string? target) => Compare(current, target) < 0;

    private static int[] Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [0];
        }

        var core = value.Trim();
        var dash = core.IndexOf('-');
        if (dash >= 0)
        {
            core = core[..dash];
        }

        return core.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(new string(part.Where(char.IsDigit).ToArray()), out var n) ? n : 0)
            .ToArray();
    }
}
