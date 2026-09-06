using System.IO;
using UnityEditor;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Generates the game's UI sprite skin procedurally (rounded 9-slice
    /// panels + buttons with a baked bevel, a radial glow, a vignette, rating pips,
    /// a padlock). All original, nothing imported.
    /// Menu: Punchcard Operator/Regenerate UI Skin.</summary>
    public static class UISkinGenerator
    {
        private const string Dir = "Assets/PunchcardOperator/Resources/UI";

        [MenuItem("Punchcard Operator/Regenerate UI Skin")]
        public static void Generate()
        {
            Directory.CreateDirectory(Dir);

            Panel("panel", 64, 20, outline: 2, bevel: 0f);
            Panel("button", 64, 18, outline: 2, bevel: 1f);
            Panel("inset", 64, 14, outline: 2, bevel: -1f);
            Radial("glow", 128, 0f);
            Radial("vignette", 256, 1f);
            Pip("pip_full", 40, true);
            Pip("pip_empty", 40, false);
            Padlock("lock", 48);
            Gear("gear", 128, 10);

            AssetDatabase.Refresh();
            SetSprite("panel", 20); SetSprite("button", 18); SetSprite("inset", 14);
            SetSprite("glow", 0); SetSprite("vignette", 0);
            SetSprite("pip_full", 0); SetSprite("pip_empty", 0); SetSprite("lock", 0); SetSprite("gear", 0);
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] UI skin regenerated in " + Dir);
        }

        // white rounded rect with a baked dark keyline + optional bevel.
        // bevel > 0 = raised (light top / dark bottom); bevel < 0 = recessed.
        private static void Panel(string name, int size, float radius, float outline, float bevel)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;   // <0 inside
                float inside = Mathf.Clamp01(0.5f - d / 1.3f);

                float lum = 1f;
                float t = (float)y / (size - 1);                    // 0 bottom .. 1 top

                if (Mathf.Abs(bevel) > 0.01f)
                {
                    lum += (bevel > 0 ? 0.11f : -0.09f) * (t - 0.5f) * 2f;    // face gradient
                    // soft highlight/shadow only within ~4px of the top/bottom inner edge
                    float ring = d < 0 ? Mathf.Clamp01(1f - Mathf.Abs(d) / 4.5f) : 0f;
                    float dir = bevel > 0 ? (t - 0.5f) : (0.5f - t);           // +top / -bottom
                    lum += Mathf.Clamp(dir * 3f, -1f, 1f) * ring * 0.35f;
                }

                // dark keyline hugging the edge
                float key = Mathf.Clamp01(1f - Mathf.Abs(d + outline * 0.5f) / (outline * 0.5f + 0.7f));
                lum *= 1f - key * 0.55f;

                lum = Mathf.Clamp01(lum);
                px[y * size + x] = new Color(lum, lum, lum, inside);
            }
            Write(name, size, px);
        }

        private static void Radial(string name, int size, float invert)
        {
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a);          // smoothstep
                if (invert > 0.5f) a = Mathf.Clamp01((r - 0.55f) / 0.45f);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            Write(name, size, px);
        }

        private static void Pip(string name, int size, bool filled)
        {
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a;
                if (filled) a = Mathf.Clamp01((0.82f - r) / 0.12f);
                else a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.72f) / 0.14f);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            Write(name, size, px);
        }

        private static void Padlock(string name, int size)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / (size - 1), v = (float)y / (size - 1);
                float a = 0f;
                // body
                if (u > 0.2f && u < 0.8f && v > 0.12f && v < 0.62f) a = 1f;
                // shackle (ring)
                float sd = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.62f) * (v - 0.62f));
                if (sd > 0.14f && sd < 0.24f && v > 0.6f) a = 1f;
                px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
            }
            Write(name, size, px);
        }

        private static void Gear(string name, int size, int teeth)
        {
            var px = new Color32[size * size];
            float cx = (size - 1) * 0.5f;
            const float rBody = 0.60f, rTeeth = 0.80f, hole = 0.26f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / cx, dy = (y - cx) / cx;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float toothT = Mathf.SmoothStep(0f, 1f, Mathf.Cos(ang * teeth) * 4f + 0.5f); // sharp square teeth
                float outer = Mathf.Lerp(rBody, rTeeth, toothT);
                float a = Mathf.Clamp01((r - hole) / 0.03f) * Mathf.Clamp01((outer - r) / 0.03f);
                // subtle inner ring detail + rim shade
                float lum = 1f;
                lum -= Mathf.Clamp01(1f - Mathf.Abs(r - (hole + 0.10f)) / 0.02f) * 0.4f;
                lum -= Mathf.Clamp01((r - rBody) / 0.2f) * 0.25f;
                px[y * size + x] = new Color(Mathf.Clamp01(lum), Mathf.Clamp01(lum), Mathf.Clamp01(lum), a);
            }
            Write(name, size, px);
        }

        private static void Write(string name, int size, Color32[] px)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes($"{Dir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void SetSprite(string name, int border)
        {
            var path = $"{Dir}/{name}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            if (border > 0)
            {
                var settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteBorder = new Vector4(border, border, border, border);
                imp.SetTextureSettings(settings);
            }
            imp.SaveAndReimport();
        }
    }
}
