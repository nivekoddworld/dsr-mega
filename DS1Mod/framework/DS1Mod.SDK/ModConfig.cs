using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DS1Mod.SDK;

/// <summary>
/// Typed configuration wrapper for mods.
/// Stores values and type information in a single JSON file.
/// </summary>
public class ModConfig
{
    private Dictionary<string, object> _data = new();
    private Dictionary<string, ConfigFieldInfo> _schema = new();
    private string _filePath = "";

    public string ModName { get; set; } = "";

    public ConfigFieldBuilder AddBool(string key, bool defaultValue = false)
    {
        if (!_data.ContainsKey(key))
            _data[key] = defaultValue;
        var fieldInfo = new ConfigFieldInfo { Type = "bool" };
        _schema[key] = fieldInfo;
        return new ConfigFieldBuilder(fieldInfo);
    }

    public ConfigFieldBuilder AddString(string key, string defaultValue = "")
    {
        if (!_data.ContainsKey(key))
            _data[key] = defaultValue;
        var fieldInfo = new ConfigFieldInfo { Type = "string" };
        _schema[key] = fieldInfo;
        return new ConfigFieldBuilder(fieldInfo);
    }

    public ConfigFieldBuilder AddInt(string key, int defaultValue = 0)
    {
        if (!_data.ContainsKey(key))
            _data[key] = defaultValue;
        var fieldInfo = new ConfigFieldInfo { Type = "int" };
        _schema[key] = fieldInfo;
        return new ConfigFieldBuilder(fieldInfo);
    }

    public ConfigFieldBuilder AddFloat(string key, float defaultValue = 0f)
    {
        if (!_data.ContainsKey(key))
            _data[key] = defaultValue;
        var fieldInfo = new ConfigFieldInfo { Type = "float" };
        _schema[key] = fieldInfo;
        return new ConfigFieldBuilder(fieldInfo);
    }

    public ConfigFieldBuilder AddEnum<T>(string key, T defaultValue) where T : Enum
    {
        if (!_data.ContainsKey(key))
            _data[key] = defaultValue.ToString();

        var enumValues = Enum.GetNames(typeof(T));
        var fieldInfo = new ConfigFieldInfo
        {
            Type = "enum",
            EnumValues = enumValues.ToList()
        };
        _schema[key] = fieldInfo;
        return new ConfigFieldBuilder(fieldInfo);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_data.TryGetValue(key, out var value))
        {
            if (value is bool b) return b;
            if (value is string s && bool.TryParse(s, out var result)) return result;
        }
        return defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (_data.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_data.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is string s && int.TryParse(s, out var result)) return result;
        }
        return defaultValue;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (_data.TryGetValue(key, out var value))
        {
            if (value is float f) return f;
            if (value is double d) return (float)d;
            if (value is string s && float.TryParse(s, out var result)) return result;
        }
        return defaultValue;
    }

    public T GetEnum<T>(string key, T defaultValue) where T : Enum
    {
        if (_data.TryGetValue(key, out var value) && value is string s)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), s);
            }
            catch { }
        }
        return defaultValue;
    }

    public void SetBool(string key, bool value) => _data[key] = value;
    public void SetString(string key, string value) => _data[key] = value;
    public void SetInt(string key, int value) => _data[key] = value;
    public void SetFloat(string key, float value) => _data[key] = value;
    public void SetEnum<T>(string key, T value) where T : Enum => _data[key] = value.ToString();

    public bool HasKey(string key) => _data.ContainsKey(key);

    public async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(_filePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? "");

        var output = new Dictionary<string, object>(_data);
        if (_schema.Count > 0)
        {
            output["__schema"] = _schema;
        }

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public static async Task<ModConfig> LoadAsync(string modName, string filePath)
    {
        var config = new ModConfig { ModName = modName, _filePath = filePath };

        if (!File.Exists(filePath))
            return config;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (data == null)
                return config;

            // Load schema if present
            if (data.TryGetValue("__schema", out var schemaElement))
            {
                var schema = JsonSerializer.Deserialize<Dictionary<string, ConfigFieldInfo>>(schemaElement.GetRawText());
                if (schema != null)
                    config._schema = schema;
                data.Remove("__schema");
            }

            // Load data values
            foreach (var kvp in data)
            {
                config._data[kvp.Key] = ConvertJsonElement(kvp.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load config for mod {modName}: {ex.Message}");
        }

        return config;
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out var i) ? (object)i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.Deserialize<Dictionary<string, object>>() ?? new(),
            _ => ""
        };
    }
}

public class ConfigFieldBuilder
{
    private readonly ConfigFieldInfo _fieldInfo;

    public ConfigFieldBuilder(ConfigFieldInfo fieldInfo)
    {
        _fieldInfo = fieldInfo;
    }

    public ConfigFieldBuilder Tooltip(string description)
    {
        _fieldInfo.Description = description;
        return this;
    }
}

public class ConfigFieldInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("values")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? EnumValues { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
