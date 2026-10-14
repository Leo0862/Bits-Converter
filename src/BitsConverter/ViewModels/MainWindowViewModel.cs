using System.Collections.ObjectModel;
using Avalonia.Media;
using BitsConverter.Core;
using BitsConverter.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BitsConverter.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    // Semi-transparent so the same palette reads well on light and dark backgrounds.
    private static readonly IBrush[] FieldPalette =
    [
        new SolidColorBrush(Color.Parse("#663B82F6")),
        new SolidColorBrush(Color.Parse("#66F59E0B")),
        new SolidColorBrush(Color.Parse("#6610B981")),
        new SolidColorBrush(Color.Parse("#6606B6D4")),
        new SolidColorBrush(Color.Parse("#668B5CF6")),
        new SolidColorBrush(Color.Parse("#6614B8A6")),
        new SolidColorBrush(Color.Parse("#66F97316")),
        new SolidColorBrush(Color.Parse("#6684CC16")),
    ];

    private static readonly IBrush OutOfRangeBrush = new SolidColorBrush(Color.Parse("#80EF4444"));

    private readonly IUiServices? _services;
    private readonly BitViewModel[] _allBits;
    private IReadOnlyList<BitField> _fields = [];
    private int _width;
    private uint _value;

    public MainWindowViewModel()
        : this(null, new AppState())
    {
    }

    public MainWindowViewModel(IUiServices? services, AppState state)
    {
        _services = services;
        _allBits = Enumerable.Range(0, BitMath.MaxWidth).Select(i => new BitViewModel(this, i)).ToArray();
        WidthOptions = BitMath.SupportedWidths.Select(w => new WidthOptionViewModel(this, w)).ToList();

        DecInput = new NumberInputViewModel(NumberBase.Dec, () => Width, ApplyValue, CopyText);
        HexInput = new NumberInputViewModel(NumberBase.Hex, () => Width, ApplyValue, CopyText);
        BinInput = new NumberInputViewModel(NumberBase.Bin, () => Width, ApplyValue, CopyText);

        _isTableExpanded = state.ShowTable;
        _isDataExpanded = state.ShowData;
        _width = BitMath.IsSupportedWidth(state.Width) ? state.Width : 8;
        _shiftCount = Math.Clamp(state.ShiftCount, 1, _width - 1);
        _fields = FieldLayout.Sort(StateSerializer.FromDto(state.Fields));
        _fieldLayoutText = FieldLayoutParser.Format(_fields);

        OnWidthChanged();
        ApplyValue(state.Value);
    }

    public IReadOnlyList<WidthOptionViewModel> WidthOptions { get; }

    public ObservableCollection<ByteRowViewModel> ByteRows { get; } = [];

    public ObservableCollection<FieldRowViewModel> FieldRows { get; } = [];

    public NumberInputViewModel DecInput { get; }

    public NumberInputViewModel HexInput { get; }

    public NumberInputViewModel BinInput { get; }

    public bool HasFields => _fields.Count > 0;

    public int Width
    {
        get => _width;
        set
        {
            if (_width == value || !BitMath.IsSupportedWidth(value)) return;
            _width = value;
            OnPropertyChanged();
            OnWidthChanged();
            ApplyValue(_value);
        }
    }

    public uint Value => _value;

    public int MaxShift => Width - 1;

    [ObservableProperty]
    private int _shiftCount = 1;

    [ObservableProperty]
    private bool _isTableExpanded;

    [ObservableProperty]
    private bool _isDataExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFieldLayoutError))]
    private string? _fieldLayoutError;

    public bool HasFieldLayoutError => FieldLayoutError is not null;

    [ObservableProperty]
    private string _fieldLayoutText = string.Empty;

    [ObservableProperty]
    private string? _statusText;

    public string DataHex => NumberFormatter.ToHex(_value, Width, group: true);
    public string DataBin => NumberFormatter.ToBin(_value, Width);
    public string DataDec => NumberFormatter.ToDec(_value, Width);
    public string InvertedHex => NumberFormatter.ToHex(BitMath.Invert(_value, Width), Width, group: true);
    public string InvertedBin => NumberFormatter.ToBin(BitMath.Invert(_value, Width), Width);
    // Copy forms: HEX with a 0x prefix, BIN without the nibble separators, so they paste as literals.
    public string DataHexCopy => "0x" + NumberFormatter.ToHex(_value, Width);
    public string InvertedHexCopy => "0x" + NumberFormatter.ToHex(BitMath.Invert(_value, Width), Width);
    public string DataBinPlain => NumberFormatter.ToBin(_value, Width, group: false);
    public string InvertedBinPlain => NumberFormatter.ToBin(BitMath.Invert(_value, Width), Width, group: false);
    public string InvertedDec => NumberFormatter.ToDec(BitMath.Invert(_value, Width), Width);
    public string BytesLittleEndian => NumberFormatter.ToBytes(_value, Width, littleEndian: true);
    public string BytesBigEndian => NumberFormatter.ToBytes(_value, Width, littleEndian: false);

    public IReadOnlyList<HexTableRow> HexTableLow { get; } = HexTableRow.Create(0, 8);
    public IReadOnlyList<HexTableRow> HexTableHigh { get; } = HexTableRow.Create(8, 8);

    public AppState ToState() => new()
    {
        Width = Width,
        Value = _value,
        Fields = StateSerializer.ToDto(_fields),
        ShowTable = IsTableExpanded,
        ShowData = IsDataExpanded,
        ShiftCount = ShiftCount,
    };

    // ---- Value -----------------------------------------------------------------------------

    internal void ApplyValue(uint value)
    {
        _value = value & BitMath.Mask(Width);

        foreach (var bit in _allBits)
            bit.Update(BitMath.GetBit(_value, bit.Index));
        foreach (var row in ByteRows)
            row.Hex = NumberFormatter.ToHex(BitMath.GetField(_value, row.ByteIndex * 8 + 7, row.ByteIndex * 8), 8);

        DecInput.Display(_value);
        HexInput.Display(_value);
        BinInput.Display(_value);
        foreach (var row in FieldRows)
            row.Display(_value);

        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(DataHex));
        OnPropertyChanged(nameof(DataHexCopy));
        OnPropertyChanged(nameof(DataBin));
        OnPropertyChanged(nameof(DataBinPlain));
        OnPropertyChanged(nameof(DataDec));
        OnPropertyChanged(nameof(InvertedHex));
        OnPropertyChanged(nameof(InvertedHexCopy));
        OnPropertyChanged(nameof(InvertedBin));
        OnPropertyChanged(nameof(InvertedBinPlain));
        OnPropertyChanged(nameof(InvertedDec));
        OnPropertyChanged(nameof(BytesLittleEndian));
        OnPropertyChanged(nameof(BytesBigEndian));
    }

    internal void SetBit(int index, bool on) => ApplyValue(BitMath.SetBit(_value, index, on));

    internal void SetFieldValue(int msb, int lsb, uint fieldValue) =>
        ApplyValue(BitMath.SetField(_value, msb, lsb, fieldValue));

    [RelayCommand]
    private void SetAll() => ApplyValue(uint.MaxValue);

    [RelayCommand]
    private void ClearAll() => ApplyValue(0);

    [RelayCommand]
    private void Invert() => ApplyValue(BitMath.Invert(_value, Width));

    [RelayCommand]
    private void ShiftLeft() => ApplyValue(BitMath.ShiftLeft(_value, ShiftCount, Width));

    [RelayCommand]
    private void ShiftRight() => ApplyValue(BitMath.ShiftRight(_value, ShiftCount, Width));

    [RelayCommand]
    private void RotateLeft() => ApplyValue(BitMath.RotateLeft(_value, ShiftCount, Width));

    [RelayCommand]
    private void RotateRight() => ApplyValue(BitMath.RotateRight(_value, ShiftCount, Width));

    partial void OnShiftCountChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, MaxShift);
        if (clamped != value) ShiftCount = clamped;
    }

    // ---- Width -----------------------------------------------------------------------------

    private void OnWidthChanged()
    {
        foreach (var option in WidthOptions)
            option.Update(option.Width == Width);

        ByteRows.Clear();
        for (var byteIndex = Width / 8 - 1; byteIndex >= 0; byteIndex--)
        {
            var bits = Enumerable.Range(0, 8).Select(i => _allBits[byteIndex * 8 + 7 - i]).ToList();
            ByteRows.Add(new ByteRowViewModel(byteIndex, bits));
        }

        OnPropertyChanged(nameof(MaxShift));
        ShiftCount = Math.Clamp(ShiftCount, 1, MaxShift);
        RefreshFieldViews();
    }

    // ---- Fields ----------------------------------------------------------------------------

    internal void ToggleBoundary(int upperBit) =>
        SetFields(FieldLayout.ToggleBoundary(_fields, Width, upperBit));

    internal BitField RenameField(BitField field, string name)
    {
        var renamed = field with { Name = name };
        _fields = FieldLayout.Sort(_fields.Select(f => f == field ? renamed : f));
        FieldLayoutText = FieldLayoutParser.Format(_fields);
        FieldLayoutError = null;
        UpdateBitDecorations();
        return renamed;
    }

    [RelayCommand]
    private void ApplyFieldLayout()
    {
        if (FieldLayoutParser.TryParse(FieldLayoutText, out var fields, out var error))
            SetFields(fields);
        else
            FieldLayoutError = error;
    }

    [RelayCommand]
    private void ClearFields() => SetFields([]);

    partial void OnFieldLayoutTextChanged(string value) => FieldLayoutError = null;

    private void SetFields(IReadOnlyList<BitField> fields)
    {
        _fields = FieldLayout.Sort(fields);
        FieldLayoutText = FieldLayoutParser.Format(_fields);
        FieldLayoutError = null;
        RefreshFieldViews();
    }

    private void RefreshFieldViews()
    {
        UpdateBitDecorations();

        FieldRows.Clear();
        if (_fields.Count > 0)
        {
            var rows = new List<FieldRowViewModel>();
            foreach (var segment in FieldLayout.GetSegments(_fields, Width))
            {
                rows.Add(segment.Field is { } f
                    ? new FieldRowViewModel(this, f.Msb, f.Lsb, f, BrushFor(f), !f.IsInRange(Width))
                    : new FieldRowViewModel(this, segment.Msb, segment.Lsb, null, Brushes.Transparent, false));
            }
            // Fields that lie completely above the current width have no segment.
            foreach (var f in _fields.Where(f => f.Lsb >= Width))
                rows.Add(new FieldRowViewModel(this, f.Msb, f.Lsb, f, BrushFor(f), true));

            foreach (var row in rows.OrderByDescending(r => r.Msb))
            {
                row.Display(_value);
                FieldRows.Add(row);
            }
        }

        OnPropertyChanged(nameof(HasFields));
    }

    private void UpdateBitDecorations()
    {
        var segments = FieldLayout.GetSegments(_fields, Width);
        for (var i = 0; i < Width; i++)
        {
            var bit = _allBits[i];
            var field = FieldLayout.SegmentOf(segments, i).Field;
            bit.Fill = field is null ? Brushes.Transparent : field.IsInRange(Width) ? BrushFor(field) : OutOfRangeBrush;
            bit.ToolTip = field is null
                ? $"Bit {i}"
                : $"Bit {i} — {(string.IsNullOrEmpty(field.Name) ? "field" : field.Name)} {field.RangeText}" +
                  (field.IsInRange(Width) ? string.Empty : " (out of range)");

            if (i == 0) continue;
            bit.IsBoundary = FieldLayout.IsBoundary(segments, i);
            bit.CanToggle = FieldLayout.CanToggleBoundary(segments, i, Width);
            bit.SplitterToolTip = !bit.CanToggle
                ? "Locked: adjacent field is out of range"
                : bit.IsBoundary
                    ? $"Merge fields at bit {i}|{i - 1}"
                    : $"Split between bit {i} and {i - 1}";
        }
    }

    private IBrush BrushFor(BitField field)
    {
        var index = 0;
        for (; index < _fields.Count; index++)
            if (_fields[index].Msb == field.Msb) break;
        return FieldPalette[index % FieldPalette.Length];
    }

    // ---- Clipboard & presets ---------------------------------------------------------------

    internal void CopyText(string text) => _ = CopyAsync(text);

    [RelayCommand]
    private Task CopyAsync(string? text)
    {
        if (string.IsNullOrEmpty(text) || _services is null) return Task.CompletedTask;
        StatusText = $"Copied \"{text}\"";
        return _services.SetClipboardTextAsync(text);
    }

    [RelayCommand]
    private async Task LoadPresetAsync()
    {
        if (_services is null) return;
        var json = await _services.OpenPresetAsync();
        if (json is null) return;

        if (!StateSerializer.TryDeserializePreset(json, out var preset, out var fields, out var error))
        {
            StatusText = error;
            return;
        }
        Width = preset.Width;
        SetFields(fields);
        StatusText = $"Loaded preset: {Width}-bit, {fields.Count} field(s)";
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        if (_services is null) return;
        var json = StateSerializer.SerializePreset(new FieldPreset { Width = Width, Fields = StateSerializer.ToDto(_fields) });
        if (await _services.SavePresetAsync(json))
            StatusText = "Preset saved";
    }
}

public sealed record HexTableRow(string Dec, string Bin, string Hex)
{
    public static IReadOnlyList<HexTableRow> Create(int start, int count) =>
        Enumerable.Range(start, count)
            .Select(i => new HexTableRow(i.ToString(), NumberFormatter.ToBin((uint)i, 4), NumberFormatter.ToHex((uint)i, 4)))
            .ToList();
}
