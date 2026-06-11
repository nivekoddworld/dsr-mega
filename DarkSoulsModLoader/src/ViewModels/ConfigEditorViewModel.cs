using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

public class ConfigEditorViewModel : INotifyPropertyChanged
{
    private readonly ModService _modService;
    private string _modName = "";
    private ObservableCollection<ConfigItemViewModel> _configItems = new();
    private string _statusText = "";

    public string ModName
    {
        get => _modName;
        set => SetProperty(ref _modName, value);
    }

    public ObservableCollection<ConfigItemViewModel> ConfigItems
    {
        get => _configItems;
        set => SetProperty(ref _configItems, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConfigEditorViewModel(ModService modService)
    {
        _modService = modService;
    }

    public async Task LoadConfigAsync(string modName)
    {
        ModName = modName;
        ConfigItems.Clear();

        try
        {
            var mod = await _modService.GetModAsync(modName);
            if (mod?.Config == null)
            {
                StatusText = "No config found";
                return;
            }

            // Extract schema if available - it might be a JsonElement
            JsonElement? schemaElement = null;
            if (mod.Config.TryGetValue("__schema", out var schemaValue))
            {
                if (schemaValue is JsonElement elem)
                {
                    schemaElement = elem;
                }
            }

            foreach (var kvp in mod.Config)
            {
                if (kvp.Key == "__schema") continue;

                var item = new ConfigItemViewModel(kvp.Key, kvp.Value, schemaElement);
                ConfigItems.Add(item);
            }

            StatusText = $"Loaded {ConfigItems.Count} settings";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    public async Task SaveConfigAsync()
    {
        try
        {
            var config = new Dictionary<string, object>();

            // Load existing config to preserve __schema
            var mod = await _modService.GetModAsync(_modName);
            if (mod?.Config?.ContainsKey("__schema") == true)
            {
                config["__schema"] = mod.Config["__schema"];
            }

            // Add current config items
            foreach (var item in ConfigItems)
            {
                config[item.Key] = item.Value ?? "";
            }

            await _modService.SaveConfigAsync(_modName, config);
            StatusText = "Config saved!";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

public class ConfigItemViewModel : INotifyPropertyChanged
{
    private string _key = "";
    private object? _value;
    private List<string>? _enumValues;
    private bool _isBool;
    private bool _isEnum;
    private string? _description;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string DisplayKey
    {
        get
        {
            if (string.IsNullOrEmpty(_key))
                return "";

            // Convert camelCase to "Title Case" with spaces
            var result = System.Text.RegularExpressions.Regex.Replace(
                _key,
                "([a-z])([A-Z])",
                "$1 $2");

            // Capitalize first letter
            return char.ToUpperInvariant(result[0]) + result.Substring(1);
        }
    }

    public object? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public bool IsBool
    {
        get => _isBool;
        set => SetProperty(ref _isBool, value);
    }

    public bool IsEnum
    {
        get => _isEnum;
        set => SetProperty(ref _isEnum, value);
    }

    public bool IsTextInput => !IsBool && !IsEnum;

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool HasDescription => !string.IsNullOrEmpty(_description);

    public List<string>? EnumValues
    {
        get => _enumValues;
        set => SetProperty(ref _enumValues, value);
    }

    public List<string>? FormattedEnumValues
    {
        get => _enumValues?.Select(FormatEnumValue).ToList();
    }

    public int SelectedEnumIndex
    {
        get
        {
            if (_enumValues == null || _value == null) return 0;
            var valueStr = _value.ToString() ?? "";
            return _enumValues.IndexOf(valueStr);
        }
        set
        {
            if (_enumValues != null && value >= 0 && value < _enumValues.Count)
            {
                Value = _enumValues[value];
            }
        }
    }

    private static string FormatEnumValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Convert camelCase to "Title Case" with spaces
        var result = Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");

        // Capitalize first letter
        return char.ToUpperInvariant(result[0]) + result.Substring(1);
    }

    public bool BoolValue
    {
        get => Value is bool b ? b : false;
        set
        {
            if (IsBool)
            {
                Value = value;
            }
        }
    }

    public string ValueString
    {
        get => _value?.ToString() ?? "";
        set
        {
            if (_value == null)
            {
                Value = value;
            }
            else
            {
                var type = _value.GetType();
                try
                {
                    object converted = type switch
                    {
                        _ when type == typeof(bool) => bool.Parse(value),
                        _ when type == typeof(int) => int.Parse(value),
                        _ when type == typeof(float) => float.Parse(value),
                        _ when type == typeof(double) => double.Parse(value),
                        _ => value
                    };
                    Value = converted;
                }
                catch
                {
                    Value = value;
                }
            }
        }
    }

    public string ValueType => _value?.GetType().Name ?? "Unknown";

    public TextBlock? FormattedDescription => GetFormattedDescription();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConfigItemViewModel(string key, object? value, JsonElement? schema = null)
    {
        Key = key;
        Value = value;

        // Extract description from schema first (before any early returns)
        if (schema.HasValue && schema.Value.TryGetProperty(key, out var keySchema))
        {
            // Extract description if available
            if (keySchema.TryGetProperty("description", out var descElem) && descElem.ValueKind == JsonValueKind.String)
            {
                Description = descElem.GetString();
            }
        }

        // Infer control type from value's actual type
        if (value != null)
        {
            // Check if it's a JsonElement boolean
            if (value is JsonElement jsonElem)
            {
                if (jsonElem.ValueKind == JsonValueKind.True || jsonElem.ValueKind == JsonValueKind.False)
                {
                    IsBool = true;
                    // Convert to actual bool for easier handling
                    Value = jsonElem.GetBoolean();
                    return;
                }
            }

            var valueType = value.GetType();

            if (valueType == typeof(bool) || valueType == typeof(bool?))
            {
                IsBool = true;
            }
            else if (valueType.IsEnum)
            {
                IsEnum = true;
                EnumValues = Enum.GetNames(valueType).ToList();
            }
        }

        // Check schema for enum info (do this after type checks)
        if (schema.HasValue && schema.Value.TryGetProperty(key, out var keySchema2))
        {
            // Check if type is "enum"
            if (keySchema2.TryGetProperty("type", out var typeElem) && typeElem.ValueKind == JsonValueKind.String)
            {
                var typeName = typeElem.GetString();
                if (typeName == "enum" && keySchema2.TryGetProperty("values", out var valuesElem) && valuesElem.ValueKind == JsonValueKind.Array)
                {
                    IsEnum = true;
                    EnumValues = new List<string>();
                    foreach (var val in valuesElem.EnumerateArray())
                    {
                        string enumValue = val.ValueKind == JsonValueKind.String
                            ? val.GetString() ?? ""
                            : val.ToString();
                        if (!string.IsNullOrEmpty(enumValue))
                        {
                            EnumValues.Add(enumValue);
                        }
                    }

                    // Convert Value to string for ComboBox binding
                    if (Value is JsonElement jsonValue)
                    {
                        Value = jsonValue.ValueKind == JsonValueKind.String
                            ? jsonValue.GetString() ?? ""
                            : jsonValue.ToString();
                    }
                }
            }
        }
    }

    private TextBlock? GetFormattedDescription()
    {
        if (string.IsNullOrEmpty(_description))
            return null;

        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 600,
            Padding = new Avalonia.Thickness(12),
            Foreground = new SolidColorBrush(Color.Parse("#cccccc"))
        };

        var parts = Regex.Split(_description, @"(\*[^*]+\*)");

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("*") && part.EndsWith("*"))
            {
                var run = new Run
                {
                    Text = part.Trim('*'),
                    Foreground = new SolidColorBrush(Color.Parse("#d4af37")),
                    FontWeight = FontWeight.Bold
                };
                textBlock.Inlines?.Add(run);
            }
            else
            {
                textBlock.Inlines?.Add(new Run { Text = part });
            }
        }

        return textBlock;
    }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
