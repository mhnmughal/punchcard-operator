using System.IO;
using UnityEditor;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Generates the Punchcard Operator app icon procedurally (original art,
    /// nothing imported) and assigns it to Player Settings.
    /// Menu: Punchcard Operator/Regenerate Game Icon.</summary>
    public static class IconGenerator
    {
        private const string IconPath = "Assets/PunchcardOperator/Art/Icon_1024.png";

        [MenuItem("Punchcard Operator/Regenerate Game Icon")]
        public static void Generate()
        {
            const int S = 1024;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color32[S * S];

            // palette
            Color bgTop = new Color32(0x12, 0x15, 0x19, 0xFF);
            Color bgBot = new Color32(0x24, 0x2A, 0x33, 0xFF);
            Color card = new Color32(0xCE, 0xC2, 0x9C, 0xFF);
            Color cardShade = new Color32(0xB0, 0xA2, 0x79, 0xFF);
            Color hole = new Color32(0x0E, 0x10, 0x13, 0xFF);
            Color amber = new Color32(0xF2, 0xA5, 0x3A, 0xFF);
            Color amberHi = new Color32(0xFF, 0xC6, 0x6E, 0xFF);

            Vector2 c = new Vector2(S * 0.5f, S * 0.46f);
            float ang = -9f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(-ang), sin = Mathf.Sin(-ang);

            // card rect in local space (centred), with a cut top-left corner
            float cw = S * 0.66f, ch = S * 0.46f;
            float cornerCut = S * 0.13f;

            // hole row
            int holes = 6;
            float holeW = cw * 0.085f, holeH = ch * 0.16f;
            float rowY = -ch * 0.12f;
            float firstX = -cw * 0.5f + cw * 0.11f;
            float gap = (cw * 0.78f) / (holes - 1);
            int punchIndex = 4; // which hole the head is driving

            for (int y = 0; y < S; y++)
            {
                float ty = (float)y / (S - 1);
                Color bg = Color.Lerp(bgBot, bgTop, ty);
                for (int x = 0; x < S; x++)
                {
                    Color o = bg;

                    // to card-local space
                    float dx = x - c.x, dy = y - c.y;
                    float lx = dx * cos - dy * sin;
                    float ly = dx * sin + dy * cos;

                    bool inCard = Mathf.Abs(lx) <= cw * 0.5f && Mathf.Abs(ly) <= ch * 0.5f;
                    // cut the top-left corner (in local space: -x, +y)
                    if (inCard && (-lx) + (ly) > (cw * 0.5f + ch * 0.5f) - cornerCut) inCard = false;

                    if (inCard)
                    {
                        // soft top-light shading
                        float shade = Mathf.InverseLerp(-ch * 0.5f, ch * 0.5f, ly);
                        o = Color.Lerp(card, cardShade, shade * 0.6f);

                        // edge darkening
                        float ex = Mathf.Abs(lx) / (cw * 0.5f);
                        float ey = Mathf.Abs(ly) / (ch * 0.5f);
                        float edge = Mathf.Max(ex, ey);
                        if (edge > 0.94f) o = Color.Lerp(o, hole, (edge - 0.94f) / 0.06f * 0.5f);

                        // punched holes
                        for (int h = 0; h < holes; h++)
                        {
                            float hx = firstX + h * gap;
                            if (Mathf.Abs(lx - hx) <= holeW * 0.5f && Mathf.Abs(ly - rowY) <= holeH * 0.5f)
                            {
                                if (h <= punchIndex) o = hole;               // already punched
                                else o = Color.Lerp(o, hole, 0.15f);          // faint guide
                            }
                        }
                    }

                    // punch head / plunger above the active hole (screen-space vertical bar)
                    float headX = firstX + punchIndex * gap;
                    // convert head local x to a rough screen column
                    float headScreenX = c.x + headX * cos + 0f;
                    float headScreenY = c.y + headX * sin;
                    float barW = holeW * 0.9f;
                    // draw the plunger in local space so it aligns with the card
                    if (Mathf.Abs(lx - headX) <= barW * 0.5f && ly > rowY + holeH * 0.2f && ly < ch * 0.60f)
                    {
                        float g = Mathf.InverseLerp(rowY, ch * 0.60f, ly);
                        o = Color.Lerp(amber, amberHi, 1f - g);
                    }
                    // plunger cap
                    if (Mathf.Abs(lx - headX) <= barW * 1.1f && ly >= ch * 0.52f && ly <= ch * 0.64f)
                        o = amber;

                    px[y * S + x] = o;
                }
            }

            tex.SetPixels32(px);
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
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            AssignIcon(icon);
            AssetDatabase.SaveAssets();
            Debug.Log("[Punchcard] Icon regenerated and assigned: " + IconPath);
        }

        private static void AssignIcon(Texture2D icon)
        {
            var tex = new[] { icon };
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, tex, IconKind.Any);   // default
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, tex, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.iOS, tex, IconKind.Any);
        }
    }
}
