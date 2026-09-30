using TecAssist.Application.Common;

namespace TecAssist.UnitTests.Rag;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("short", 10, "short")]
    [InlineData("exactly-10", 10, "exactly-10")]
    [InlineData("this string is much longer than the limit", 12, "this string…")]
    public void Truncate_ReturnsExpectedOutput(string value, int maxLength, string expected)
    {
        Assert.Equal(expected, value.Truncate(maxLength));
    }

    [Fact]
    public void Truncate_Throws_ForNegativeMaxLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".Truncate(-1));
    }
}
