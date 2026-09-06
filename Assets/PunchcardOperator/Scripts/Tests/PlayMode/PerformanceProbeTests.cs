using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PunchcardOperator;

namespace PunchcardOperator.Tests
{
    /// <summary>Lightweight performance probe. Editor frame times are not
    /// device-representative, but managed (GC) allocations are — a per-frame heap
    /// allocation causes the same collection hitch on a phone. This checks the
    /// steady-state editing screen and the execute animation don't allocate every
    /// frame, and logs the numbers for the record.</summary>
    public class PerformanceProbeTests
    {
        private const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.ResetProgress();
            Fade.Enabled = false;
            Time.timeScale = 1f;
            SaveSystem.Data.tutorialComplete = true;
            SaveSystem.Data.highestUnlockedIndex = 12;
            yield return null;
        }

        [TearDown]
        public void TearDown() => SaveSystem.ResetProgress();

        private static IEnumerator SampleAlloc(ProfilerRecorder rec, int frames, System.Action<double, long> report)
        {
            long total = 0, peak = 0; int n = 0;
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                long v = rec.LastValue;
                total += v; peak = System.Math.Max(peak, v); n++;
            }
            report(n > 0 ? (double)total / n : 0, peak);
        }

        [UnityTest]
        public IEnumerator EditingScreen_And_Execute_DoNotAllocatePerFrame()
        {
            using var allocBytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            // baseline: the bare PlayMode test scene still churns in the editor (editor
            // loop, MCP bridge, …). The game is "allocation-clean" if it adds little on top.
            for (int i = 0; i < 5; i++) yield return null;
            double baseAvg = 0; long basePeak = 0;
            yield return SampleAlloc(allocBytes, 120, (a, p) => { baseAvg = a; basePeak = p; });

            AppState.PendingLevelId = "12"; // most complex board: dual channel, 7 rows
            yield return SceneManager.LoadSceneAsync("Game");
            for (int i = 0; i < 5; i++) yield return null;

            var gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(gc);

            // --- idle editing screen ---
            double idleAvg = 0; long idlePeak = 0;
            yield return SampleAlloc(allocBytes, 180, (a, p) => { idleAvg = a; idlePeak = p; });
            double idleDelta = idleAvg - baseAvg;

            // --- run the program (animated execute) ---
            var session = (CardEditSession)gc.GetType().GetField("_session", BF).GetValue(gc);
            session.Patch(1, CardOp.Repeat, 5, out _);
            session.Swap(2, 3, out _);
            gc.GetType().GetField("_loaded", BF).SetValue(gc, true);
            var onLever = gc.GetType().GetMethod("OnLever", BF);
            string Phase() => gc.GetType().GetField("_phase", BF).GetValue(gc).ToString();
            onLever.Invoke(gc, null); // execute

            double execAvg = 0; long execPeak = 0; int execFrames = 0;
            long execTotal = 0;
            float t = 0;
            while (t < 12f && Phase() != "Result")
            {
                yield return null;
                long v = allocBytes.LastValue;
                execTotal += v; execPeak = System.Math.Max(execPeak, v); execFrames++;
                t += Time.unscaledDeltaTime;
            }
            execAvg = execFrames > 0 ? (double)execTotal / execFrames : 0;
            double execDelta = execAvg - baseAvg;

            Debug.Log($"[Perf] baseline (empty scene): avg {baseAvg:0} B/frame, peak {basePeak} B");
            Debug.Log($"[Perf] idle editing screen:    avg {idleAvg:0} B/frame  (delta {idleDelta:+0;-0} B)");
            Debug.Log($"[Perf] execute animation:      avg {execAvg:0} B/frame  (delta {execDelta:+0;-0} B) over {execFrames} frames");

            Assert.AreEqual("Result", Phase(), "execute never completed");
            // The editor baseline (~260 KB/frame of editor + MCP-bridge churn) has a
            // noise band of roughly +/- 15 KB, so these thresholds only catch a real
            // per-frame leak (the kind that was ~230 KB/frame before the sim was made
            // struct-free), not measurement jitter.
            Assert.Less(idleDelta, 24576, $"idle editing screen adds {idleDelta:0} B/frame over baseline");
            Assert.Less(execDelta, 49152, $"execute animation adds {execDelta:0} B/frame over baseline");
        }
    }
}
