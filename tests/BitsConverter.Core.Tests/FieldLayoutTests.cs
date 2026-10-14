using BitsConverter.Core;

namespace BitsConverter.Core.Tests;

public class FieldLayoutParserTests
{
    [Fact]
    public void Parse_PlainRanges()
    {
        Assert.True(FieldLayoutParser.TryParse("15:10, 9:3, 2:0", out var fields, out var error), error);
        Assert.Equal([new BitField(15, 10), new BitField(9, 3), new BitField(2, 0)], fields);
    }

    [Fact]
    public void Parse_MixedForms()
    {
        const string text = "OPCODE[15:10]; RS = 9:3\n[2:0]\r\nFLAG[16], 20";
        Assert.True(FieldLayoutParser.TryParse(text, out var fields, out var error), error);
        Assert.Equal(
        [
            new BitField(20, 20),
            new BitField(16, 16, "FLAG"),
            new BitField(15, 10, "OPCODE"),
            new BitField(9, 3, "RS"),
            new BitField(2, 0),
        ], fields);
    }

    [Fact]
    public void Parse_NameEqualsBracket()
    {
        Assert.True(FieldLayoutParser.TryParse("my field = [7:4]", out var fields, out var error), error);
        Assert.Equal(new BitField(7, 4, "my field"), Assert.Single(fields));
    }

    [Fact]
    public void Parse_SwapsReversedRange()
    {
        Assert.True(FieldLayoutParser.TryParse("[0:3]", out var fields, out _));
        Assert.Equal(new BitField(3, 0), Assert.Single(fields));
    }

    [Fact]
    public void Parse_EmptyTextGivesNoFields()
    {
        Assert.True(FieldLayoutParser.TryParse("  , ;", out var fields, out _));
        Assert.Empty(fields);
    }

    [Theory]
    [InlineData("15:10, 12:0", "overlaps")]
    [InlineData("32:0", "between 0 and 31")]
    [InlineData("abc", "Cannot parse")]
    [InlineData("1:2:3", "Cannot parse")]
    [InlineData("A[3:0", "Cannot parse")]
    [InlineData("-1", "Cannot parse")]
    [InlineData("99999999999", "Invalid bit index")]
    public void Parse_RejectsInvalidInput(string text, string expectedError)
    {
        Assert.False(FieldLayoutParser.TryParse(text, out _, out var error));
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void Format_RoundTrips()
    {
        var fields = new[] { new BitField(2, 0), new BitField(15, 10, "OP"), new BitField(7, 7, "F") };
        var text = FieldLayoutParser.Format(fields);
        Assert.Equal("OP[15:10], F[7], [2:0]", text);
        Assert.True(FieldLayoutParser.TryParse(text, out var parsed, out _));
        Assert.Equal(FieldLayout.Sort(fields), parsed);
    }
}

public class FieldLayoutTests
{
    [Fact]
    public void Segments_IncludeUnassignedRuns()
    {
        var segments = FieldLayout.GetSegments([new BitField(12, 10), new BitField(4, 4)], 16);
        Assert.Equal(
        [
            new FieldSegment(15, 13, null),
            new FieldSegment(12, 10, new BitField(12, 10)),
            new FieldSegment(9, 5, null),
            new FieldSegment(4, 4, new BitField(4, 4)),
            new FieldSegment(3, 0, null),
        ], segments);
    }

    [Fact]
    public void Segments_ClipFieldsWiderThanWidth()
    {
        var field = new BitField(15, 4);
        var segments = FieldLayout.GetSegments([field, new BitField(20, 16)], 8);
        Assert.Equal([new FieldSegment(7, 4, field), new FieldSegment(3, 0, null)], segments);
    }

    [Fact]
    public void Segments_NoFieldsIsOneRun() =>
        Assert.Equal([new FieldSegment(31, 0, null)], FieldLayout.GetSegments([], 32));

    [Fact]
    public void Toggle_SplitsUnassignedThenField()
    {
        IReadOnlyList<BitField> fields = [];
        fields = FieldLayout.ToggleBoundary(fields, 16, 10);
        Assert.Equal([new BitField(15, 10), new BitField(9, 0)], fields);

        fields = FieldLayout.ToggleBoundary(fields, 16, 3);
        Assert.Equal([new BitField(15, 10), new BitField(9, 3), new BitField(2, 0)], fields);
    }

    [Fact]
    public void Toggle_SplitKeepsNameOnUpperPart()
    {
        var fields = FieldLayout.ToggleBoundary([new BitField(7, 0, "DATA")], 8, 4);
        Assert.Equal([new BitField(7, 4, "DATA"), new BitField(3, 0)], fields);
    }

    [Fact]
    public void Toggle_MergesFieldsAndKeepsName()
    {
        var fields = FieldLayout.ToggleBoundary([new BitField(7, 4), new BitField(3, 0, "LOW")], 8, 4);
        Assert.Equal([new BitField(7, 0, "LOW")], fields);

        fields = FieldLayout.ToggleBoundary([new BitField(7, 4, "HI"), new BitField(3, 0, "LOW")], 8, 4);
        Assert.Equal([new BitField(7, 0, "HI")], fields);
    }

    [Fact]
    public void Toggle_MergesFieldWithUnassignedRun()
    {
        var fields = FieldLayout.ToggleBoundary([new BitField(5, 4, "A")], 8, 6);
        Assert.Equal([new BitField(7, 4, "A")], fields);

        fields = FieldLayout.ToggleBoundary([new BitField(5, 4, "A")], 8, 4);
        Assert.Equal([new BitField(5, 0, "A")], fields);
    }

    [Fact]
    public void Toggle_IgnoresOutOfRangeFieldsAndEdges()
    {
        IReadOnlyList<BitField> fields = [new BitField(15, 4)];
        Assert.Equal(fields, FieldLayout.ToggleBoundary(fields, 8, 4));
        Assert.Equal(fields, FieldLayout.ToggleBoundary(fields, 8, 6));
        Assert.Equal([new BitField(15, 4), new BitField(3, 2), new BitField(1, 0)], FieldLayout.ToggleBoundary(fields, 8, 2));

        Assert.Empty(FieldLayout.ToggleBoundary([], 8, 0));
        Assert.Empty(FieldLayout.ToggleBoundary([], 8, 8));
    }

    [Fact]
    public void IsBoundary_DetectsSegmentEdges()
    {
        var segments = FieldLayout.GetSegments([new BitField(7, 4)], 8);
        Assert.True(FieldLayout.IsBoundary(segments, 4));
        Assert.False(FieldLayout.IsBoundary(segments, 5));
        Assert.False(FieldLayout.IsBoundary(segments, 3));
    }

    [Theory]
    [InlineData("a,b")]
    [InlineData("x=y")]
    [InlineData("r[0]")]
    [InlineData("a:b")]
    public void IsValidName_RejectsReservedCharacters(string name) => Assert.False(FieldLayout.IsValidName(name));

    [Fact]
    public void Validate_RejectsBadNames() =>
        Assert.False(FieldLayout.TryValidate([new BitField(3, 0, "a,b")], out _));
}

public class StateSerializerTests
{
    [Fact]
    public void Preset_RoundTrips()
    {
        var preset = new FieldPreset
        {
            Width = 16,
            Fields = StateSerializer.ToDto([new BitField(2, 0), new BitField(15, 10, "OPCODE")]),
        };
        var json = StateSerializer.SerializePreset(preset);
        Assert.Contains("\"width\": 16", json);
        Assert.Contains("\"name\": \"OPCODE\"", json);

        Assert.True(StateSerializer.TryDeserializePreset(json, out var loaded, out var fields, out var error), error);
        Assert.Equal(16, loaded.Width);
        Assert.Equal([new BitField(15, 10, "OPCODE"), new BitField(2, 0)], fields);
    }

    [Theory]
    [InlineData("not json", "Invalid preset")]
    [InlineData("""{ "width": 12, "fields": [] }""", "Unsupported width")]
    [InlineData("""{ "width": 8, "fields": [ {"msb":7,"lsb":0}, {"msb":3,"lsb":3} ] }""", "overlaps")]
    [InlineData("null", "Invalid preset")]
    public void Preset_RejectsInvalidFiles(string json, string expectedError)
    {
        Assert.False(StateSerializer.TryDeserializePreset(json, out _, out _, out var error));
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void State_FallsBackToDefaults()
    {
        var state = StateSerializer.DeserializeStateOrDefault("""{ "width": 7, "value": 4095, "shiftCount": 99, "fields": [ {"msb":40,"lsb":0} ] }""");
        Assert.Equal(8, state.Width);
        Assert.Equal(0xFFu, state.Value);
        Assert.Equal(7, state.ShiftCount);
        Assert.Empty(state.Fields);

        Assert.Equal(8, StateSerializer.DeserializeStateOrDefault("{{{").Width);
        Assert.Equal(8, StateSerializer.DeserializeStateOrDefault(null).Width);
    }

    [Fact]
    public void State_RoundTrips()
    {
        var json = StateSerializer.SerializeState(new AppState
        {
            Width = 32, Value = 0xDEADBEEF, ShowTable = true, ShiftCount = 4,
            Fields = StateSerializer.ToDto([new BitField(31, 16, "HI")]),
        });
        var state = StateSerializer.DeserializeStateOrDefault(json);
        Assert.Equal(32, state.Width);
        Assert.Equal(0xDEADBEEFu, state.Value);
        Assert.True(state.ShowTable);
        Assert.True(state.ShowData); // default when not saved
        Assert.Equal(4, state.ShiftCount);
        Assert.Equal([new BitField(31, 16, "HI")], StateSerializer.FromDto(state.Fields));
    }
}
