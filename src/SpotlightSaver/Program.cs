using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SpotlightSaver;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private readonly Label statusLabel;
    private readonly string saveFolder;

    public MainForm()
    {
        saveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "SpotlightSaver"
        );

        Text = "SpotlightSaver";
        Width = 540;
        Height = 245;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var title = new Label
        {
            Text = "SpotlightSaver",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Left = 24,
            Top = 20,
            Width = 460,
            Height = 32
        };

        var subtitle = new Label
        {
            Text = "Save the current Windows wallpaper or Spotlight image before it disappears.",
            Left = 24,
            Top = 60,
            Width = 470,
            Height = 36
        };

        var saveButton = new Button
        {
            Text = "Save Current Wallpaper",
            Left = 24,
            Top = 110,
            Width = 220,
            Height = 38
        };
        saveButton.Click += (_, _) => SaveWallpaper();

        var openButton = new Button
        {
            Text = "Open Save Folder",
            Left = 260,
            Top = 110,
            Width = 180,
            Height = 38
        };
        openButton.Click += (_, _) => OpenSaveFolder();

        statusLabel = new Label
        {
            Text = "Ready.",
            Left = 24,
            Top = 165,
            Width = 470,
            Height = 40
        };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(saveButton);
        Controls.Add(openButton);
        Controls.Add(statusLabel);
    }

    private void SaveWallpaper()
    {
        try
        {
            Directory.CreateDirectory(saveFolder);

            var candidate = WallpaperFinder.FindBestCandidate();

            if (candidate is null)
            {
                statusLabel.Text = "No usable wallpaper image found.";
                MessageBox.Show("No usable wallpaper image found.", "SpotlightSaver");
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var extension = ImageHelper.GetExtension(candidate.Path);
            var outputImage = Path.Combine(saveFolder, $"SpotlightSaver_{timestamp}{extension}");
            var outputText = Path.Combine(saveFolder, $"SpotlightSaver_{timestamp}.txt");

            File.Copy(candidate.Path, outputImage, false);

            File.WriteAllText(
                outputText,
                MetadataHelper.BuildMetadata(outputImage, candidate.Path, candidate.Source),
                Encoding.UTF8
            );

            statusLabel.Text = $"Saved {Path.GetFileName(outputImage)}";

            MessageBox.Show(
                $"Saved image and metadata to:\n\n{saveFolder}",
                "SpotlightSaver",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Save failed.";
            MessageBox.Show(ex.Message, "SpotlightSaver error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenSaveFolder()
    {
        Directory.CreateDirectory(saveFolder);

        Process.Start(new ProcessStartInfo
        {
            FileName = saveFolder,
            UseShellExecute = true
        });
    }
}

public sealed record WallpaperCandidate(string Path, string Source);

public static class WallpaperFinder
{
    private const int SPI_GETDESKWALLPAPER = 0x0073;
    private const int MAX_PATH = 260;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

    public static WallpaperCandidate? FindBestCandidate()
    {
        var candidates = new List<WallpaperCandidate>();

        var current = GetCurrentWallpaperPath();
        if (!string.IsNullOrWhiteSpace(current) && File.Exists(current))
        {
            candidates.Add(new WallpaperCandidate(current, "Current Windows wallpaper"));
        }

        foreach (var file in GetThemeCacheFiles())
        {
            candidates.Add(new WallpaperCandidate(file, "Windows theme cache"));
        }

        foreach (var file in GetSpotlightFiles())
        {
            candidates.Add(new WallpaperCandidate(file, "Windows Spotlight asset cache"));
        }

        return candidates
            .Where(x => File.Exists(x.Path))
            .Where(x => ImageHelper.IsImage(x.Path))
            .OrderByDescending(x => Score(x))
            .FirstOrDefault();
    }

    private static string? GetCurrentWallpaperPath()
    {
        var buffer = new StringBuilder(MAX_PATH);
        SystemParametersInfo(SPI_GETDESKWALLPAPER, buffer.Capacity, buffer, 0);

        var path = buffer.ToString();
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private static IEnumerable<string> GetThemeCacheFiles()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var themeRoot = Path.Combine(appData, "Microsoft", "Windows", "Themes");

        var transcoded = Path.Combine(themeRoot, "TranscodedWallpaper");
        if (File.Exists(transcoded))
        {
            yield return transcoded;
        }

        var cached = Path.Combine(themeRoot, "CachedFiles");
        if (Directory.Exists(cached))
        {
            foreach (var file in Directory.GetFiles(cached))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> GetSpotlightFiles()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var assets = Path.Combine(
            localAppData,
            "Packages",
            "Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy",
            "LocalState",
            "Assets"
        );

        if (!Directory.Exists(assets))
        {
            yield break;
        }

        foreach (var file in Directory.GetFiles(assets))
        {
            yield return file;
        }
    }

    private static long Score(WallpaperCandidate candidate)
    {
        var info = new FileInfo(candidate.Path);
        long score = info.LastWriteTimeUtc.Ticks / 1000000L;

        if (candidate.Source.Contains("Current", StringComparison.OrdinalIgnoreCase))
        {
            score += 9_000_000_000;
        }

        if (candidate.Source.Contains("theme", StringComparison.OrdinalIgnoreCase))
        {
            score += 5_000_000_000;
        }

        try
        {
            using var image = Image.FromFile(candidate.Path);

            if (image.Width > image.Height)
            {
                score += 1_000_000;
            }

            if (image.Width >= 1000 && image.Height >= 700)
            {
                score += 1_000_000;
            }

            score += image.Width * image.Height;
        }
        catch
        {
            score -= 10_000_000;
        }

        return score;
    }
}

public static class ImageHelper
{
    public static bool IsImage(string path)
    {
        try
        {
            using var image = Image.FromFile(path);
            return image.Width >= 500 && image.Height >= 300;
        }
        catch
        {
            return false;
        }
    }

    public static string GetExtension(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[4];
            stream.Read(header);

            if (header[0] == 0xFF && header[1] == 0xD8)
            {
                return ".jpg";
            }

            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                return ".png";
            }

            if (header[0] == 0x42 && header[1] == 0x4D)
            {
                return ".bmp";
            }
        }
        catch
        {
        }

        var extension = Path.GetExtension(path);
        return string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
    }
}

public static class MetadataHelper
{
    public static string BuildMetadata(string savedPath, string originalPath, string source)
    {
        var fileInfo = new FileInfo(savedPath);
        var sb = new StringBuilder();

        sb.AppendLine("SpotlightSaver Metadata");
        sb.AppendLine("=======================");
        sb.AppendLine();
        sb.AppendLine($"Saved file: {Path.GetFileName(savedPath)}");
        sb.AppendLine($"Saved path: {savedPath}");
        sb.AppendLine($"Captured at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Original detected path: {originalPath}");
        sb.AppendLine($"Source type: {source}");
        sb.AppendLine($"File size: {fileInfo.Length:N0} bytes");

        try
        {
            using var image = Image.FromFile(savedPath);

            sb.AppendLine($"Dimensions: {image.Width} x {image.Height}");
            sb.AppendLine();

            sb.AppendLine("Available local metadata:");
            sb.AppendLine($"Title: {ReadProperty(image, 0x0320) ?? "Unknown"}");
            sb.AppendLine($"Description: {ReadProperty(image, 0x010E) ?? "Unknown"}");
            sb.AppendLine($"Artist/Creator: {ReadProperty(image, 0x013B) ?? "Unknown"}");
            sb.AppendLine($"Software: {ReadProperty(image, 0x0131) ?? "Unknown"}");
            sb.AppendLine($"Date taken: {ReadProperty(image, 0x9003) ?? "Unknown"}");
        }
        catch
        {
            sb.AppendLine("Dimensions: Unknown");
        }

        sb.AppendLine();
        sb.AppendLine("Location:");
        sb.AppendLine("Location: Unknown. Windows did not expose reliable location metadata for this image.");
        sb.AppendLine();
        sb.AppendLine("Privacy note:");
        sb.AppendLine("This file was created locally by SpotlightSaver. No image or metadata was uploaded.");

        return sb.ToString();
    }

    private static string? ReadProperty(Image image, int id)
    {
        try
        {
            if (!image.PropertyIdList.Contains(id))
            {
                return null;
            }

            var prop = image.GetPropertyItem(id);
            var value = Encoding.UTF8.GetString(prop.Value).Trim('\0', ' ', '\r', '\n', '\t');

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
}
