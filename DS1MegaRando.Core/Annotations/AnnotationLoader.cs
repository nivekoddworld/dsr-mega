using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DS1MegaRando.Core.Annotations;

public static class AnnotationLoader
{
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static AnnotationData LoadFog()  => LoadEmbedded("DS1MegaRando.Data.Annotations.ds1-fog.yaml");
    public static AnnotationData LoadEnemies() => LoadEmbedded("DS1MegaRando.Data.Annotations.ds1-enemies.yaml");

    private static AnnotationData LoadEmbedded(string resourceName)
    {
        // Try Data assembly first (where the YAML is embedded)
        var dataAsm = Assembly.Load("DS1MegaRando.Data");
        using var stream = dataAsm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);

        try
        {
            return _deserializer.Deserialize<AnnotationData>(reader);
        }
        catch (Exception ex)
        {
            // Flatten the exception chain into one message so the UI can display it
            var msgs = new List<string> { ex.Message };
            var inner = ex.InnerException;
            while (inner != null)
            {
                msgs.Add(inner.Message);
                inner = inner.InnerException;
            }
            throw new InvalidDataException(
                $"Failed to load annotation '{resourceName}': {string.Join(" | ", msgs)}", ex);
        }
    }

    public static AnnotationData LoadFromFile(string path)
    {
        using var reader = new StreamReader(path);
        return _deserializer.Deserialize<AnnotationData>(reader);
    }
}
