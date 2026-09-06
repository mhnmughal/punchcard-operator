using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PunchcardOperator;

namespace PunchcardOperator.Tests
{
    /// <summary>End-to-end screen flow: Boot routes correctly on first launch vs.
    /// a returning player, the pause menu works, and Quit-to-Vault lands in the
    /// right Menu view.</summary>
    public class FlowTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.ResetProgress();
            Fade.Enabled = false;
            Time.timeScale = 1f;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            SaveSystem.ResetProgress();
        }

        [UnityTest]
        public IEnumerator Boot_FirstLaunch_RoutesToOperatorTraining()
        {
            SaveSystem.ResetProgress(); // tutorialComplete = false
            yield return SceneManager.LoadSceneAsync("Boot");
            var boot = Object.FindFirstObjectByType<BootController>();
            Assert.IsNotNull(boot, "no BootController");

            // skip the power-on sequence
            boot.GetType().GetMethod("Route", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(boot, null);
            yield return null; yield return null;

            Assert.AreEqual("Game", SceneManager.GetActiveScene().name);
            var gc = Object.FindFirstObjectByType<GameController>();
            yield return null;
            var def = (PunchCardLevelDefinition)gc.GetType()
                .GetField("_def", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(gc);
            Assert.IsNotNull(def);
            Assert.AreEqual("00", def.id);
            Assert.IsTrue(def.isTutorial);
        }

        [UnityTest]
        public IEnumerator Boot_ReturningPlayer_RoutesToControlRoom()
        {
            SaveSystem.ResetProgress();
            SaveSystem.Data.tutorialComplete = true;
            SaveSystem.Save();

            yield return SceneManager.LoadSceneAsync("Boot");
            var boot = Object.FindFirstObjectByType<BootController>();
            boot.GetType().GetMethod("Route", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(boot, null);
            yield return null; yield return null;

            Assert.AreEqual("Menu", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindFirstObjectByType<MenuController>());
        }

        [UnityTest]
        public IEnumerator Pause_StopsTime_ResumeRestoresIt()
        {
            AppState.PendingLevelId = "01";
            yield return SceneManager.LoadSceneAsync("Game");
            for (int i = 0; i < 4; i++) yield return null;
            var gc = Object.FindFirstObjectByType<GameController>();
            var setPaused = gc.GetType().GetMethod("SetPaused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            setPaused.Invoke(gc, new object[] { true });
            Assert.AreEqual(0f, Time.timeScale, "pause did not stop time");
            var overlay = GameObject.Find("Canvas/Root/PauseOverlay");
            Assert.IsNotNull(overlay, "no pause overlay");
            var resume = FindButtonByLabel(overlay.transform, "RESUME");
            Assert.IsNotNull(resume, "no RESUME button");

            resume.onClick.Invoke();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "resume did not restore time");
            Assert.IsNull(GameObject.Find("Canvas/Root/PauseOverlay"), "pause overlay not removed");
        }

        private static UnityEngine.UI.Button FindButtonByLabel(Transform root, string label)
        {
            foreach (var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var t = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (t != null && t.text == label) return b;
            }
            return null;
        }
    }
}
