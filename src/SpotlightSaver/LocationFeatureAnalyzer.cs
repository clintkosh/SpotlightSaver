using System.Drawing;
using System.Text;

namespace SpotlightSaver;

public static class LocationFeatureAnalyzer
{
    public static string AnalyzeImage(string imagePath)
    {
        try
        {
            using var original = Image.FromFile(imagePath);
            using var bitmap = new Bitmap(original, new Size(224, 126));

            var stats = ImageStats.FromBitmap(bitmap);
            var features = DetectFeatures(stats);
            var candidates = RankLocationCandidates(stats, features);

            var sb = new StringBuilder();

            sb.AppendLine("Location Candidate Report");
            sb.AppendLine("=========================");
            sb.AppendLine();
            sb.AppendLine("Goal:");
            sb.AppendLine("Identify possible locations from visible features only.");
            sb.AppendLine();

            sb.AppendLine("Detected location-relevant features:");
            if (features.Count == 0)
            {
                sb.AppendLine("- no strong location-relevant feature detected");
            }
            else
            {
                foreach (var feature in features.Distinct().Take(12))
                {
                    sb.AppendLine("- " + feature);
                }
            }

            sb.AppendLine();
            sb.AppendLine("Possible location candidates:");

            if (candidates.Count == 0)
            {
                sb.AppendLine("1. Unknown");
                sb.AppendLine("   Confidence: Low");
                sb.AppendLine("   Reason: The image does not contain enough distinctive visible location features.");
            }
            else
            {
                var index = 1;

                foreach (var candidate in candidates.Take(8))
                {
                    sb.AppendLine($"{index}. {candidate.Name}");
                    sb.AppendLine($"   Confidence: {candidate.Confidence}");
                    sb.AppendLine($"   Reason: {candidate.Reason}");
                    index++;
                }
            }

            sb.AppendLine();
            sb.AppendLine("Exact location status:");
            sb.AppendLine("Not confirmed from pixels alone. These are feature-based candidates unless confirmed by Microsoft metadata, EXIF GPS, visible text, or external lookup.");
            sb.AppendLine();
            sb.AppendLine("Suggested manual search terms:");
            sb.AppendLine("- " + BuildSearchTerms(features, candidates));
            sb.AppendLine();
            sb.AppendLine("Privacy note:");
            sb.AppendLine("This analysis was generated locally inside SpotlightSaver. No image was uploaded.");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"""
Location Candidate Report
=========================

Analysis failed.

Reason:
{ex.Message}

The wallpaper was still saved. Only the feature-based location analysis failed.
""";
        }
    }

    private static List<string> DetectFeatures(ImageStats s)
    {
        var features = new List<string>();

        if (s.HasWaterfall)
        {
            features.Add("waterfalls / cascades");
        }

        if (s.HasTerracedWater)
        {
            features.Add("terraced waterfall ledges or stepped pools");
        }

        if (s.TurquoiseWater > 0.10)
        {
            features.Add("turquoise or mineral-rich water");
        }

        if (s.WhiteFoam > 0.07)
        {
            features.Add("white-water foam or falling water");
        }

        if (s.Forest > 0.10)
        {
            features.Add("forest or dense vegetation");
        }

        if (s.RedPinkFoliage > 0.08)
        {
            features.Add("red/pink foliage or heavy color-graded leaves");
        }

        if (s.SnowIce > 0.16)
        {
            features.Add("snow, ice, glacier, or bright alpine terrain");
        }

        if (s.MountainRock > 0.14 && s.Sky > 0.08)
        {
            features.Add("mountains, cliffs, canyon walls, or rocky highlands");
        }

        if (s.SandDesert > 0.12 && s.Forest < 0.08)
        {
            features.Add("sand, desert, dunes, canyon, or arid terrain");
        }

        if (s.OceanBeach)
        {
            features.Add("beach, coast, island, reef, or shoreline");
        }

        if (s.UrbanStructure)
        {
            features.Add("city, dense architecture, skyline, or human-built structures");
        }

        if (s.MonumentLike)
        {
            features.Add("statue, monument, tower, temple, column, arch, or landmark-like structure");
        }

        if (s.BridgeLike)
        {
            features.Add("bridge-like horizontal structure");
        }

        if (s.CastleTempleLike)
        {
            features.Add("castle, cathedral, temple, ruin, or historic stone structure");
        }

        if (s.NightCityLike)
        {
            features.Add("night city, lights, skyline, or illuminated urban scene");
        }

        if (s.AuroraLike)
        {
            features.Add("aurora-like green/purple sky bands");
        }

        return features;
    }

    private static List<LocationCandidate> RankLocationCandidates(ImageStats s, List<string> features)
    {
        var results = new List<LocationCandidate>();

        AddWaterfallCandidates(results, s);
        AddMountainCandidates(results, s);
        AddDesertCandidates(results, s);
        AddBeachCandidates(results, s);
        AddUrbanLandmarkCandidates(results, s);
        AddSnowAuroraCandidates(results, s);
        AddHistoricStructureCandidates(results, s);

        return results
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static void AddWaterfallCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (!s.HasWaterfall && !s.HasTerracedWater)
        {
            return;
        }

        if (s.HasTerracedWater && s.TurquoiseWater > 0.12 && s.Forest > 0.08)
        {
            results.Add(new LocationCandidate(
                "Kuang Si / Tat Kuang Si Falls, Luang Prabang, Laos",
                "Medium",
                96,
                "terraced turquoise pools, white cascades, limestone-like ledges, and forest setting"
            ));

            results.Add(new LocationCandidate(
                "Erawan Falls, Kanchanaburi, Thailand",
                "Low-Medium",
                82,
                "multi-tier turquoise waterfall pools in a forested limestone setting"
            ));

            results.Add(new LocationCandidate(
                "Plitvice Lakes National Park, Croatia",
                "Low",
                64,
                "terraced water and cascading pools, though foliage/color may not match as strongly"
            ));
        }
        else if (s.HasWaterfall)
        {
            results.Add(new LocationCandidate(
                "Iguazú Falls, Argentina/Brazil",
                "Low",
                48,
                "large waterfall/cascade features, but no unique confirming landmark visible"
            ));

            results.Add(new LocationCandidate(
                "Yosemite waterfall area, California, USA",
                "Low",
                42,
                "waterfall and rock/cliff signals, but exact place is not confirmed"
            ));

            results.Add(new LocationCandidate(
                "Generic forest waterfall or cascade location",
                "Medium",
                40,
                "waterfall/cascade detected, but not enough unique location evidence"
            ));
        }
    }

    private static void AddMountainCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (!(s.MountainRock > 0.14 && s.Sky > 0.08) && s.SnowIce < 0.16)
        {
            return;
        }

        if (s.SnowIce > 0.20 && s.TurquoiseWater > 0.05)
        {
            results.Add(new LocationCandidate(
                "Canadian Rockies, Alberta/British Columbia, Canada",
                "Low-Medium",
                72,
                "snow or glacier signals with turquoise water and mountain terrain"
            ));

            results.Add(new LocationCandidate(
                "Swiss Alps",
                "Low-Medium",
                66,
                "snowy alpine terrain and mountain features"
            ));

            results.Add(new LocationCandidate(
                "Patagonia, Argentina/Chile",
                "Low",
                58,
                "snowy mountain and glacial landscape clues"
            ));
        }
        else
        {
            results.Add(new LocationCandidate(
                "Dolomites, Italy",
                "Low",
                54,
                "rocky mountain/cliff features; exact peak shape not confirmed"
            ));

            results.Add(new LocationCandidate(
                "Swiss Alps",
                "Low",
                50,
                "mountain and sky signals without a unique landmark"
            ));

            results.Add(new LocationCandidate(
                "Rocky Mountains, North America",
                "Low",
                46,
                "broad mountain terrain clues"
            ));
        }
    }

    private static void AddDesertCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (s.SandDesert < 0.12 || s.Forest > 0.08)
        {
            return;
        }

        if (s.MountainRock > 0.12)
        {
            results.Add(new LocationCandidate(
                "Antelope Canyon / Arizona canyon region, USA",
                "Low-Medium",
                68,
                "warm sandstone/canyon-like tones and low vegetation"
            ));

            results.Add(new LocationCandidate(
                "Wadi Rum, Jordan",
                "Low",
                56,
                "desert rock and arid terrain clues"
            ));

            results.Add(new LocationCandidate(
                "Sahara or North African desert region",
                "Low",
                48,
                "sand/desert color profile with little vegetation"
            ));
        }
        else
        {
            results.Add(new LocationCandidate(
                "Sahara Desert / dune region",
                "Low",
                50,
                "sand/dune-like color profile with limited vegetation"
            ));

            results.Add(new LocationCandidate(
                "Namib Desert, Namibia",
                "Low",
                46,
                "warm sand and arid terrain clues"
            ));
        }
    }

    private static void AddBeachCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (!s.OceanBeach)
        {
            return;
        }

        if (s.TurquoiseWater > 0.12 && s.SandDesert > 0.05)
        {
            results.Add(new LocationCandidate(
                "Maldives or Indian Ocean island region",
                "Low-Medium",
                66,
                "turquoise water, bright sand, and tropical shoreline clues"
            ));

            results.Add(new LocationCandidate(
                "Caribbean island beach",
                "Low",
                58,
                "turquoise coastal water and beach-like tones"
            ));

            results.Add(new LocationCandidate(
                "Seychelles",
                "Low",
                52,
                "tropical water and beach clues, not uniquely confirmed"
            ));
        }
        else
        {
            results.Add(new LocationCandidate(
                "Generic coastline or beach location",
                "Medium",
                42,
                "coastal water/shoreline features detected"
            ));
        }
    }

    private static void AddUrbanLandmarkCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (!s.UrbanStructure && !s.MonumentLike && !s.BridgeLike)
        {
            return;
        }

        if (s.BridgeLike)
        {
            results.Add(new LocationCandidate(
                "Golden Gate Bridge, San Francisco, USA",
                "Low",
                46,
                "bridge-like structure detected, but color/shape confirmation is limited"
            ));

            results.Add(new LocationCandidate(
                "Tower Bridge, London, United Kingdom",
                "Low",
                42,
                "bridge-like structure and urban clues, not uniquely confirmed"
            ));
        }

        if (s.MonumentLike)
        {
            results.Add(new LocationCandidate(
                "Statue of Liberty, New York, USA",
                "Low",
                44,
                "statue/monument-like vertical form detected, but no exact statue recognition"
            ));

            results.Add(new LocationCandidate(
                "Christ the Redeemer, Rio de Janeiro, Brazil",
                "Low",
                42,
                "large monument/statue-like form detected, but pose/shape not confirmed"
            ));

            results.Add(new LocationCandidate(
                "Moai statues, Rapa Nui / Easter Island, Chile",
                "Low",
                38,
                "statue/monument-like object detected, but specific shape not confirmed"
            ));

            results.Add(new LocationCandidate(
                "Famous monument, statue, tower, temple, or city landmark",
                "Low-Medium",
                36,
                "landmark-like structure detected without enough exact shape recognition"
            ));
        }

        if (s.UrbanStructure)
        {
            results.Add(new LocationCandidate(
                "New York City skyline, USA",
                "Low",
                40,
                "dense urban/skyline-like structure detected"
            ));

            results.Add(new LocationCandidate(
                "Dubai skyline, United Arab Emirates",
                "Low",
                38,
                "tall modern urban structure clues"
            ));

            results.Add(new LocationCandidate(
                "European historic city center",
                "Low",
                34,
                "dense architecture signals without precise landmark recognition"
            ));
        }
    }

    private static void AddSnowAuroraCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (s.AuroraLike)
        {
            results.Add(new LocationCandidate(
                "Iceland aurora region",
                "Low-Medium",
                68,
                "aurora-like green/purple sky bands and cold landscape clues"
            ));

            results.Add(new LocationCandidate(
                "Northern Norway / Tromsø region",
                "Low",
                58,
                "aurora-like sky clues"
            ));

            results.Add(new LocationCandidate(
                "Lapland, Finland/Sweden",
                "Low",
                54,
                "aurora and cold-region visual clues"
            ));
        }

        if (s.SnowIce > 0.22 && s.Sky > 0.08)
        {
            results.Add(new LocationCandidate(
                "Iceland glacier or ice landscape",
                "Low",
                48,
                "snow/ice and open sky clues"
            ));

            results.Add(new LocationCandidate(
                "Alaska or Arctic region",
                "Low",
                46,
                "snow/ice landscape clues"
            ));
        }
    }

    private static void AddHistoricStructureCandidates(List<LocationCandidate> results, ImageStats s)
    {
        if (!s.CastleTempleLike)
        {
            return;
        }

        results.Add(new LocationCandidate(
            "European castle, cathedral, or old town",
            "Low-Medium",
            52,
            "historic stone/architectural feature pattern detected"
        ));

        results.Add(new LocationCandidate(
            "Angkor-style temple complex, Cambodia",
            "Low",
            44,
            "temple/ruin-like structure and vegetation clues"
        ));

        results.Add(new LocationCandidate(
            "Mayan or ancient stone ruins, Central America",
            "Low",
            38,
            "ruin-like stone structure pattern detected"
        ));
    }

    private static string BuildSearchTerms(List<string> features, List<LocationCandidate> candidates)
    {
        if (candidates.Count > 0)
        {
            var top = candidates[0].Name;
            var topFeatures = string.Join(" ", features.Take(4));
            return $"{top} {topFeatures} Windows Spotlight wallpaper".Trim();
        }

        if (features.Count > 0)
        {
            return string.Join(" ", features.Take(5)) + " Windows Spotlight wallpaper";
        }

        return "Windows Spotlight wallpaper location";
    }

    private sealed record LocationCandidate(string Name, string Confidence, int Score, string Reason);

    private sealed class ImageStats
    {
        public double TurquoiseWater { get; init; }
        public double WhiteFoam { get; init; }
        public double Forest { get; init; }
        public double RedPinkFoliage { get; init; }
        public double Rock { get; init; }
        public double MountainRock { get; init; }
        public double SnowIce { get; init; }
        public double SandDesert { get; init; }
        public double Sky { get; init; }
        public double Dark { get; init; }
        public double SaturatedArtificial { get; init; }
        public double EdgeDensity { get; init; }
        public double VerticalWhiteFlow { get; init; }
        public double TerraceSignal { get; init; }
        public double BrightWaterTopContrast { get; init; }

        public bool HasWaterfall =>
            WhiteFoam > 0.065 &&
            VerticalWhiteFlow > 0.012 &&
            (TurquoiseWater > 0.06 || Rock > 0.08 || Forest > 0.08);

        public bool HasTerracedWater =>
            TurquoiseWater > 0.08 &&
            WhiteFoam > 0.045 &&
            TerraceSignal > 0.08 &&
            Rock > 0.06;

        public bool OceanBeach =>
            TurquoiseWater > 0.10 &&
            SandDesert > 0.045 &&
            Sky > 0.08 &&
            TerraceSignal < 0.10;

        public bool UrbanStructure =>
            EdgeDensity > 0.20 &&
            SaturatedArtificial > 0.10 &&
            Forest < 0.16 &&
            SandDesert < 0.20;

        public bool MonumentLike =>
            EdgeDensity > 0.16 &&
            Rock > 0.08 &&
            Sky > 0.04 &&
            VerticalWhiteFlow < 0.02 &&
            WhiteFoam < 0.08;

        public bool BridgeLike =>
            EdgeDensity > 0.18 &&
            TerraceSignal > 0.10 &&
            Sky > 0.06 &&
            TurquoiseWater < 0.12 &&
            WhiteFoam < 0.08;

        public bool CastleTempleLike =>
            EdgeDensity > 0.18 &&
            Rock > 0.12 &&
            SaturatedArtificial < 0.16 &&
            WhiteFoam < 0.08 &&
            TurquoiseWater < 0.10;

        public bool NightCityLike =>
            Dark > 0.32 &&
            SaturatedArtificial > 0.08 &&
            EdgeDensity > 0.12;

        public bool AuroraLike { get; init; }

        public static ImageStats FromBitmap(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int total = width * height;

            int turquoise = 0;
            int foam = 0;
            int forest = 0;
            int redPink = 0;
            int rock = 0;
            int mountainRock = 0;
            int snowIce = 0;
            int sand = 0;
            int sky = 0;
            int dark = 0;
            int artificial = 0;
            int edges = 0;
            int verticalFlow = 0;
            int terraceRows = 0;
            int auroraPixels = 0;

            for (int y = 1; y < height - 1; y++)
            {
                int rowEdges = 0;

                for (int x = 1; x < width - 1; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    var left = bitmap.GetPixel(x - 1, y);
                    var right = bitmap.GetPixel(x + 1, y);
                    var up = bitmap.GetPixel(x, y - 1);
                    var down = bitmap.GetPixel(x, y + 1);

                    double r = c.R / 255.0;
                    double g = c.G / 255.0;
                    double b = c.B / 255.0;

                    double max = Math.Max(r, Math.Max(g, b));
                    double min = Math.Min(r, Math.Min(g, b));
                    double brightness = (r + g + b) / 3.0;
                    double saturation = max == 0 ? 0 : (max - min) / max;

                    bool isTurquoise =
                        g > 0.36 &&
                        b > 0.36 &&
                        g > r * 1.12 &&
                        b > r * 1.08 &&
                        brightness > 0.30 &&
                        brightness < 0.86;

                    bool isFoam =
                        brightness > 0.72 &&
                        saturation < 0.34;

                    bool isForest =
                        g > r * 1.10 &&
                        g > b * 0.88 &&
                        brightness > 0.13 &&
                        saturation > 0.16;

                    bool isRedPink =
                        r > 0.46 &&
                        r > g * 1.10 &&
                        r > b * 0.82 &&
                        saturation > 0.26;

                    bool isRock =
                        brightness > 0.22 &&
                        brightness < 0.70 &&
                        saturation < 0.34 &&
                        Math.Abs(r - g) < 0.20 &&
                        Math.Abs(g - b) < 0.22;

                    bool isMountainRock =
                        isRock ||
                        (brightness > 0.18 && brightness < 0.62 && saturation < 0.46 && r > 0.20 && g > 0.18);

                    bool isSnowIce =
                        brightness > 0.76 &&
                        saturation < 0.28;

                    bool isSand =
                        r > 0.40 &&
                        g > 0.30 &&
                        b < 0.36 &&
                        r >= g &&
                        saturation > 0.12 &&
                        brightness > 0.34;

                    bool isSky =
                        b > r * 1.12 &&
                        b > g * 1.02 &&
                        brightness > 0.40;

                    bool isDark =
                        brightness < 0.20;

                    bool isArtificial =
                        saturation > 0.52 &&
                        brightness > 0.30;

                    bool isAurora =
                        y < height / 2 &&
                        saturation > 0.32 &&
                        brightness > 0.26 &&
                        (
                            (g > r * 1.20 && g > b * 1.02) ||
                            (b > g * 1.06 && r > g * 0.75)
                        );

                    if (isTurquoise) turquoise++;
                    if (isFoam) foam++;
                    if (isForest) forest++;
                    if (isRedPink) redPink++;
                    if (isRock) rock++;
                    if (isMountainRock) mountainRock++;
                    if (isSnowIce) snowIce++;
                    if (isSand) sand++;
                    if (isSky) sky++;
                    if (isDark) dark++;
                    if (isArtificial) artificial++;
                    if (isAurora) auroraPixels++;

                    int verticalContrast =
                        Math.Abs(up.R - down.R) +
                        Math.Abs(up.G - down.G) +
                        Math.Abs(up.B - down.B);

                    int horizontalContrast =
                        Math.Abs(left.R - right.R) +
                        Math.Abs(left.G - right.G) +
                        Math.Abs(left.B - right.B);

                    int edge = verticalContrast + horizontalContrast;

                    if (edge > 120)
                    {
                        edges++;
                        rowEdges++;
                    }

                    if (isFoam && verticalContrast > 65 && y > height * 0.12 && y < height * 0.94)
                    {
                        verticalFlow++;
                    }
                }

                if (rowEdges > width * 0.20)
                {
                    terraceRows++;
                }
            }

            return new ImageStats
            {
                TurquoiseWater = Pct(turquoise, total),
                WhiteFoam = Pct(foam, total),
                Forest = Pct(forest, total),
                RedPinkFoliage = Pct(redPink, total),
                Rock = Pct(rock, total),
                MountainRock = Pct(mountainRock, total),
                SnowIce = Pct(snowIce, total),
                SandDesert = Pct(sand, total),
                Sky = Pct(sky, total),
                Dark = Pct(dark, total),
                SaturatedArtificial = Pct(artificial, total),
                EdgeDensity = Pct(edges, total),
                VerticalWhiteFlow = Pct(verticalFlow, total),
                TerraceSignal = terraceRows / (double)height,
                BrightWaterTopContrast = 0,
                AuroraLike = Pct(auroraPixels, total) > 0.08 && Pct(dark, total) > 0.12
            };
        }

        private static double Pct(int count, int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            return count / (double)total;
        }
    }
}
