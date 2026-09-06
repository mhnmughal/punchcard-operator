using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Punchcard Operator is fully offline. A transitive test-tooling package
    /// (com.unity.test-framework.performance) injects android.permission.INTERNET via a
    /// build hook. This post-processor removes network permissions from the generated
    /// Gradle project's manifest so the shipped app requests no network access.</summary>
    public class StripNetworkPermissions : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        private static readonly string[] Drop =
        {
            "android.permission.INTERNET",
            "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.ACCESS_WIFI_STATE",
        };

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            foreach (var manifest in Directory.GetFiles(path, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(manifest);
                var original = text;
                foreach (var perm in Drop)
                    text = Regex.Replace(text,
                        @"[ \t]*<uses-permission[^>]*android:name=""" + Regex.Escape(perm) + @"""[^>]*/>\s*",
                        "\n");
                if (text != original)
                {
                    File.WriteAllText(manifest, text);
                    Debug.Log($"[Punchcard] Stripped network permissions from {manifest}");
                }
            }
        }
    }
}
