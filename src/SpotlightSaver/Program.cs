using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
    private readonly string saveFolder;
    private readonly SleekButton saveButton;
    private readonly SleekButton openFolderButton;
    private readonly Label statusLabel;
    private readonly LinkLabel lastSavedLink;
    private readonly Label saveLocationLabel;
    private readonly CheckBox aiCheckBox;
    private readonly Label aiHintLabel;
    private string? lastSavedPath;

    public MainForm()
    {
        saveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "SpotlightSaver"
        );

        Text = "SpotlightSaver";
        Width = 790;
        Height = 485;
        MinimumSize = new Size(790, 485);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Palette.Background;
        DoubleBuffered = true;

        var shell = new RoundedPanel
        {
            Left = 24,
            Top = 24,
            Width = 730,
            Height = 380,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Palette.Card,
            Radius = 24
        };

        var title = new Label
        {
            Text = "SpotlightSaver",
            Font = new Font("Segoe UI Variable Display", 24, FontStyle.Bold),
            ForeColor = Palette.Text,
            BackColor = Palette.Card,
            Left = 30,
            Top = 24,
            Width = 520,
            Height = 42
        };

        var subtitle = new Label
        {
            Text = "Save Windows wallpapers and Spotlight images before they disappear.",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.Card,
            Left = 32,
            Top = 72,
            Width = 650,
            Height = 26
        };

        saveLocationLabel = new Label
        {
            Text = $"Default save location: {saveFolder}",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.Card,
            Left = 32,
            Top = 102,
            Width = 660,
            Height = 25
        };

        aiCheckBox = new CheckBox
        {
            Text = "Use local AI analysis with Ollama",
            Checked = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Palette.Text,
            BackColor = Palette.Card,
            Left = 32,
            Top = 132,
            Width = 310,
            Height = 28
        };

        aiHintLabel = new Label
        {
            Text = "Looks for local vision models at http://localhost:11434. No cloud upload.",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.Card,
            Left = 350,
            Top = 137,
            Width = 360,
            Height = 24
        };

        saveButton = new SleekButton
        {
            Text = $"Save Current Wallpaper{Environment.NewLine}Default: {saveFolder}",
            Left = 32,
            Top = 175,
            Width = 660,
            Height = 78,
            Radius = 18,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            NormalColor = Palette.Accent,
            HoverColor = Palette.AccentHover,
            PressedColor = Palette.AccentPressed,
            TextColor = Color.White
        };
        saveButton.Click += async (_, _) => await SaveWallpaperAsync();

        openFolderButton = new SleekButton
        {
            Text = "Open Save Folder",
            Left = 32,
            Top = 268,
            Width = 190,
            Height = 46,
            Radius = 16,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            NormalColor = Palette.Secondary,
            HoverColor = Palette.SecondaryHover,
            PressedColor = Palette.SecondaryPressed,
            TextColor = Palette.Text
        };
        openFolderButton.Click += (_, _) => OpenPath(saveFolder);

        statusLabel = new Label
        {
            Text = "Ready. AI analysis is optional and local-only.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.Card,
            Left = 238,
            Top = 274,
            Width = 455,
            Height = 22
        };

        lastSavedLink = new LinkLabel
        {
            Text = "",
            Visible = false,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            LinkColor = Palette.Link,
            ActiveLinkColor = Palette.LinkActive,
            VisitedLinkColor = Palette.Link,
            BackColor = Palette.Card,
            Left = 238,
            Top = 298,
            Width = 455,
            Height = 28
        };
        lastSavedLink.LinkClicked += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(lastSavedPath) && File.Exists(lastSavedPath))
            {
                OpenPath(lastSavedPath);
            }
        };

        var privacyBadge = new Label
        {
            Text = "Local only • No upload • No telemetry",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.CardAlt,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 32,
            Top = 330,
            Width = 230,
            Height = 28
        };

        var aiBadge = new Label
        {
            Text = "AI notes are guesses, not facts",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.CardAlt,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 278,
            Top = 330,
            Width = 230,
            Height = 28
        };

        shell.Controls.Add(title);
        shell.Controls.Add(subtitle);
        shell.Controls.Add(saveLocationLabel);
        shell.Controls.Add(aiCheckBox);
        shell.Controls.Add(aiHintLabel);
        shell.Controls.Add(saveButton);
        shell.Controls.Add(openFolderButton);
        shell.Controls.Add(statusLabel);
        shell.Controls.Add(lastSavedLink);
        shell.Controls.Add(privacyBadge);
        shell.Controls.Add(aiBadge);

        var footer = new Label
        {
            Text = "By Clinton Kosh",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.Background,
            TextAlign = ContentAlignment.MiddleRight,
            Left = 566,
            Top = 415,
            Width = 185,
            Height = 24,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        Controls.Add(shell);
        Controls.Add(footer);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var brush = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(20, 24, 36),
            Color.FromArgb(9, 12, 20),
            LinearGradientMode.ForwardDiagonal
        );

        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    private async Task SaveWallpaperAsync()
    {
        saveButton.Enabled = false;
        openFolderButton.Enabled = false;
        aiCheckBox.Enabled = false;

        try
        {
            Directory.CreateDirectory(saveFolder);

            statusLabel.Text = "Finding current wallpaper...";
            Refresh();

            var candidate = WallpaperFinder.FindBestCandidate();

            if (candidate is null)
            {
                statusLabel.Text = "No usable wallpaper image found.";
                lastSavedLink.Visible = false;

                SleekDialog.ShowInfo(
                    this,
                    "No image found",
                    "SpotlightSaver could not find a usable wallpaper or Spotlight image."
                );

                return;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var extension = ImageHelper.GetExtension(candidate.Path);
            var outputImage = Path.Combine(saveFolder, $"SpotlightSaver_{timestamp}{extension}");
            var outputText = Path.Combine(saveFolder, $"SpotlightSaver_{timestamp}.txt");

            File.Copy(candidate.Path, outputImage, false);

            string aiAnalysis;

            if (aiCheckBox.Checked)
            {
                statusLabel.Text = "Saved image. Asking local Ollama vision model...";
                Refresh();

                aiAnalysis = await LocalAiAnalyzer.AnalyzeImageAsync(outputImage);
            }
            else
            {
                aiAnalysis = "Local AI analysis: Skipped by user.";
            }

            File.WriteAllText(
                outputText,
                MetadataHelper.BuildMetadata(outputImage, candidate.Path, candidate.Source, aiAnalysis),
                Encoding.UTF8
            );

            lastSavedPath = outputImage;
            lastSavedLink.Text = Path.GetFileName(outputImage);
            lastSavedLink.Visible = true;

            statusLabel.Text = "Saved successfully. Click the filename to open it.";

            SleekDialog.ShowSaved(this, outputImage, outputText, saveFolder, aiAnalysis);
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Save failed.";
            lastSavedLink.Visible = false;

            SleekDialog.ShowInfo(
                this,
                "Save failed",
                ex.Message
            );
        }
        finally
        {
            saveButton.Enabled = true;
            openFolderButton.Enabled = true;
            aiCheckBox.Enabled = true;
        }
    }

    private static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}

public static class LocalAiAnalyzer
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(90)
    };

    private static readonly string[] PreferredVisionModelNames =
    [
        "llama3.2-vision",
        "llava",
        "bakllava",
        "moondream",
        "gemma3"
    ];

    public static async Task<string> AnalyzeImageAsync(string imagePath)
    {
        try
        {
            var model = await FindVisionModelAsync();

            if (string.IsNullOrWhiteSpace(model))
            {
                return """
Local AI analysis: Unavailable.

Reason:
No likely Ollama vision model was found.

Install one locally, for example:
ollama pull llama3.2-vision
or:
ollama pull llava

Then rerun SpotlightSaver.
""";
            }

            var base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));

            var prompt = """
You are analyzing a Windows wallpaper or Windows Spotlight background image saved locally.

Provide a careful best-effort visual analysis.

Include:
1. A concise scene description.
2. Visible natural features, architecture, landmarks, signs, text, vehicles, terrain, plants, water, mountains, city details, or cultural clues.
3. Possible location, region, or country if visual clues support it.
4. Confidence level for any location guess.
5. A warning if the location is unknown or uncertain.

Rules:
- Do not claim a precise location unless the image itself strongly supports it.
- Do not pretend Microsoft metadata is available.
- Do not invent.
- Label guesses clearly.
- Keep it under 220 words.
""";

            var request = new
            {
                model,
                prompt,
                images = new[] { base64Image },
                stream = false,
                options = new
                {
                    temperature = 0.1,
                    num_predict = 350
                }
            };

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await Client.PostAsync("http://localhost:11434/api/generate", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"""
Local AI analysis: Failed.

Model attempted:
{model}

HTTP status:
{(int)response.StatusCode} {response.ReasonPhrase}

Response:
{TrimForMetadata(responseText)}
""";
            }

            using var doc = JsonDocument.Parse(responseText);

            if (doc.RootElement.TryGetProperty("response", out var result))
            {
                var analysis = result.GetString();

                if (!string.IsNullOrWhiteSpace(analysis))
                {
                    return $"""
Local AI analysis generated by Ollama model:
{model}

{analysis.Trim()}
""";
                }
            }

            return $"""
Local AI analysis: Failed.

Model attempted:
{model}

Reason:
Ollama returned no usable response text.
""";
        }
        catch (HttpRequestException ex)
        {
            return $"""
Local AI analysis: Unavailable.

Reason:
Could not connect to Ollama at http://localhost:11434.

Details:
{ex.Message}

Fix:
Start Ollama and make sure a vision model is installed.
""";
        }
        catch (TaskCanceledException)
        {
            return """
Local AI analysis: Timed out.

Reason:
The local vision model took too long to respond.

Fix:
Try a smaller model such as moondream or make sure Ollama is running with GPU acceleration.
""";
        }
        catch (Exception ex)
        {
            return $"""
Local AI analysis: Failed.

Details:
{ex.Message}
""";
        }
    }

    private static async Task<string?> FindVisionModelAsync()
    {
        try
        {
            using var response = await Client.GetAsync("http://localhost:11434/api/tags");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseText = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseText);

            if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var installed = new List<string>();

            foreach (var model in models.EnumerateArray())
            {
                if (model.TryGetProperty("name", out var nameProp))
                {
                    var name = nameProp.GetString();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        installed.Add(name);
                    }
                }
            }

            foreach (var preferred in PreferredVisionModelNames)
            {
                var match = installed.FirstOrDefault(x =>
                    x.Contains(preferred, StringComparison.OrdinalIgnoreCase)
                );

                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string TrimForMetadata(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        value = value.Trim();

        return value.Length <= 1200 ? value : value[..1200] + "...";
    }
}

public sealed class SleekDialog : Form
{
    private SleekDialog(string titleText, string message, string? filePath, string? metadataPath, string? folderPath, string? aiAnalysis)
    {
        Text = titleText;
        Width = 620;
        Height = 390;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Palette.Background;
        DoubleBuffered = true;

        var card = new RoundedPanel
        {
            Left = 18,
            Top = 18,
            Width = 566,
            Height = 300,
            Radius = 20,
            BackColor = Palette.Card
        };

        var title = new Label
        {
            Text = titleText,
            Font = new Font("Segoe UI Variable Display", 17, FontStyle.Bold),
            ForeColor = Palette.Text,
            BackColor = Palette.Card,
            Left = 24,
            Top = 20,
            Width = 510,
            Height = 34
        };

        var body = new Label
        {
            Text = message,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.Card,
            Left = 24,
            Top = 58,
            Width = 510,
            Height = 42
        };

        card.Controls.Add(title);
        card.Controls.Add(body);

        var y = 105;

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var link = new LinkLabel
            {
                Text = Path.GetFileName(filePath),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                LinkColor = Palette.Link,
                ActiveLinkColor = Palette.LinkActive,
                VisitedLinkColor = Palette.Link,
                BackColor = Palette.Card,
                Left = 24,
                Top = y,
                Width = 510,
                Height = 24
            };
            link.LinkClicked += (_, _) => OpenPath(filePath);
            card.Controls.Add(link);
            y += 30;
        }

        if (!string.IsNullOrWhiteSpace(metadataPath))
        {
            var metaLink = new LinkLabel
            {
                Text = "Open matching metadata text file",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                LinkColor = Palette.Link,
                ActiveLinkColor = Palette.LinkActive,
                VisitedLinkColor = Palette.Link,
                BackColor = Palette.Card,
                Left = 24,
                Top = y,
                Width = 510,
                Height = 24
            };
            metaLink.LinkClicked += (_, _) => OpenPath(metadataPath);
            card.Controls.Add(metaLink);
            y += 30;
        }

        var aiPreview = BuildAiPreview(aiAnalysis);

        var aiLabel = new Label
        {
            Text = aiPreview,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Palette.MutedText,
            BackColor = Palette.CardAlt,
            Left = 24,
            Top = y + 4,
            Width = 510,
            Height = 76,
            Padding = new Padding(10),
        };
        card.Controls.Add(aiLabel);

        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            var folderButton = new SleekButton
            {
                Text = "Open Save Folder",
                Left = 24,
                Top = 250,
                Width = 170,
                Height = 38,
                Radius = 14,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                NormalColor = Palette.Secondary,
                HoverColor = Palette.SecondaryHover,
                PressedColor = Palette.SecondaryPressed,
                TextColor = Palette.Text
            };
            folderButton.Click += (_, _) => OpenPath(folderPath);
            card.Controls.Add(folderButton);
        }

        var okButton = new SleekButton
        {
            Text = "Done",
            Left = 474,
            Top = 328,
            Width = 110,
            Height = 38,
            Radius = 14,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            NormalColor = Palette.Accent,
            HoverColor = Palette.AccentHover,
            PressedColor = Palette.AccentPressed,
            TextColor = Color.White
        };
        okButton.Click += (_, _) => Close();

        Controls.Add(card);
        Controls.Add(okButton);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var brush = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(20, 24, 36),
            Color.FromArgb(9, 12, 20),
            LinearGradientMode.ForwardDiagonal
        );

        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    public static void ShowSaved(Form owner, string filePath, string metadataPath, string folderPath, string aiAnalysis)
    {
        using var dialog = new SleekDialog(
            "Wallpaper saved",
            "Your wallpaper and metadata were saved locally. Click the filename below to open it.",
            filePath,
            metadataPath,
            folderPath,
            aiAnalysis
        );

        dialog.ShowDialog(owner);
    }

    public static void ShowInfo(Form owner, string title, string message)
    {
        using var dialog = new SleekDialog(title, message, null, null, null, null);
        dialog.ShowDialog(owner);
    }

    private static string BuildAiPreview(string? aiAnalysis)
    {
        if (string.IsNullOrWhiteSpace(aiAnalysis))
        {
            return "Local AI: No analysis was added.";
        }

        var firstLine = aiAnalysis
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => !x.StartsWith("Local AI analysis generated", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            firstLine = "Local AI analysis was added to the metadata file.";
        }

        firstLine = firstLine.Trim();

        if (firstLine.Length > 185)
        {
            firstLine = firstLine[..185] + "...";
        }

        return "AI preview: " + firstLine;
    }

    private static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}

public sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 18;

    public RoundedPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = DrawingTools.RoundedRectangle(ClientRectangle, Radius);
        using var brush = new SolidBrush(BackColor);

        e.Graphics.FillPath(brush, path);
    }
}

public sealed class SleekButton : Button
{
    private bool hovering;
    private bool pressing;

    public int Radius { get; set; } = 16;
    public Color NormalColor { get; set; } = Palette.Accent;
    public Color HoverColor { get; set; } = Palette.AccentHover;
    public Color PressedColor { get; set; } = Palette.AccentPressed;
    public Color TextColor { get; set; } = Color.White;

    public SleekButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovering = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovering = false;
        pressing = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        pressing = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        pressing = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var color = pressing ? PressedColor : hovering ? HoverColor : NormalColor;

        using var path = DrawingTools.RoundedRectangle(ClientRectangle, Radius);
        using var brush = new SolidBrush(color);

        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            TextColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.WordBreak |
            TextFormatFlags.EndEllipsis
        );
    }
}

public static class DrawingTools
{
    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}

public static class Palette
{
    public static readonly Color Background = Color.FromArgb(9, 12, 20);
    public static readonly Color Card = Color.FromArgb(22, 27, 40);
    public static readonly Color CardAlt = Color.FromArgb(32, 39, 55);
    public static readonly Color Text = Color.FromArgb(245, 247, 250);
    public static readonly Color MutedText = Color.FromArgb(160, 170, 190);
    public static readonly Color Accent = Color.FromArgb(78, 119, 255);
    public static readonly Color AccentHover = Color.FromArgb(99, 139, 255);
    public static readonly Color AccentPressed = Color.FromArgb(52, 91, 220);
    public static readonly Color Secondary = Color.FromArgb(38, 47, 66);
    public static readonly Color SecondaryHover = Color.FromArgb(48, 58, 80);
    public static readonly Color SecondaryPressed = Color.FromArgb(30, 37, 52);
    public static readonly Color Link = Color.FromArgb(125, 178, 255);
    public static readonly Color LinkActive = Color.FromArgb(170, 205, 255);
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
    public static string BuildMetadata(string savedPath, string originalPath, string source, string aiAnalysis)
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
        sb.AppendLine("Location from local metadata:");
        sb.AppendLine("Location: Unknown unless EXIF or local Windows metadata exposes it.");
        sb.AppendLine();

        sb.AppendLine("Local AI visual analysis:");
        sb.AppendLine("-------------------------");
        sb.AppendLine(aiAnalysis);
        sb.AppendLine();
        sb.AppendLine("AI accuracy note:");
        sb.AppendLine("The AI analysis is a best-effort visual guess from a local model. Treat location guesses as uncertain unless confirmed by external evidence.");
        sb.AppendLine();

        sb.AppendLine("Privacy note:");
        sb.AppendLine("This file was created locally by SpotlightSaver. No image or metadata was uploaded by this app.");

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
