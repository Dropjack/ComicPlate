namespace ComicPlate.Core.Sorting;

public sealed class NaturalPathComparer : IComparer<string>
{
    public static NaturalPathComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var left = NormalizeSeparators(x);
        var right = NormalizeSeparators(y);
        var leftIndex = 0;
        var rightIndex = 0;

        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            var leftChar = left[leftIndex];
            var rightChar = right[rightIndex];

            if (char.IsDigit(leftChar) && char.IsDigit(rightChar))
            {
                var numberComparison = CompareNumberSegments(left, ref leftIndex, right, ref rightIndex);
                if (numberComparison != 0)
                {
                    return numberComparison;
                }

                continue;
            }

            var charComparison = char.ToUpperInvariant(leftChar).CompareTo(char.ToUpperInvariant(rightChar));
            if (charComparison != 0)
            {
                return charComparison;
            }

            leftIndex++;
            rightIndex++;
        }

        if (leftIndex != left.Length || rightIndex != right.Length)
        {
            return (left.Length - leftIndex).CompareTo(right.Length - rightIndex);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    }

    private static string NormalizeSeparators(string value)
    {
        return value.Replace('\\', '/');
    }

    private static int CompareNumberSegments(string left, ref int leftIndex, string right, ref int rightIndex)
    {
        var leftStart = leftIndex;
        var rightStart = rightIndex;

        while (leftIndex < left.Length && char.IsDigit(left[leftIndex]))
        {
            leftIndex++;
        }

        while (rightIndex < right.Length && char.IsDigit(right[rightIndex]))
        {
            rightIndex++;
        }

        var leftTrimmed = TrimLeadingZeroes(left, leftStart, leftIndex);
        var rightTrimmed = TrimLeadingZeroes(right, rightStart, rightIndex);

        var lengthComparison = leftTrimmed.Length.CompareTo(rightTrimmed.Length);
        if (lengthComparison != 0)
        {
            return lengthComparison;
        }

        for (var index = 0; index < leftTrimmed.Length; index++)
        {
            var digitComparison = leftTrimmed[index].CompareTo(rightTrimmed[index]);
            if (digitComparison != 0)
            {
                return digitComparison;
            }
        }

        var originalLengthComparison = (leftIndex - leftStart).CompareTo(rightIndex - rightStart);
        if (originalLengthComparison != 0)
        {
            return originalLengthComparison;
        }

        return 0;
    }

    private static ReadOnlySpan<char> TrimLeadingZeroes(string value, int start, int end)
    {
        while (start < end - 1 && value[start] == '0')
        {
            start++;
        }

        return value.AsSpan(start, end - start);
    }
}
