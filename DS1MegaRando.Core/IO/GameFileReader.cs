using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Areas;
using SoulsFormats;
using System.Reflection;
using System.Xml;

namespace DS1MegaRando.Core.IO;

/// <summary>
/// Reads DSR game files into GameData using SoulsFormats.
/// Params are loaded from GameParam.parambnd.dcx; maps from map\MapStudio\*.msb.dcx.
/// </summary>
public class GameFileReader
{
    private readonly GlobalSettings _settings;

    // Cached layouts loaded once from embedded resources
    private static Dictionary<string, PARAM.Layout>? _layouts;
    private static readonly object _layoutLock = new();

    public GameFileReader(GlobalSettings settings)
    {
        _settings = settings;
    }

    public GameData ReadAll()
    {
        if (!Directory.Exists(_settings.GameDirectory))
            throw new DirectoryNotFoundException(
                $"Game directory not found: {_settings.GameDirectory}");

        var layouts = GetLayouts();
        var data = new GameData();

        ReadParams(data, layouts);
        ReadMapFiles(data);

        return data;
    }

    private void ReadParams(GameData data, Dictionary<string, PARAM.Layout> layouts)
    {
        string parambndPath = Path.Combine(
            _settings.GameDirectory,
            @"param\GameParam\GameParam.parambnd.dcx");

        if (!File.Exists(parambndPath))
            throw new FileNotFoundException("GameParam.parambnd.dcx not found.", parambndPath);

        var bnd = BND3.Read(parambndPath);
        data.ParamBnd = bnd;
        data.ParamBndCompression = bnd.Compression;

        foreach (var file in bnd.Files)
        {
            string paramName = Path.GetFileNameWithoutExtension(file.Name);
            PARAM param;
            try { param = PARAM.Read(file.Bytes); }
            catch { continue; }

            if (layouts.TryGetValue(param.ParamType, out var layout)
                && layout.Size == param.DetectedSize)
            {
                var paramdef = layout.ToParamdef(param.ParamType, out _);
                param.ApplyParamdef(paramdef);
            }

            switch (paramName)
            {
                case "ItemLotParam":    data.ItemLotParam    = param; break;
                case "ShopLineupParam": data.ShopLineupParam = param; break;
                case "NpcParam":        data.NpcParam        = param; break;
                case "CharaInitParam":  data.CharaInitParam  = param; break;
            }
        }
    }

    private void ReadMapFiles(GameData data)
    {
        string msbDir = Path.Combine(_settings.GameDirectory, @"map\MapStudio");
        if (!Directory.Exists(msbDir)) return;

        // DSR uses plain .msb; some builds may have .msb.dcx — handle both
        var msbPaths = Directory.EnumerateFiles(msbDir, "*.msb")
            .Concat(Directory.EnumerateFiles(msbDir, "*.msb.dcx"));

        foreach (var msbPath in msbPaths)
        {
            string fileName = Path.GetFileName(msbPath);
            string mapId = fileName.Replace(".msb.dcx", "").Replace(".msb", "");
            if (MapSpecs.ByMapId(mapId) == null) continue;

            try
            {
                var msb = MSB1.Read(msbPath);
                data.Maps[mapId] = msb;
                data.MapSourcePaths[mapId] = msbPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read {msbPath}: {ex.Message}");
            }
        }
    }

    // ── Layout loading from embedded XML resources ─────────────────────────

    private static Dictionary<string, PARAM.Layout> GetLayouts()
    {
        lock (_layoutLock)
        {
            if (_layouts != null) return _layouts;
            _layouts = LoadEmbeddedLayouts();
            return _layouts;
        }
    }

    private static Dictionary<string, PARAM.Layout> LoadEmbeddedLayouts()
    {
        var result = new Dictionary<string, PARAM.Layout>();

        // Search both Data and Core assemblies for embedded layout XMLs
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(),                             // Core
            typeof(DS1MegaRando.Data.Areas.MapSpecs).Assembly,          // Data
        };

        foreach (var asm in assemblies)
        {
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;

                using var stream = asm.GetManifestResourceStream(name)!;
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(stream);

                // Resource name: "DS1MegaRando.Data.Params.ITEMLOT_PARAM_ST.xml"
                // Split by '.' → last part is "xml", second-to-last is the file stem
                var parts = name.Split('.');
                string paramType = parts.Length >= 2 ? parts[parts.Length - 2] : "";

                try
                {
                    var layout = PARAM.Layout.ReadXMLDoc(xmlDoc);
                    result[paramType] = layout;
                }
                catch { /* skip malformed layouts */ }
            }
        }

        return result;
    }
}
