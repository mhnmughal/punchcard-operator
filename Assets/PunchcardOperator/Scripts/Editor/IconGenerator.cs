using System.IO;
using UnityEditor;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Generates the Punchcard Operator app icon procedurally (original art,
    /// nothing imported, no AI image generation): a single glowing punch-card slot
    /// inside a rounded frame, on a dark stage. High contrast, no fine detail, reads
    /// at 20px. Fills the legacy icon, Android Adaptive layers, and the full iOS icon
    /// set (Application, Settings, Notifications, Spotlight, Marketing).
    /// Menu: Punchcard Operator/Regenerate Game Icon.</summary>
    public static class IconGenerator
    {
        private const string IconPath = "Assets/PunchcardOperator/Art/Icon_1024.png";
        private const string AdaptiveBgPath = "Assets/PunchcardOperator/Art/Icon_Adaptive_Background.png";
        private const string AdaptiveFgPath = "Assets/PunchcardOperator/Art/Icon_Adaptive_Foreground.png";
        private const string IosDir = "Assets/PunchcardOperator/Art/iOS";
        private const int Size = 1024;
        private const int SS = 4;
        private const int AdaptiveSize = 432;
        private const float FgSafeZoneScale = 0.78f; // keeps the glyph clear of Android's adaptive mask crop

        private static readonly Vector3 BgCentre = new Vector3(0.09f, 0.10f, 0.14f);
        private static readonly Vector3 BgEdge   = new Vector3(0.02f, 0.03f, 0.05f);
        private static readonly Vector3 Amber    = new Vector3(1.00f, 0.66f, 0.16f);
        private static readonly Vector3 AmberHi  = new Vector3(1.00f, 0.92f, 0.60f);
        private static readonly Vector3 Frame    = new Vector3(0.55f, 0.58f, 0.66f);

        private const float OuterHalf = 0.30f, OuterCorner = 0.09f;
        private const float RingInner = 0.010f, RingOuter = 0.026f;
        private const float SlotHalfW = 0.085f, SlotHalfH = 0.20f, SlotCorner = 0.03f;

        [MenuItem("Punchcard Operator/Regenerate Game Icon")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/PunchcardOperator/Art");

            var acc = new Color[Size * Size];
            for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                Vector3 sum = Vector3.zero;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float u = (px + (sx + 0.5f) / SS) / Size - 0.5f;
                    float v = 0.5f - (py + (sy + 0.5f) / SS) / Size;
                    sum += SampleFlat(u, v);
                }
                sum /= (SS * SS);
                acc[(Size - 1 - py) * Size + px] = new Color(sum.x, sum.y, sum.z, 1f);
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(acc);
            tex.Apply();
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var set = new[] { icon };
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, set, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, set, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.iOS, set, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] Icon regenerated (4x SS) and assigned: " + IconPath);

            GenerateAdaptive();
            GenerateIos();
        }

        /// <summary>Same flat combined art as the legacy icon: opaque, background baked
        /// in, no separate layers. Used directly for iOS, which has no adaptive concept.</summary>
        private static Vector3 SampleFlat(float u, float v)
        {
            float aa = 1.6f / Size;

            float r = Mathf.Sqrt(u * u + v * v) / 0.62f;
            Vector3 col = Vector3.Lerp(BgCentre, BgEdge, Mathf.Clamp01(r * r));

            float bloom = Mathf.Exp(-(u * u + v * v) / 0.05f);
            col += Amber * bloom * 0.25f;

            float outerSd = SdRound(new Vector2(u, v), new Vector2(OuterHalf, OuterHalf), OuterCorner);
            float ring = Mathf.Clamp01(Smooth(outerSd + RingInner, aa) - Smooth(outerSd + RingOuter, aa));
            col = Vector3.Lerp(col, Frame, ring * 0.9f);

            float slotSd = SdRound(new Vector2(u, v), new Vector2(SlotHalfW, SlotHalfH), SlotCorner);
            float slotMask = Smooth(slotSd, aa);
            Vector3 slotCol = Vector3.Lerp(Amber, AmberHi, Mathf.Exp(-(v * v) / 0.05f));
            col = Vector3.Lerp(col, slotCol, slotMask);

            float innerGlow = Mathf.Exp(-(u * u + v * v) / 0.02f) * (1f - slotMask);
            col += AmberHi * innerGlow * 0.5f;

            return new Vector3(Mathf.Clamp01(col.x), Mathf.Clamp01(col.y), Mathf.Clamp01(col.z));
        }

        /// <summary>Android Adaptive icon: same visual language as the legacy icon, split
        /// into an opaque background layer (the dark stage plus ambient bloom) and a
        /// transparent foreground layer (the frame and glowing slot), the foreground
        /// scaled inside Android's mask safe zone so it survives circle/squircle/
        /// rounded-square cropping on any launcher.</summary>
        private static void GenerateAdaptive()
        {
            var bgPixels = new Color[AdaptiveSize * AdaptiveSize];
            var fgPixels = new Color[AdaptiveSize * AdaptiveSize];

            for (int py = 0; py < AdaptiveSize; py++)
            for (int px = 0; px < AdaptiveSize; px++)
            {
                Vector3 bgSum = Vector3.zero;
                Vector3 fgSum = Vector3.zero;
                float fgAlphaSum = 0f;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float u = (px + (sx + 0.5f) / SS) / AdaptiveSize - 0.5f;
                    float v = 0.5f - (py + (sy + 0.5f) / SS) / AdaptiveSize;

                    float r = Mathf.Sqrt(u * u + v * v) / 0.62f;
                    bgSum += Vector3.Lerp(BgCentre, BgEdge, Mathf.Clamp01(r * r));

                    float su = u / FgSafeZoneScale, sv = v / FgSafeZoneScale;
                    float aa = 1.6f / AdaptiveSize * FgSafeZoneScale;

                    float bloom = Mathf.Exp(-(su * su + sv * sv) / 0.05f);
                    float outerSd = SdRound(new Vector2(su, sv), new Vector2(OuterHalf, OuterHalf), OuterCorner);
                    float ring = Mathf.Clamp01(Smooth(outerSd + RingInner, aa) - Smooth(outerSd + RingOuter, aa));

                    float slotSd = SdRound(new Vector2(su, sv), new Vector2(SlotHalfW, SlotHalfH), SlotCorner);
                    float slotMask = Smooth(slotSd, aa);
                    Vector3 slotCol = Vector3.Lerp(Amber, AmberHi, Mathf.Exp(-(sv * sv) / 0.05f));

                    Vector3 fgCol = Vector3.Lerp(Frame * ring, slotCol, slotMask);
                    float innerGlow = Mathf.Exp(-(su * su + sv * sv) / 0.02f) * (1f - slotMask);
                    fgCol += AmberHi * innerGlow * 0.5f;

                    fgSum += fgCol;
                    fgAlphaSum += Mathf.Max(Mathf.Max(ring, slotMask), Mathf.Max(bloom * 0.4f, innerGlow));
                }
                int n = SS * SS;
                bgPixels[(AdaptiveSize - 1 - py) * AdaptiveSize + px] = new Color(bgSum.x / n, bgSum.y / n, bgSum.z / n, 1f);
                Vector3 fgAvg = fgSum / n;
                fgPixels[(AdaptiveSize - 1 - py) * AdaptiveSize + px] =
                    new Color(Mathf.Clamp01(fgAvg.x), Mathf.Clamp01(fgAvg.y), Mathf.Clamp01(fgAvg.z), Mathf.Clamp01(fgAlphaSum / n));
            }

            var bgTex = WritePng(AdaptiveBgPath, AdaptiveSize, bgPixels, false);
            var fgTex = WritePng(AdaptiveFgPath, AdaptiveSize, fgPixels, true);

            var akind = System.Type.GetType("UnityEditor.Android.AndroidPlatformIconKind, UnityEditor.Android.Extensions");
            var adaptiveKind = (PlatformIconKind)akind.GetField("Adaptive").GetValue(null);
            var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, adaptiveKind);
            icons[0].SetTextures(new[] { bgTex, fgTex });
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, adaptiveKind, icons);
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] Android Adaptive icon layers generated and assigned.");
        }

        /// <summary>iOS has no adaptive concept, every slot is a flat pre-rendered square
        /// at a fixed pixel size. Renders the same flat art at every unique size Apple
        /// requires across Application, Settings, Notifications, Spotlight and Marketing,
        /// then fills every slot Unity's iOS Player Settings expose.</summary>
        private static void GenerateIos()
        {
            Directory.CreateDirectory(IosDir);
            var kindType = System.Type.GetType("UnityEditor.iOS.iOSPlatformIconKind, UnityEditor.Apple.Extensions.Common");
            var kindNames = new[] { "Application", "Settings", "Notification", "Spotlight", "Marketing" };
            var cache = new System.Collections.Generic.Dictionary<int, Texture2D>();

            foreach (var kn in kindNames)
            {
                var kind = (PlatformIconKind)kindType.GetField(kn).GetValue(null);
                var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.iOS, kind);
                for (int i = 0; i < icons.Length; i++)
                {
                    int size = icons[i].width; // iOS icons are always square
                    if (!cache.TryGetValue(size, out var tex))
                    {
                        tex = RenderFlatAt(size);
                        var path = IosDir + "/Icon_" + size + ".png";
                        var written = WritePng(path, size, tex, false);
                        Object.DestroyImmediate(tex);
                        tex = written;
                        cache[size] = tex;
                    }
                    icons[i].SetTextures(new[] { tex });
                }
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.iOS, kind, icons);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] iOS icon set generated (" + cache.Count + " unique sizes) and assigned across Application, Settings, Notifications, Spotlight, Marketing.");
        }

        private static Texture2D RenderFlatAt(int size)
        {
            var pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                Vector3 sum = Vector3.zero;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float u = (px + (sx + 0.5f) / SS) / size - 0.5f;
                    float v = 0.5f - (py + (sy + 0.5f) / SS) / size;
                    sum += SampleFlat(u, v);
                }
                sum /= (SS * SS);
                pixels[(size - 1 - py) * size + px] = new Color(sum.x, sum.y, sum.z, 1f);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D WritePng(string path, int size, Color[] pixels, bool alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            var written = WritePng(path, size, tex, alpha);
            Object.DestroyImmediate(tex);
            return written;
        }

        private static Texture2D WritePng(string path, int size, Texture2D source, bool alpha)
        {
            File.WriteAllBytes(path, source.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(size, 32);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static float SdBox(Vector2 p, Vector2 b)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
            return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        private static float SdRound(Vector2 p, Vector2 b, float rad)
            => SdBox(p, new Vector2(Mathf.Max(b.x - rad, 0), Mathf.Max(b.y - rad, 0))) - rad;

        private static float Smooth(float sd, float aa) => Mathf.Clamp01(0.5f - sd / (2f * aa));
    }
}
