using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace CoatOfArms;

public static class CoatOfArmsPresets
{
    private const string PresetsFolderName = "Presets";
    private const string Extension = ".coa.txt";

    public static string GetPresetsDirectory()
    {
        ModContentPack pack = LoadedModManager.RunningMods?.FirstOrDefault(m => m.PackageId == "kiero298.coatofarms");
        if (pack == null)
            return null;
        return Path.Combine(pack.RootDir, PresetsFolderName);
    }

    public static string GetExportsDirectory()
    {
        if (!string.IsNullOrWhiteSpace(CoatOfArmsSettings.ExportFolderOverride))
            return CoatOfArmsSettings.ExportFolderOverride.Trim();
        string presetsDir = GetPresetsDirectory();
        if (string.IsNullOrEmpty(presetsDir))
            return null;
        return Path.Combine(Path.GetDirectoryName(presetsDir), "Exports");
    }

    public static void EnsureExportsDirectoryExists()
    {
        string dir = GetExportsDirectory();
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>Creates the exports folder if needed and opens it in the system file manager.</summary>
    public static void OpenExportsFolder()
    {
        string dir = GetExportsDirectory();
        if (string.IsNullOrEmpty(dir))
            return;
        EnsureExportsDirectoryExists();
        try
        {
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning("[CoatOfArms] Could not open export folder: " + ex.Message);
        }
    }

    public static void EnsurePresetsDirectoryExists()
    {
        string dir = GetPresetsDirectory();
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static List<string> GetPresetNames()
    {
        string dir = GetPresetsDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return new List<string>();

        List<string> names = new List<string>();
        foreach (string path in Directory.GetFiles(dir, "*" + Extension))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.EndsWith(".coa"))
                name = name.Substring(0, name.Length - 4);
            else
                name = Path.GetFileNameWithoutExtension(path);
            names.Add(name);
        }
        names.Sort();
        return names;
    }

    public static bool SavePreset(string name, CoatOfArmsData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(name))
            return false;

        string dir = GetPresetsDirectory();
        if (string.IsNullOrEmpty(dir))
            return false;

        EnsurePresetsDirectoryExists();
        string safeName = SanitizeFileName(name);
        string path = Path.Combine(dir, safeName + Extension);
        string content = CoatOfArmsSerializer.ToString(data);
        try
        {
            File.WriteAllText(path, content, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("[CoatOfArms] SavePreset failed: " + ex.Message);
            return false;
        }
    }

    public static bool LoadPreset(string name, out CoatOfArmsData data)
    {
        data = null;
        string dir = GetPresetsDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return false;

        string path = Path.Combine(dir, SanitizeFileName(name) + Extension);
        if (!File.Exists(path))
            return false;

        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            return CoatOfArmsSerializer.TryParse(content, out data);
        }
        catch (Exception ex)
        {
            Log.Warning("[CoatOfArms] LoadPreset failed: " + ex.Message);
            return false;
        }
    }

    public static bool DeletePreset(string name)
    {
        string dir = GetPresetsDirectory();
        if (string.IsNullOrEmpty(dir))
            return false;

        string path = Path.Combine(dir, SanitizeFileName(name) + Extension);
        if (!File.Exists(path))
            return false;

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("[CoatOfArms] DeletePreset failed: " + ex.Message);
            return false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Preset";
        char[] invalid = Path.GetInvalidFileNameChars();
        StringBuilder builder = new StringBuilder();
        foreach (char c in name)
        {
            if (c >= 32 && Array.IndexOf(invalid, c) < 0)
                builder.Append(c);
        }
        return builder.Length > 0 ? builder.ToString() : "Preset";
    }

    /// <summary>Renders the coat of arms at high resolution and saves as PNG. Returns the full path if successful, null otherwise.</summary>
    public static string ExportAsPng(CoatOfArmsData data)
    {
        if (data == null)
            return null;

        string dir = GetExportsDirectory();
        if (string.IsNullOrEmpty(dir))
            return null;

        EnsureExportsDirectoryExists();

        string fileName = string.Format("CoatOfArms_{0:yyyy-MM-dd_HH-mm-ss}.png", DateTime.Now);
        string path = Path.Combine(dir, fileName);

        Texture2D texture = CoatOfArmsRenderer.Render(data, CoatOfArmsSettings.Resolution);
        try
        {
            byte[] pngBytes = texture.EncodeToPNG();
            if (pngBytes == null || pngBytes.Length == 0)
                return null;
            File.WriteAllBytes(path, pngBytes);
            return path;
        }
        finally
        {
            if (texture != null)
                UnityEngine.Object.Destroy(texture);
        }
    }
}
