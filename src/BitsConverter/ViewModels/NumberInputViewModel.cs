using BitsConverter.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BitsConverter.ViewModels;

/// <summary>
/// An editable HEX / DEC / BIN text box. Valid input is committed immediately;
/// invalid input only sets <see cref="Error"/> and leaves the value untouched.
/// </summary>
public sealed partial class NumberInputViewModel : ObservableObject
{
    private readonly Func<int> _width;
    private readonly Action<uint> _commit;
    private readonly Action<string> _copy;
    private bool _committing;
    private uint _value;
    private string _text = string.Empty;

    public NumberInputViewModel(NumberBase numberBase, Func<int> width, Action<uint> commit, Action<string> copy)
    {
        Base = numberBase;
        _width = width;
        _commit = commit;
        _copy = copy;
    }

    public NumberBase Base { get; }

    public string Watermark => Base switch
    {
        NumberBase.Hex => "HEX",
        NumberBase.Bin => "BIN",
        _ => "DEC",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _error;

    [ObservableProperty]
    private bool _isEnabled = true;

    public bool HasError => Error is not null;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            OnPropertyChanged();
            Parse();
        }
    }

    /// <summary>Shows <paramref name="value"/> unless this box is the one currently committing it.</summary>
    public void Display(uint value)
    {
        _value = value;
        if (_committing) return;
        Error = null;
        SetTextSilently(Format(value));
    }

    /// <summary>Re-formats the text (e.g. "0x1f" becomes "1F") once the user leaves the box.</summary>
    public void Normalize()
    {
        if (!HasError && IsEnabled) SetTextSilently(Format(_value));
    }

    public void ShowPlaceholder(string text)
    {
        Error = null;
        SetTextSilently(text);
    }

    [RelayCommand]
    private void Copy()
    {
        if (!HasError && IsEnabled) _copy(Format(_value, group: false));
    }

    private void Parse()
    {
        if (NumberParser.TryParse(_text, Base, _width(), out var value, out var error))
        {
            Error = null;
            _value = value;
            _committing = true;
            try
            {
                _commit(value);
            }
            finally
            {
                _committing = false;
            }
        }
        else
        {
            Error = error;
        }
    }

    private string Format(uint value, bool group = true) => Base switch
    {
        NumberBase.Hex => NumberFormatter.ToHex(value, _width()),
        NumberBase.Bin => NumberFormatter.ToBin(value, _width(), group),
        _ => NumberFormatter.ToDec(value, _width()),
    };

    private void SetTextSilently(string text)
    {
        if (_text == text) return;
        _text = text;
        OnPropertyChanged(nameof(Text));
    }
}
