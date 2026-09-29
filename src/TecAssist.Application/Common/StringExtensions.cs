namespace TecAssist.Application.Common;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
    }
}
