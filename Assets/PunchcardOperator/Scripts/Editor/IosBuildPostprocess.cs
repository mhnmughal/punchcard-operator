using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Trims the generated Xcode project for an offline game: declares
    /// export-compliance (no non-exempt encryption) so App Store Connect doesn't
    /// prompt, and drops AVKit (video playback - the game has no video).
    /// Not wrapped in #if UNITY_IOS so it still runs when a build is kicked off
    /// right after a platform switch, before editor scripts recompile.</summary>
    public sealed class IosBuildPostprocess : IPostprocessBuildWithReport
    {
        public int callbackOrder => 90;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string path = report.summary.outputPath;

            // Info.plist - export compliance
            string plistPath = System.IO.Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);

            // drop the AVKit framework (video) from the target
            string pbxPath = PBXProject.GetPBXProjectPath(path);
            var pbx = new PBXProject();
            pbx.ReadFromFile(pbxPath);
            foreach (var guid in new[] { pbx.GetUnityMainTargetGuid(), pbx.GetUnityFrameworkTargetGuid() })
            {
                if (string.IsNullOrEmpty(guid)) continue;
                pbx.RemoveFrameworkFromProject(guid, "AVKit.framework");
            }
            pbx.WriteToFile(pbxPath);

            Debug.Log("[Punchcard] iOS post-process: export-compliance set, AVKit removed.");
        }
    }
}
