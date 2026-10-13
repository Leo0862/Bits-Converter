using Avalonia.Headless.XUnit;
using BitsConverter.Core;
using BitsConverter.ViewModels;
using BitsConverter.Views;

namespace BitsConverter.Tests;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel Create(int width = 8, uint value = 0) =>
        new(null, new AppState { Width = width, Value = value });

    private static BitViewModel Bit(MainWindowViewModel vm, int index) =>
        vm.ByteRows.SelectMany(r => r.Bits).Single(b => b.Index == index);

    [AvaloniaFact]
    public void Inputs_StayInSync()
    {
        var vm = Create(16);
        vm.HexInput.Text = "0x00ff";

        Assert.Equal(0xFFu, vm.Value);
        Assert.Equal("255", vm.DecInput.Text);
        Assert.Equal("0000 0000 1111 1111", vm.BinInput.Text);
        Assert.Equal("0x00ff", vm.HexInput.Text); // the box being edited is not rewritten
        Assert.True(Bit(vm, 7).IsSet);
        Assert.False(Bit(vm, 8).IsSet);
        Assert.Equal("FF00", vm.InvertedHex);
        Assert.Equal("FF 00", vm.BytesLittleEndian);

        vm.HexInput.Normalize();
        Assert.Equal("00FF", vm.HexInput.Text);
    }

    [AvaloniaFact]
    public void InvalidInput_KeepsValueAndReportsError()
    {
        var vm = Create(8, 0x12);
        vm.DecInput.Text = "300";

        Assert.True(vm.DecInput.HasError);
        Assert.Equal(0x12u, vm.Value);
        Assert.Equal("12", vm.HexInput.Text);

        vm.HexInput.Text = "34";
        Assert.False(vm.DecInput.HasError);
        Assert.Equal("52", vm.DecInput.Text);
    }

    [AvaloniaFact]
    public void CheckingABit_UpdatesValue()
    {
        var vm = Create(32);
        Bit(vm, 31).IsSet = true;
        Assert.Equal(0x80000000u, vm.Value);
        Assert.Equal("80000000", vm.HexInput.Text);
    }

    [AvaloniaFact]
    public void Width_ChangesRowsAndTruncates()
    {
        var vm = Create(32, 0x12345678);
        Assert.Equal(4, vm.ByteRows.Count);
        Assert.Equal("[31:24]", vm.ByteRows[0].Label);

        vm.WidthOptions.Single(o => o.Width == 16).IsSelected = true;
        Assert.Equal(16, vm.Width);
        Assert.Equal(2, vm.ByteRows.Count);
        Assert.Equal(0x5678u, vm.Value);
        Assert.False(vm.WidthOptions.Single(o => o.Width == 32).IsSelected);

        vm.Width = 24;
        Assert.Equal(0x5678u, vm.Value);
        Assert.Equal("005678", vm.HexInput.Text);
    }

    [AvaloniaFact]
    public void Operations()
    {
        var vm = Create(8, 0x81);
        vm.RotateRightCommand.Execute(null);
        Assert.Equal(0xC0u, vm.Value);
        vm.ShiftCount = 2;
        vm.ShiftRightCommand.Execute(null);
        Assert.Equal(0x30u, vm.Value);
        vm.InvertCommand.Execute(null);
        Assert.Equal(0xCFu, vm.Value);
        vm.SetAllCommand.Execute(null);
        Assert.Equal(0xFFu, vm.Value);
        vm.ClearAllCommand.Execute(null);
        Assert.Equal(0u, vm.Value);

        vm.ShiftCount = 100;
        Assert.Equal(7, vm.ShiftCount);
    }

    [AvaloniaFact]
    public void SplitterClicks_DefineFields()
    {
        var vm = Create(16, 0xB6CA);
        Bit(vm, 10).ToggleSplitCommand.Execute(null);
        Bit(vm, 3).ToggleSplitCommand.Execute(null);

        Assert.Equal("[15:10], [9:3], [2:0]", vm.FieldLayoutText);
        Assert.Equal(["[15:10]", "[9:3]", "[2:0]"], vm.FieldRows.Select(r => r.RangeText));
        Assert.Equal(["2D", "59", "2"], vm.FieldRows.Select(r => r.Hex.Text));
        Assert.True(Bit(vm, 10).IsBoundary);
        Assert.False(Bit(vm, 9).IsBoundary);

        // Clicking an existing boundary merges the fields again.
        Bit(vm, 3).ToggleSplitCommand.Execute(null);
        Assert.Equal("[15:10], [9:0]", vm.FieldLayoutText);
    }

    [AvaloniaFact]
    public void EditingFieldValue_WritesBack()
    {
        var vm = Create(16, 0xB6CA);
        vm.FieldLayoutText = "OP[15:10], RS[9:3], [2:0]";
        vm.ApplyFieldLayoutCommand.Execute(null);

        vm.FieldRows[2].Bin.Text = "111";
        Assert.Equal(0xB6CFu, vm.Value);
        vm.FieldRows[0].Dec.Text = "0";
        Assert.Equal(0x02CFu, vm.Value);

        vm.FieldRows[1].Hex.Text = "80"; // 7-bit field: max 7F
        Assert.True(vm.FieldRows[1].Hex.HasError);
        Assert.Equal(0x02CFu, vm.Value);
    }

    [AvaloniaFact]
    public void RenamingField_UpdatesLayoutText()
    {
        var vm = Create(8);
        vm.FieldLayoutText = "7:4, 3:0";
        vm.ApplyFieldLayoutCommand.Execute(null);

        vm.FieldRows[1].Name = "LOW";
        Assert.Equal("[7:4], LOW[3:0]", vm.FieldLayoutText);

        vm.FieldRows[0].Name = "bad,name";
        Assert.True(vm.FieldRows[0].HasNameError);
        Assert.Equal("[7:4], LOW[3:0]", vm.FieldLayoutText);
    }

    [AvaloniaFact]
    public void UnassignedBits_ShowAsRows()
    {
        var vm = Create(16, 0xFFFF);
        vm.FieldLayoutText = "11:8";
        vm.ApplyFieldLayoutCommand.Execute(null);

        Assert.Equal(["[15:12]", "[11:8]", "[7:0]"], vm.FieldRows.Select(r => r.RangeText));
        Assert.True(vm.FieldRows[0].IsUnassigned);
        Assert.False(vm.FieldRows[0].CanRename);
        Assert.Equal("F", vm.FieldRows[0].Hex.Text);
    }

    [AvaloniaFact]
    public void InvalidLayout_IsRejected()
    {
        var vm = Create(8);
        vm.FieldLayoutText = "7:4, 5:0";
        vm.ApplyFieldLayoutCommand.Execute(null);

        Assert.True(vm.HasFieldLayoutError);
        Assert.False(vm.HasFields);

        vm.FieldLayoutText = "7:4";
        Assert.False(vm.HasFieldLayoutError);
    }

    [AvaloniaFact]
    public void ShrinkingWidth_MarksFieldsOutOfRange()
    {
        var vm = Create(32, 0xDEADBEEF);
        vm.FieldLayoutText = "HI[31:16], [15:4]";
        vm.ApplyFieldLayoutCommand.Execute(null);

        vm.Width = 8;
        var hi = vm.FieldRows.Single(r => r.RangeText == "[31:16]");
        var mid = vm.FieldRows.Single(r => r.RangeText == "[15:4]");
        Assert.True(hi.IsOutOfRange);
        Assert.True(mid.IsOutOfRange);
        Assert.False(mid.Hex.IsEnabled);
        Assert.False(Bit(vm, 5).CanToggle);   // inside the clipped [15:4] field
        Assert.True(Bit(vm, 2).CanToggle);    // unassigned [3:0]
        Assert.Equal("HI[31:16], [15:4]", vm.FieldLayoutText);

        vm.Width = 32;
        Assert.All(vm.FieldRows, r => Assert.False(r.IsOutOfRange));
    }

    [AvaloniaFact]
    public void State_RoundTrips()
    {
        var vm = new MainWindowViewModel(null, new AppState
        {
            Width = 24,
            Value = 0xABCDEF,
            ShowTable = true,
            ShiftCount = 3,
            Fields = [new FieldDto { Name = "A", Msb = 23, Lsb = 12 }],
        });
        var state = vm.ToState();

        Assert.Equal(24, state.Width);
        Assert.Equal(0xABCDEFu, state.Value);
        Assert.True(state.ShowTable);
        Assert.Equal(3, state.ShiftCount);
        Assert.Equal("A", Assert.Single(state.Fields).Name);
        Assert.Equal("ABC", vm.FieldRows[0].Hex.Text);
    }

    [AvaloniaFact]
    public void MainWindow_Renders()
    {
        var window = new MainWindow { DataContext = Create(16, 0x1234) };
        window.Show();
        Assert.True(window.IsVisible);
        window.Close();
    }
}
