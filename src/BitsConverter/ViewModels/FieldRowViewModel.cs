using Avalonia.Media;
using BitsConverter.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BitsConverter.ViewModels;

/// <summary>A row of the bit-field table: a defined field or an unassigned run of bits.</summary>
public sealed partial class FieldRowViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;

    public FieldRowViewModel(MainWindowViewModel owner, int msb, int lsb, BitField? field, IBrush swatch, bool isOutOfRange)
    {
        _owner = owner;
        Msb = msb;
        Lsb = lsb;
        Field = field;
        Swatch = swatch;
        IsOutOfRange = isOutOfRange;
        _name = field?.Name ?? string.Empty;

        Hex = new NumberInputViewModel(NumberBase.Hex, () => BitCount, Commit, owner.CopyText);
        Dec = new NumberInputViewModel(NumberBase.Dec, () => BitCount, Commit, owner.CopyText);
        Bin = new NumberInputViewModel(NumberBase.Bin, () => BitCount, Commit, owner.CopyText);
        foreach (var input in Inputs) input.IsEnabled = !isOutOfRange;
    }

    public int Msb { get; }

    public int Lsb { get; }

    public BitField? Field { get; private set; }

    public IBrush Swatch { get; }

    public bool IsUnassigned => Field is null;

    public bool IsOutOfRange { get; }

    public bool CanRename => !IsUnassigned;

    public int BitCount => Msb - Lsb + 1;

    public string RangeText => Msb == Lsb ? $"[{Msb}]" : $"[{Msb}:{Lsb}]";

    public string BitCountText => BitCount == 1 ? "1 bit" : $"{BitCount} bits";

    public string NameWatermark => IsUnassigned ? "(unassigned)" : "name";

    public string? StatusToolTip => IsOutOfRange ? "Out of range for the current width" : null;

    public NumberInputViewModel Hex { get; }

    public NumberInputViewModel Dec { get; }

    public NumberInputViewModel Bin { get; }

    private IEnumerable<NumberInputViewModel> Inputs => [Hex, Dec, Bin];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNameError))]
    private string? _nameError;

    public bool HasNameError => NameError is not null;

    [ObservableProperty]
    private string _name;

    partial void OnNameChanged(string value)
    {
        if (Field is null) return;
        var trimmed = value.Trim();
        if (!FieldLayout.IsValidName(trimmed))
        {
            NameError = "Name must not contain , ; = : [ ]";
            return;
        }
        NameError = null;
        Field = _owner.RenameField(Field, trimmed);
    }

    internal void Display(uint value)
    {
        if (IsOutOfRange)
        {
            foreach (var input in Inputs) input.ShowPlaceholder("—");
            return;
        }
        var fieldValue = BitMath.GetField(value, Msb, Lsb);
        foreach (var input in Inputs) input.Display(fieldValue);
    }

    private void Commit(uint fieldValue) => _owner.SetFieldValue(Msb, Lsb, fieldValue);
}
