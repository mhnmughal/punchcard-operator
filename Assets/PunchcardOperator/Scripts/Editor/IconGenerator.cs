using System.IO;
using UnityEditor;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Generates the Punchcard Operator app icon procedurally (original art,
    /// nothing imported): a manila punch card with the classic cut corner and a row of
    /// punched holes, a chunky amber punch ram driving into the next hole, chads flying,
    /// on a dark vignette with a warm rim light. Rendered 4x-supersampled for clean edges.
    /// Menu: Punchcard Operator/Regenerate Game Icon.</summary>
    public static class IconGenerator
    {
        private const string IconPath = "Assets/PunchcardOperator/Art/Icon_1024.png";
        private const int Size = 1024;
        private const int SS = 4; // supersample factor per axis

        [MenuItem("Punchcard Operator/Regenerate Game Icon")]
        public static void Generate()
        {
            int hi = Size * SS;
            var acc = new Color[Size * Size];

            // palette
            var bgCentre = new Vector3(0.135f, 0.16f, 0.20f);
            var bgEdge = new Vector3(0.045f, 0.052f, 0.065f);
            var glow = new Vector3(0.95f, 0.62f, 0.22f);
            var card = new Vector3(0.80f, 0.70f, 0.50f);
            var cardHi = new Vector3(0.91f, 0.83f, 0.63f);
            var cardLo = new Vector3(0.55f, 0.47f, 0.32f);
            var cardEdge = new Vector3(0.20f, 0.16f, 0.10f);
            var hole = new Vector3(0.045f, 0.05f, 0.06f);
            var amber = new Vector3(0.95f, 0.64f, 0.20f);
            var amberHi = new Vector3(1.0f, 0.83f, 0.46f);

            // card geometry in centred, unrotated space (units = fraction of Size)
            const float ang = -8f * Mathf.Deg2Rad;
            float cs = Mathf.Cos(-ang), sn = Mathf.Sin(-ang);
            const float cw = 0.66f, ch = 0.46f, corner = 0.055f, cut = 0.14f;
            const int holes = 6;
            const float holeW = 0.052f, holeH = 0.085f, rowY = -0.04f;
            const float firstX = -cw * 0.5f + 0.085f;
            const float gap = (cw - 0.17f) / (holes - 1);
            const int ram = 3; // hole the ram is driving into

            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    Vector3 sum = Vector3.zero;
                    for (int sy = 0; sy < SS; sy++)
                    for (int sx = 0; sx < SS; sx++)
                    {
                        float u = (px + (sx + 0.5f) / SS) / Size - 0.5f;
                        float v = 0.5f - (py + (sy + 0.5f) / SS) / Size; // +v up
                        sum += Sample(u, v);
                    }
                    sum /= (SS * SS);
                    // SetPixels expects bottom-to-top row order; our loop runs top-to-bottom
                    acc[(Size - 1 - py) * Size + px] = new Color(sum.x, sum.y, sum.z, 1f);
                }
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
            var tex1 = new[] { icon };
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, tex1, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, tex1, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.iOS, tex1, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] Icon regenerated (4x SS) and assigned: " + IconPath);

            // ---- local sampler -------------------------------------------------

            Vector3 Sample(float u, float v)
            {
                // background: radial vignette + a tight warm glow low behind the card
                float r = Mathf.Sqrt(u * u + v * v) / 0.72f;
                Vector3 col = Vector3.Lerp(bgCentre, bgEdge, Mathf.Clamp01(r));
                float g = Mathf.Exp(-((u * u) + (v + 0.28f) * (v + 0.28f)) / 0.045f);
                col += glow * g * 0.11f;

                // to card-local
                float lx = u * cs - v * sn;
                float ly = u * sn + v * cs;

                // rounded rect signed distance (negative inside)
                float qx = Mathf.Abs(lx) - (cw * 0.5f - corner);
                float qy = Mathf.Abs(ly) - (ch * 0.5f - corner);
                float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
                float sd = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - corner;
                // cut the top-left corner: everything past this diagonal is outside the card
                float cutSd = ((-lx) + ly) - (cw * 0.5f + ch * 0.5f - cut);
                sd = Mathf.Max(sd, cutSd);

                float aa = 1.6f / Size;
                float cardMask = Smooth(sd, aa);

                // tight contact shadow hugging the card edge
                float shadow = Smooth(sd - 0.004f, 0.012f) * 0.5f * (1f - cardMask);
                col = Vector3.Lerp(col, new Vector3(0.018f, 0.022f, 0.028f), shadow);

                if (cardMask > 0.001f)
                {
                    float shade = Mathf.InverseLerp(-ch * 0.5f, ch * 0.5f, ly);
                    Vector3 face = Vector3.Lerp(cardHi, cardLo, Mathf.Clamp01(shade * 1.1f));
                    face = Vector3.Lerp(face, card, 0.35f);

                    // engraved guide line under the hole row
                    if (Mathf.Abs(ly - (rowY - holeH * 0.9f)) < 0.004f) face *= 0.82f;

                    // holes
                    float holeMask = 0f;
                    for (int h = 0; h < holes; h++)
                    {
                        float hx = firstX + h * gap;
                        float hSd = SdBox(new Vector2(lx - hx, ly - rowY), new Vector2(holeW * 0.5f, holeH * 0.5f)) - 0.006f;
                        float m = Smooth(hSd, aa);
                        if (h < ram) holeMask = Mathf.Max(holeMask, m);          // punched clean through
                        else if (h > ram) face = Vector3.Lerp(face, hole, m * 0.28f); // faint unpunched guide
                    }
                    Vector3 cardCol = Vector3.Lerp(face, hole, holeMask);

                    // rim light on the upper-left edge
                    float rim = Smooth(-(sd + 0.004f), aa) * Smooth(sd + 0.014f, aa);
                    cardCol += amberHi * rim * 0.45f * Mathf.Clamp01(0.55f - shade);

                    col = Vector3.Lerp(col, cardCol, cardMask);
                }
                // crisp inset keyline just inside the card edge, for definition
                float key = Mathf.Clamp01(Smooth(sd + 0.0035f, aa) - Smooth(sd + 0.009f, aa));
                col = Vector3.Lerp(col, cardEdge, key * 0.8f);

                // punch ram — amber bar descending from a gantry into hole `ram`
                float ramX = firstX + ram * gap;
                float gantryY = ch * 0.5f + 0.055f;        // just above the card's top edge
                float ramTop = gantryY - 0.02f;
                float ramBot = rowY - 0.02f;               // tip sits just inside the hole
                float ramMid = (ramTop + ramBot) * 0.5f;
                float ramHalf = (ramTop - ramBot) * 0.5f;

                // the ram shaft
                float ramSd = SdBox(new Vector2(lx - ramX, ly - ramMid), new Vector2(0.030f, ramHalf));
                float ramShade = Mathf.InverseLerp(ramBot, ramTop, ly);
                col = Vector3.Lerp(col, Vector3.Lerp(amber, amberHi, ramShade), Smooth(ramSd, aa));
                // bright left edge of the shaft
                float ramEdge = Smooth(SdBox(new Vector2(lx - (ramX - 0.024f), ly - ramMid), new Vector2(0.007f, ramHalf)), aa);
                col = Vector3.Lerp(col, amberHi, ramEdge * 0.7f);
                // press head — a chunky block at the top of the shaft
                float headSd = SdBox(new Vector2(lx - ramX, ly - (ramTop - 0.03f)), new Vector2(0.075f, 0.045f));
                col = Vector3.Lerp(col, Vector3.Lerp(amberHi, amber, 0.35f), Smooth(headSd, aa));
                // dark bite in the struck hole
                float biteSd = SdBox(new Vector2(lx - ramX, ly - rowY), new Vector2(holeW * 0.5f, holeH * 0.5f)) - 0.006f;
                col = Vector3.Lerp(col, hole, Smooth(biteSd, aa));

                // chads flung up and out from the struck hole
                Vector2[] chads = {
                    new Vector2(ramX + 0.085f, rowY + 0.085f), new Vector2(ramX + 0.125f, rowY + 0.15f),
                    new Vector2(ramX - 0.075f, rowY + 0.12f), new Vector2(ramX + 0.02f, rowY + 0.20f),
                };
                for (int c = 0; c < chads.Length; c++)
                {
                    float ch2 = SdBox(new Vector2(lx, ly) - chads[c], new Vector2(0.011f, 0.011f)) - 0.003f;
                    col = Vector3.Lerp(col, cardHi, Smooth(ch2, aa));
                }

                return new Vector3(Mathf.Clamp01(col.x), Mathf.Clamp01(col.y), Mathf.Clamp01(col.z));
            }
        }

        private static float SdBox(Vector2 p, Vector2 b)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
            return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        private static float Smooth(float sd, float aa) => Mathf.Clamp01(0.5f - sd / (2f * aa));
    }
}
