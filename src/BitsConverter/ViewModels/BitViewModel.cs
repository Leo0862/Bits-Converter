using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BitsConverter.ViewModels;

/// <summary>One bit cell, plus the splitter on its right (between this bit and the next lower one).</summary>
public sealed partial class BitViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;
    private bool _updating;

    public BitViewModel(MainWindowViewModel owner, int index)
    {
        _owner = owner;
        Index = index;
    }

    public int Index { get; }

    public bool HasSplitter => Index > 0;

    public bool IsNibbleEdge => Index % 4 == 0;

    [ObservableProperty]
    private bool _isSet;

    [ObservableProperty]
    private IBrush _fill = Brushes.Transparent;

    [ObservableProperty]
    private string? _toolTip;

    [ObservableProperty]
    private bool _isBoundary;

    [ObservableProperty]
    private bool _canToggle;

    [ObservableProperty]
    private string? _splitterToolTip;

    partial void OnIsSetChanged(bool value)
    {
        if (!_updating) _owner.SetBit(Index, value);
    }

    internal void Update(bool isSet)
    {
        _updating = true;
        IsSet = isSet;
        _updating = false;
    }

    [RelayCommand]
    private void ToggleSplit() => _owner.ToggleBoundary(Index);
}

public sealed partial class ByteRowViewModel : ObservableObject
{
    public ByteRowViewModel(int byteIndex, IReadOnlyList<BitViewModel> bits)
    {
        ByteIndex = byteIndex;
        Bits = bits;
        Label = $"[{byteIndex * 8 + 7}:{byteIndex * 8}]";
    }

    public int ByteIndex { get; }

    public string Label { get; }

    public IReadOnlyList<BitViewModel> Bits { get; }

    [ObservableProperty]
    private string _hex = "00";
}

public sealed partial class WidthOptionViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;
    private bool _updating;

    public WidthOptionViewModel(MainWindowViewModel owner, int width)
    {
        _owner = owner;
        Width = width;
    }

    public int Width { get; }

    public string Label => $"{Width}-bit";

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value && !_updating) _owner.Width = Width;
    }

    internal void Update(bool selected)
    {
        _updating = true;
        IsSelected = selected;
        _updating = false;
    }
}
