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
            Text = "Use feature-based location candidates",
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
            Text = "Detects mountains, waterfalls, statues, landmarks, cities, beaches, deserts, and more.",
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
            Text = "Ready. Location candidates are generated locally from visible features.",
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
            Text = "Location candidates are visual guesses",
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
                statusLabel.Text = "Saved image. Detecting location-relevant features...";
                Refresh();

                aiAnalysis = await Task.Run(() => LocationFeatureAnalyzer.AnalyzeImage(outputImage));
            }
            else
            {
                aiAnalysis = "Onboard image analysis: Skipped by user.";
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

public static class BuiltInImageAnalyzer
{
    public static string AnalyzeImage(string imagePath)
    {
        try
        {
            using var original = Image.FromFile(imagePath);
            using var bitmap = new Bitmap(original, new Size(160, 90));

            var width = bitmap.Width;
            var height = bitmap.Height;
            var total = width * height;

            int skyLike = 0;
            int waterLike = 0;
            int vegetationLike = 0;
            int snowLike = 0;
            int sandLike = 0;
            int rockLike = 0;
            int darkLike = 0;
            int brightLike = 0;
            int warmLike = 0;
            int coolLike = 0;
            int artificialLike = 0;

            double totalBrightness = 0;
            double totalSaturation = 0;
            double topBlue = 0;
            double bottomBlue = 0;
            double topGreen = 0;
            double bottomGreen = 0;

            var edgeScore = 0;
            var horizonScore = 0;

            for (int y = 0; y < height; y++)
            {
                double rowBrightness = 0;

                for (int x = 0; x < width; x++)
                {
                    var c = bitmap.GetPixel(x, y);

                    var r = c.R / 255.0;
                    var g = c.G / 255.0;
                    var b = c.B / 255.0;

                    var max = Math.Max(r, Math.Max(g, b));
                    var min = Math.Min(r, Math.Min(g, b));
                    var brightness = (r + g + b) / 3.0;
                    var saturation = max == 0 ? 0 : (max - min) / max;

                    totalBrightness += brightness;
                    totalSaturation += saturation;
                    rowBrightness += brightness;

                    if (y < height / 3)
                    {
                        topBlue += b;
                        topGreen += g;
                    }

                    if (y > height * 2 / 3)
                    {
                        bottomBlue += b;
                        bottomGreen += g;
                    }

                    if (b > r * 1.15 && b > g * 1.05 && brightness > 0.35)
                    {
                        skyLike++;
                    }

                    if (b > r * 1.1 && g > r * 1.05 && brightness > 0.22 && brightness < 0.75)
                    {
                        waterLike++;
                    }

                    if (g > r * 1.15 && g > b * 1.05 && brightness > 0.18)
                    {
                        vegetationLike++;
                    }

                    if (brightness > 0.78 && saturation < 0.22)
                    {
                        snowLike++;
                    }

                    if (r > 0.42 && g > 0.34 && b < 0.32 && saturation > 0.18)
                    {
                        sandLike++;
                    }

                    if (brightness > 0.22 && brightness < 0.58 && saturation < 0.22)
                    {
                        rockLike++;
                    }

                    if (brightness < 0.18)
                    {
                        darkLike++;
                    }

                    if (brightness > 0.75)
                    {
                        brightLike++;
                    }

                    if (r > b * 1.18 && r > g * 1.02)
                    {
                        warmLike++;
                    }

                    if (b > r * 1.15)
                    {
                        coolLike++;
                    }

                    if (saturation > 0.55 && brightness > 0.35)
                    {
                        artificialLike++;
                    }

                    if (x > 0 && y > 0)
                    {
                        var left = bitmap.GetPixel(x - 1, y);
                        var above = bitmap.GetPixel(x, y - 1);

                        var diffLeft = Math.Abs(c.R - left.R) + Math.Abs(c.G - left.G) + Math.Abs(c.B - left.B);
                        var diffAbove = Math.Abs(c.R - above.R) + Math.Abs(c.G - above.G) + Math.Abs(c.B - above.B);

                        if (diffLeft + diffAbove > 110)
                        {
                            edgeScore++;
                        }
                    }
                }

                if (y > 0 && y < height - 1)
                {
                    double normalizedRow = rowBrightness / width;

                    double above = 0;
                    double below = 0;

                    for (int x = 0; x < width; x++)
                    {
                        var ca = bitmap.GetPixel(x, y - 1);
                        var cb = bitmap.GetPixel(x, y + 1);

                        above += (ca.R + ca.G + ca.B) / 765.0;
                        below += (cb.R + cb.G + cb.B) / 765.0;
                    }

                    above /= width;
                    below /= width;

                    if (Math.Abs(above - below) > 0.18 && normalizedRow > 0.2)
                    {
                        horizonScore++;
                    }
                }
            }

            double pctSky = Percent(skyLike, total);
            double pctWater = Percent(waterLike, total);
            double pctVegetation = Percent(vegetationLike, total);
            double pctSnow = Percent(snowLike, total);
            double pctSand = Percent(sandLike, total);
            double pctRock = Percent(rockLike, total);
            double pctDark = Percent(darkLike, total);
            double pctBright = Percent(brightLike, total);
            double pctWarm = Percent(warmLike, total);
            double pctCool = Percent(coolLike, total);
            double pctArtificial = Percent(artificialLike, total);
            double pctEdges = Percent(edgeScore, total);

            double avgBrightness = totalBrightness / total;
            double avgSaturation = totalSaturation / total;

            var features = new List<string>();
            var possibleScenes = new List<string>();
            var locationClues = new List<string>();

            if (pctSky > 18) features.Add("large sky or open-air area");
            if (pctWater > 12) features.Add("possible water, lake, ocean, river, or reflective blue surface");
            if (pctVegetation > 12) features.Add("visible vegetation or green landscape");
            if (pctSnow > 12) features.Add("snow, ice, clouds, pale stone, or bright low-saturation terrain");
            if (pctSand > 8) features.Add("sand, desert, dry grass, canyon rock, or warm earth tones");
            if (pctRock > 15) features.Add("stone, cliffs, mountains, concrete, or muted terrain");
            if (pctEdges > 18) features.Add("high detail or many hard edges, possibly architecture, city texture, trees, or rocky terrain");
            if (pctArtificial > 18 && pctEdges > 12) features.Add("strong saturated detail, possibly urban lighting, signage, flowers, or edited color");

            if (pctWater > 15 && pctSky > 15)
            {
                possibleScenes.Add("coastal, lake, river, or island landscape");
                locationClues.Add("Water plus open sky can suggest a coast, lake district, river valley, or waterfront city.");
            }

            if (pctVegetation > 18 && pctSky > 12)
            {
                possibleScenes.Add("forest, meadow, park, countryside, or mountain valley");
                locationClues.Add("Heavy green coverage suggests a temperate, tropical, or spring/summer landscape.");
            }

            if (pctSnow > 18 && pctSky > 8)
            {
                possibleScenes.Add("snowy mountain, glacier, winter landscape, or bright cloud scene");
                locationClues.Add("Snow/ice clues can point toward alpine, polar, or winter regions, but this is not enough for a precise location.");
            }

            if (pctSand > 12 && pctVegetation < 10)
            {
                possibleScenes.Add("desert, canyon, beach, dunes, or arid terrain");
                locationClues.Add("Warm earth tones with limited vegetation suggest an arid, desert, canyon, or beach-like environment.");
            }

            if (pctEdges > 22 && pctArtificial > 12)
            {
                possibleScenes.Add("city, architecture, street scene, interior, or highly structured landscape");
                locationClues.Add("Dense edges and saturated regions may indicate buildings, windows, signs, lights, or human-made structures.");
            }

            if (pctRock > 20 && pctSky > 10)
            {
                possibleScenes.Add("mountain, cliff, canyon, rocky coastline, or stone architecture");
                locationClues.Add("Rock-like muted tones with sky can suggest cliffs, mountains, canyons, or rocky shores.");
            }

            if (pctDark > 30)
            {
                possibleScenes.Add("night scene, shadow-heavy landscape, cave, dark forest, or low-light image");
                locationClues.Add("Darkness limits location confidence because important visual clues may be hidden.");
            }

            if (possibleScenes.Count == 0)
            {
                possibleScenes.Add("general landscape, abstract wallpaper, close-up scene, or image without strong category signals");
            }

            if (features.Count == 0)
            {
                features.Add("no dominant simple visual feature detected");
            }

            var colorMood = BuildColorMood(avgBrightness, avgSaturation, pctWarm, pctCool);
            var confidence = BuildConfidence(pctSky, pctWater, pctVegetation, pctSnow, pctSand, pctRock, pctEdges);

            var sb = new StringBuilder();

            sb.AppendLine("Onboard image analysis generated by SpotlightSaver.");
            sb.AppendLine();
            sb.AppendLine("Important:");
            sb.AppendLine("This is a lightweight built-in visual estimate. It does not use Microsoft location metadata, cloud AI, Ollama, or an external model.");
            sb.AppendLine();
            sb.AppendLine("Scene estimate:");
            sb.AppendLine("- " + string.Join("; ", possibleScenes.Distinct().Take(4)) + ".");
            sb.AppendLine();
            sb.AppendLine("Detected visual clues:");
            foreach (var feature in features.Distinct().Take(8))
            {
                sb.AppendLine("- " + feature);
            }

            sb.AppendLine();
            sb.AppendLine("Color and image character:");
            sb.AppendLine("- " + colorMood);
            sb.AppendLine($"- Average brightness: {avgBrightness:P0}");
            sb.AppendLine($"- Average saturation: {avgSaturation:P0}");
            sb.AppendLine($"- Approximate edge/detail density: {pctEdges:P0}");

            sb.AppendLine();
            sb.AppendLine("Possible location clues:");
            foreach (var clue in locationClues.Distinct().Take(5))
            {
                sb.AppendLine("- " + clue);
            }

            if (locationClues.Count == 0)
            {
                sb.AppendLine("- No reliable location-specific clue was detected from the image alone.");
            }

            sb.AppendLine();
            sb.AppendLine("Location confidence:");
            sb.AppendLine("- " + confidence);
            sb.AppendLine();
            sb.AppendLine("Suggested manual search terms:");
            sb.AppendLine("- " + BuildSearchTerms(possibleScenes, features));

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"""
Onboard image analysis: Failed.

Reason:
{ex.Message}

The wallpaper was still saved. Only the built-in visual analysis failed.
""";
        }
    }

    private static double Percent(int value, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return value / (double)total;
    }

    private static string BuildColorMood(double brightness, double saturation, double warm, double cool)
    {
        var mood = new List<string>();

        if (brightness > 0.68) mood.Add("bright");
        else if (brightness < 0.28) mood.Add("dark or shadow-heavy");
        else mood.Add("balanced brightness");

        if (saturation > 0.42) mood.Add("colorful");
        else if (saturation < 0.20) mood.Add("muted or low-saturation");
        else mood.Add("moderate color");

        if (warm > cool * 1.2) mood.Add("warm-toned");
        else if (cool > warm * 1.2) mood.Add("cool-toned");
        else mood.Add("mixed warm/cool tones");

        return string.Join(", ", mood) + ".";
    }

    private static string BuildConfidence(double sky, double water, double vegetation, double snow, double sand, double rock, double edges)
    {
        var strongest = new[] { sky, water, vegetation, snow, sand, rock, edges }.Max();

        if (strongest > 0.30)
        {
            return "Medium for broad scene type, low for exact location. The image has strong visual category signals but not enough for a precise place name.";
        }

        if (strongest > 0.16)
        {
            return "Low to medium for broad scene type, low for exact location. Some visual clues exist, but they are not unique.";
        }

        return "Low. The image does not contain enough distinctive visual information for location inference.";
    }

    private static string BuildSearchTerms(List<string> scenes, List<string> features)
    {
        var terms = new List<string>();

        foreach (var scene in scenes.Take(2))
        {
            terms.Add(scene.Split(',')[0].Trim());
        }

        foreach (var feature in features.Take(3))
        {
            var cleaned = feature
                .Replace("possible ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("visible ", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            terms.Add(cleaned);
        }

        terms.Add("Windows Spotlight wallpaper");

        return string.Join(" ", terms.Distinct()).Trim();
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
            return "Location analysis: No candidate report was added.";
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

        return "Location preview: " + firstLine;
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

        sb.AppendLine("Location candidates by visible features:");
        sb.AppendLine("-------------------------");
        sb.AppendLine(aiAnalysis);
        sb.AppendLine();
        sb.AppendLine("AI accuracy note:");
        sb.AppendLine("The onboard analysis is a lightweight best-effort visual guess based on image features. Treat location guesses as uncertain unless confirmed by external evidence.");
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


