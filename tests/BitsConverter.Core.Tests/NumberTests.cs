using BitsConverter.Core;

namespace BitsConverter.Core.Tests;

public class NumberFormatterTests
{
    [Theory]
    [InlineData(0u, 8, "00")]
    [InlineData(0xABu, 16, "00AB")]
    [InlineData(0x123456u, 24, "123456")]
    [InlineData(0xDEADBEEFu, 32, "DEADBEEF")]
    [InlineData(0x5u, 3, "5")]
    [InlineData(0x3Fu, 6, "3F")]
    public void ToHex_PadsToWidth(uint value, int width, string expected) =>
        Assert.Equal(expected, NumberFormatter.ToHex(value, width));

    [Theory]
    [InlineData(0xA5u, 8, true, "1010 0101")]
    [InlineData(0x1u, 16, true, "0000 0000 0000 0001")]
    [InlineData(0x5u, 6, true, "00 0101")]
    [InlineData(0x5u, 3, true, "101")]
    [InlineData(0xA5u, 8, false, "10100101")]
    public void ToBin_GroupsFromLsb(uint value, int width, bool group, string expected) =>
        Assert.Equal(expected, NumberFormatter.ToBin(value, width, group));

    [Fact]
    public void ToDec_IsUnsigned()
    {
        Assert.Equal("4294967295", NumberFormatter.ToDec(uint.MaxValue, 32));
        Assert.Equal("255", NumberFormatter.ToDec(uint.MaxValue, 8));
    }

    [Theory]
    [InlineData(0x12345678u, 32, true, "78 56 34 12")]
    [InlineData(0x12345678u, 32, false, "12 34 56 78")]
    [InlineData(0x123456u, 24, true, "56 34 12")]
    [InlineData(0x00FFu, 16, false, "00 FF")]
    [InlineData(0xABu, 8, true, "AB")]
    public void ToBytes_RespectsEndianness(uint value, int width, bool littleEndian, string expected) =>
        Assert.Equal(expected, NumberFormatter.ToBytes(value, width, littleEndian));
}

public class NumberParserTests
{
    [Theory]
    [InlineData("ff", 8, 0xFFu)]
    [InlineData("0xFF", 8, 0xFFu)]
    [InlineData("0X00ff", 8, 0xFFu)]
    [InlineData("DEAD_BEEF", 32, 0xDEADBEEFu)]
    [InlineData(" 12 34 ", 16, 0x1234u)]
    [InlineData("000000000001", 8, 1u)]
    public void Hex_Valid(string text, int width, uint expected)
    {
        Assert.True(NumberParser.TryParseHex(text, width, out var value, out var error), error);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("", 8)]
    [InlineData("0x", 8)]
    [InlineData("100", 8)]
    [InlineData("G1", 8)]
    [InlineData("1_0000_0000", 32)]
    [InlineData(null, 8)]
    public void Hex_Invalid(string? text, int width) =>
        Assert.False(NumberParser.TryParseHex(text, width, out _, out _));

    [Theory]
    [InlineData("255", 8, 255u)]
    [InlineData("4294967295", 32, uint.MaxValue)]
    [InlineData("1_000", 16, 1000u)]
    [InlineData("16777215", 24, 0xFFFFFFu)]
    public void Dec_Valid(string text, int width, uint expected)
    {
        Assert.True(NumberParser.TryParseDec(text, width, out var value, out var error), error);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("256", 8)]
    [InlineData("4294967296", 32)]
    [InlineData("99999999999999999999999", 32)]
    [InlineData("-1", 8)]
    [InlineData("1.5", 8)]
    [InlineData("", 8)]
    public void Dec_Invalid(string text, int width) =>
        Assert.False(NumberParser.TryParseDec(text, width, out _, out _));

    [Theory]
    [InlineData("1010 0101", 8, 0xA5u)]
    [InlineData("0b1111", 4, 0xFu)]
    [InlineData("1_0", 2, 2u)]
    [InlineData("00000000001", 8, 1u)]
    public void Bin_Valid(string text, int width, uint expected)
    {
        Assert.True(NumberParser.TryParseBin(text, width, out var value, out var error), error);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("102", 8)]
    [InlineData("1 0000 0000", 8)]
    [InlineData("0b", 8)]
    public void Bin_Invalid(string text, int width) =>
        Assert.False(NumberParser.TryParseBin(text, width, out _, out _));

    [Fact]
    public void Error_DescribesProblem()
    {
        NumberParser.TryParse("300", NumberBase.Dec, 8, out _, out var error);
        Assert.Contains("max 255", error);
        NumberParser.TryParse("xyz", NumberBase.Hex, 8, out _, out error);
        Assert.Contains("'x'", error);
    }
}
