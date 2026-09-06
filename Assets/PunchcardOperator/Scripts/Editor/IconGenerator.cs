using System.IO;
using UnityEditor;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Generates the Punchcard Operator app icon procedurally (original art,
    /// nothing imported): one bold cream punch card tilted on a dark stage, a clean
    /// row of four square holes, the active instruction hole lit amber with a bloom.
    /// High contrast, no fine detail, reads at 48px. Rendered 4x-supersampled.
    /// Menu: Punchcard Operator/Regenerate Game Icon.</summary>
    public static class IconGenerator
    {
        private const string IconPath = "Assets/PunchcardOperator/Art/Icon_1024.png";
        private const int Size = 1024;
        private const int SS = 4;

        [MenuItem("Punchcard Operator/Regenerate Game Icon")]
        public static void Generate()
        {
            var acc = new Color[Size * Size];

            var bgCentre = new Vector3(0.17f, 0.20f, 0.25f);
            var bgEdge   = new Vector3(0.030f, 0.035f, 0.046f);
            var cardHi   = new Vector3(0.98f, 0.93f, 0.78f);
            var cardLo   = new Vector3(0.80f, 0.71f, 0.51f);
            var cardKey  = new Vector3(0.14f, 0.10f, 0.06f);
            var hole     = new Vector3(0.050f, 0.052f, 0.064f);
            var amber    = new Vector3(1.00f, 0.66f, 0.16f);
            var amberHi  = new Vector3(1.00f, 0.88f, 0.52f);

            const float ang = -6f * Mathf.Deg2Rad;
            float cs = Mathf.Cos(-ang), sn = Mathf.Sin(-ang);
            const float cw = 0.66f, chh = 0.44f, corner = 0.045f;
            const int holes = 4;
            const float holeW = 0.072f, holeH = 0.135f, rowY = 0.0f;
            const float firstX = -cw * 0.5f + 0.13f;
            const float gap = (cw - 0.26f) / (holes - 1);
            const int lit = 2; // the active instruction

            float litX = firstX + lit * gap;

            for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                Vector3 sum = Vector3.zero;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float u = (px + (sx + 0.5f) / SS) / Size - 0.5f;
                    float v = 0.5f - (py + (sy + 0.5f) / SS) / Size;
                    sum += Sample(u, v);
                }
                sum /= (SS * SS);
                acc[(Size - 1 - py) * Size + px] = new Color(sum.x, sum.y, sum.z, 1f);
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(acc);
            tex.Apply();

            Directory.CreateDirectory("Assets/PunchcardOperator/Art");
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var set = new[] { icon };
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, set, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, set, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.iOS, set, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] Icon regenerated (4x SS) and assigned: " + IconPath);

            Vector3 Sample(float u, float v)
            {
                float aa = 1.6f / Size;

                // stage
                float r = Mathf.Sqrt(u * u + v * v) / 0.70f;
                Vector3 col = Vector3.Lerp(bgCentre, bgEdge, Mathf.Clamp01(r * r));

                // card-local
                float lx = u * cs - v * sn;
                float ly = u * sn + v * cs;

                // amber bloom from the lit hole, bleeding onto the stage
                float bloom = Mathf.Exp(-(new Vector2(lx - litX, ly - rowY).sqrMagnitude) / 0.020f);
                col += amber * bloom * 0.28f;

                float sd = SdRound(new Vector2(lx, ly), new Vector2(cw * 0.5f, chh * 0.5f), corner);
                float cardMask = Smooth(sd, aa);

                // drop shadow
                float shSd = SdRound(new Vector2(lx - 0.03f, ly + 0.032f), new Vector2(cw * 0.5f, chh * 0.5f), corner);
                col = Vector3.Lerp(col, new Vector3(0.010f, 0.012f, 0.018f), Smooth(shSd, 0.055f) * 0.6f * (1f - cardMask));

                if (cardMask > 0.001f)
                {
                    float shade = Mathf.Clamp01(Mathf.InverseLerp(-chh * 0.5f, chh * 0.55f, ly));
                    Vector3 face = Vector3.Lerp(cardLo, cardHi, 1f - shade * 0.9f);

                    float holeMask = 0f, litMask = 0f;
                    for (int h = 0; h < holes; h++)
                    {
                        float hx = firstX + h * gap;
                        float hSd = SdRound(new Vector2(lx - hx, ly - rowY), new Vector2(holeW * 0.5f, holeH * 0.5f), 0.012f);
                        float m = Smooth(hSd, aa);
                        holeMask = Mathf.Max(holeMask, m);
                        if (h == lit) litMask = m;
                    }

                    Vector3 cardCol = Vector3.Lerp(face, hole, holeMask);
                    // lit hole: amber fill + inner glow
                    Vector3 litCol = Vector3.Lerp(amber, amberHi,
                        Mathf.Exp(-(new Vector2(lx - litX, ly - rowY).sqrMagnitude) / 0.0032f));
                    cardCol = Vector3.Lerp(cardCol, litCol, litMask);

                    // top rim light
                    float rim = Smooth(-(sd + 0.003f), aa) * Smooth(sd + 0.013f, aa);
                    cardCol += amberHi * rim * 0.30f * Mathf.Clamp01(1.3f - shade * 2f);

                    col = Vector3.Lerp(col, cardCol, cardMask);
                }

                // dark keyline
                float key = Mathf.Clamp01(Smooth(sd + 0.003f, aa) - Smooth(sd + 0.009f, aa));
                col = Vector3.Lerp(col, cardKey, key * 0.9f);

                return new Vector3(Mathf.Clamp01(col.x), Mathf.Clamp01(col.y), Mathf.Clamp01(col.z));
            }
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
