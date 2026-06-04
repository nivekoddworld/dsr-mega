using DS1MegaRando.Settings;

namespace DS1MegaRando.IO;

public class BackupManager
{
    private static readonly string[] GameFilePaths =
    {
        @"param\GameParam\GameParam.parambnd.dcx",
        @"msg\ENGLISH\menu.msgbnd.dcx",
        @"msg\ENGLISH\item.msgbnd.dcx",
        @"sfx\FRPG_SfxBnd_CommonEffects.ffxbnd.dcx",
    };

    public void BackupAll(GlobalSettings settings)
    {
        string gameDir = settings.GameDirectory;

        // Backup GameParam
        foreach (var relPath in GameFilePaths)
        {
            string fullPath = Path.Combine(gameDir, relPath);
            if (File.Exists(fullPath))
                BackupFile(fullPath);
        }

        // Backup all MSB files
        string msbDir = Path.Combine(gameDir, @"map\MapStudio");
        if (Directory.Exists(msbDir))
        {
            foreach (var msb in Directory.EnumerateFiles(msbDir, "*.msb.dcx"))
                BackupFile(msb);
        }
    }

    private static void BackupFile(string path)
    {
        string bak = path + ".bak";
        if (!File.Exists(bak))
            File.Copy(path, bak);
    }

    public static void RestoreAll(string gameDir)
    {
        // Restore GameParam
        foreach (var relPath in GameFilePaths)
        {
            string fullPath = Path.Combine(gameDir, relPath);
            string bak = fullPath + ".bak";
            if (File.Exists(bak))
                File.Copy(bak, fullPath, overwrite: true);
        }

        // Restore MSBs
        string msbDir = Path.Combine(gameDir, @"map\MapStudio");
        if (Directory.Exists(msbDir))
        {
            foreach (var bak in Directory.EnumerateFiles(msbDir, "*.msb.dcx.bak"))
            {
                string orig = bak[..^4]; // remove .bak
                File.Copy(bak, orig, overwrite: true);
            }
        }
    }
}
