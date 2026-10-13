using BitsConverter.Core;

namespace BitsConverter.Core.Tests;

public class BitMathTests
{
    [Theory]
    [InlineData(8, 0xFFu)]
    [InlineData(16, 0xFFFFu)]
    [InlineData(24, 0xFFFFFFu)]
    [InlineData(32, 0xFFFFFFFFu)]
    [InlineData(0, 0u)]
    public void Mask_ReturnsLowBits(int width, uint expected) => Assert.Equal(expected, BitMath.Mask(width));

    [Fact]
    public void Mask_RejectsInvalidWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BitMath.Mask(33));
        Assert.Throws<ArgumentOutOfRangeException>(() => BitMath.Mask(-1));
    }

    [Theory]
    [InlineData(0x81u, 1, 8, 0x02u)]
    [InlineData(0x8001u, 1, 16, 0x0002u)]
    [InlineData(0x80000001u, 4, 32, 0x00000010u)]
    [InlineData(0xFFu, 8, 8, 0u)]
    public void ShiftLeft_DiscardsOverflow(uint value, int count, int width, uint expected) =>
        Assert.Equal(expected, BitMath.ShiftLeft(value, count, width));

    [Theory]
    [InlineData(0x81u, 1, 8, 0x40u)]
    [InlineData(0x80000000u, 31, 32, 1u)]
    [InlineData(0xFFFFFFu, 24, 24, 0u)]
    public void ShiftRight_ShiftsInZeros(uint value, int count, int width, uint expected) =>
        Assert.Equal(expected, BitMath.ShiftRight(value, count, width));

    [Theory]
    [InlineData(0x81u, 1, 8, 0x03u)]
    [InlineData(0x800000u, 1, 24, 0x000001u)]
    [InlineData(0x80000001u, 1, 32, 0x00000003u)]
    [InlineData(0x12u, 8, 8, 0x12u)]
    [InlineData(0x12u, 12, 8, 0x21u)]
    public void RotateLeft_WrapsWithinWidth(uint value, int count, int width, uint expected) =>
        Assert.Equal(expected, BitMath.RotateLeft(value, count, width));

    [Theory]
    [InlineData(0x81u, 1, 8, 0xC0u)]
    [InlineData(0x0001u, 1, 16, 0x8000u)]
    [InlineData(0x00000003u, 1, 32, 0x80000001u)]
    public void RotateRight_WrapsWithinWidth(uint value, int count, int width, uint expected) =>
        Assert.Equal(expected, BitMath.RotateRight(value, count, width));

    [Fact]
    public void RotateLeftThenRight_IsIdentity()
    {
        foreach (var width in BitMath.SupportedWidths)
            for (var n = 0; n < width; n++)
                Assert.Equal(0xA5C3_5A3Cu & BitMath.Mask(width),
                    BitMath.RotateRight(BitMath.RotateLeft(0xA5C3_5A3Cu, n, width), n, width));
    }

    [Fact]
    public void Invert_StaysWithinWidth()
    {
        Assert.Equal(0xFFu, BitMath.Invert(0, 8));
        Assert.Equal(0xFF00u, BitMath.Invert(0x00FF, 16));
        Assert.Equal(0u, BitMath.Invert(uint.MaxValue, 32));
    }

    [Fact]
    public void GetAndSetField()
    {
        const uint value = 0b1011_0110_1100_1010;
        Assert.Equal(0b101101u, BitMath.GetField(value, 15, 10));
        Assert.Equal(0b1011001u, BitMath.GetField(value, 9, 3));
        Assert.Equal(0b010u, BitMath.GetField(value, 2, 0));
        Assert.Equal(value, BitMath.GetField(value, 31, 0));

        Assert.Equal(0b0000_0010_1100_1010u, BitMath.SetField(value, 15, 10, 0));
        Assert.Equal(0b1011_0110_1100_1111u, BitMath.SetField(value, 2, 0, 0b111));
        // Values wider than the field are truncated.
        Assert.Equal(0b1011_0110_1100_1000u, BitMath.SetField(value, 2, 0, 0b1000));
        Assert.Equal(0xDEADBEEFu, BitMath.SetField(value, 31, 0, 0xDEADBEEF));
    }

    [Fact]
    public void SetBit_TogglesSingleBit()
    {
        Assert.Equal(0x80u, BitMath.SetBit(0, 7, true));
        Assert.Equal(0x7Fu, BitMath.SetBit(0xFF, 7, false));
        Assert.True(BitMath.GetBit(0x80000000, 31));
    }
}
