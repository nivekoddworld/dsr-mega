#!/usr/bin/env dotnet-script
#r "nuget: SixLabors.ImageSharp, 3.0.0"

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var outputDir = "DarkSoulsModLoader/src/Assets/Textures";
Directory.CreateDirectory(outputDir);

// Create dark leather texture
var darkLeather = new Image<Rgba32>(256, 256, new Rgba32(26, 20, 16));
var rand = new Random(42);

darkLeather.Mutate(ctx =>
{
    for (int y = 0; y < 256; y++)
    {
        for (int x = 0; x < 256; x++)
        {
            int noise = rand.Next(-20, 20);
            byte r = (byte)Math.Max(0, Math.Min(255, 26 + noise));
            byte g = (byte)Math.Max(0, Math.Min(255, 20 + noise - 3));
            byte b = (byte)Math.Max(0, Math.Min(255, 16 + noise - 5));
            darkLeather[x, y] = new Rgba32(r, g, b, 255);
        }
    }
});

darkLeather.SaveAsPng($"{outputDir}/leather_dark.png");
Console.WriteLine("Created leather_dark.png");

// Create border leather texture
var borderLeather = new Image<Rgba32>(256, 256, new Rgba32(107, 90, 71));
rand = new Random(43);

borderLeather.Mutate(ctx =>
{
    for (int y = 0; y < 256; y++)
    {
        for (int x = 0; x < 256; x++)
        {
            int noise = rand.Next(-25, 25);
            byte r = (byte)Math.Max(0, Math.Min(255, 107 + noise));
            byte g = (byte)Math.Max(0, Math.Min(255, 90 + noise - 5));
            byte b = (byte)Math.Max(0, Math.Min(255, 71 + noise - 8));
            borderLeather[x, y] = new Rgba32(r, g, b, 255);
        }
    }
});

borderLeather.SaveAsPng($"{outputDir}/leather_border.png");
Console.WriteLine("Created leather_border.png");
