using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PunchcardOperator
{
    /// <summary>Validates the whole campaign. Target result: "13 / 13 VALID".
    /// Level 0 (tutorial) + Levels 01..12 (campaign).</summary>
    public static class LevelValidator
    {
        public static readonly string[] ExpectedIds =
            { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" };

        public class Issue
        {
            public string levelId;
            public string message;
            public override string ToString() => $"[{levelId}] {message}";
        }

        public class Report
        {
            public int total;
            public int valid;
            public readonly List<Issue> issues = new List<Issue>();
            public bool AllValid => total == ExpectedIds.Length && valid == total && issues.Count == 0;
            public string Summary => $"{valid} / {total} VALID";
        }

        public static Report Validate(LevelDatabase db)
        {
            var r = new Report { total = ExpectedIds.Length };
            if (db == null)
            {
                r.issues.Add(new Issue { levelId = "-", message = "Level database is null." });
                return r;
            }

            var byId = new Dictionary<string, PunchCardLevelDefinition>();
            foreach (var lvl in db.levels)
            {
                if (lvl == null) { r.issues.Add(new Issue { levelId = "-", message = "Null entry in database." }); continue; }
                if (byId.ContainsKey(lvl.id))
                    r.issues.Add(new Issue { levelId = lvl.id, message = "Duplicate level id." });
                else
                    byId[lvl.id] = lvl;
            }

            foreach (var id in ExpectedIds)
            {
                if (!byId.TryGetValue(id, out var lvl))
                {
                    r.issues.Add(new Issue { levelId = id, message = "Missing expected level." });
                    continue;
                }

                int before = r.issues.Count;
                ValidateOne(lvl, r);
                if (r.issues.Count == before) r.valid++;
            }

            foreach (var kv in byId)
            {
                bool expected = System.Array.IndexOf(ExpectedIds, kv.Key) >= 0;
                if (!expected)
                    r.issues.Add(new Issue { levelId = kv.Key, message = "Unexpected extra level (campaign is 00..12 only)." });
            }

            return r;
        }

        private static void ValidateOne(PunchCardLevelDefinition lvl, Report r)
        {
            void Err(string m) => r.issues.Add(new Issue { levelId = lvl.id, message = m });

            bool isTut = lvl.id == "00";
            if (lvl.isTutorial != isTut)
                Err(isTut ? "Level 00 must have isTutorial = true." : "Only Level 00 may have isTutorial = true.");

            if (!isTut)
            {
                int expectedOrder = int.Parse(lvl.id) - 1;
                if (lvl.orderIndex != expectedOrder)
                    Err($"orderIndex should be {expectedOrder} but is {lvl.orderIndex}.");
                if (lvl.bank == ProgramBank.None)
                    Err("Campaign level has no Program Bank assigned.");
            }

            if (lvl.grid == null || lvl.grid.width < 1 || lvl.grid.height < 1)
            { Err("Invalid grid dimensions."); return; }

            void CheckMachine(MachineConfig m, string name, bool required)
            {
                if (m == null) { if (required) Err($"{name} config missing."); return; }
                if (!required && !m.enabled) return;
                if (!lvl.grid.InBounds(m.start)) Err($"{name} start {m.start} is off the grid.");
                if (!lvl.grid.InBounds(m.target)) Err($"{name} target {m.target} is off the grid.");
                if (lvl.grid.IsWall(m.start)) Err($"{name} start is inside a wall.");
                if (lvl.grid.IsWall(m.target)) Err($"{name} target is inside a wall.");
                if (m.start == m.target) Err($"{name} start and target are identical.");
            }

            CheckMachine(lvl.machineA, "Machine A", true);
            CheckMachine(lvl.machineB, "Machine B", false);

            if (lvl.solutionCard == null || lvl.solutionCard.rows.Count == 0)
            { Err("solutionCard is empty."); return; }

            bool usesB = lvl.DualChannel;
            foreach (var row in lvl.solutionCard.rows)
            {
                if (row.op == CardOp.Repeat && row.repeatCount < 1)
                    Err("solutionCard has a Repeat with count < 1.");
                if (!usesB && row.channel == Channel.B)
                    Err("solutionCard uses Channel B on a single-channel level.");
            }

            if (lvl.initialCard == null || lvl.initialCard.rows.Count == 0)
                Err("initialCard is empty.");
            else if (lvl.initialCard.rows.Count != lvl.solutionCard.rows.Count)
                Err("initialCard and solutionCard have different row counts.");

            var result = PunchCardSimulator.Run(lvl.grid, lvl.machineA, lvl.machineB, lvl.solutionCard);
            if (!result.success)
                Err($"solutionCard does not solve the level: {result.failureReason}");

            if (!isTut)
            {
                if (lvl.perfectEditTarget < 0 || lvl.compactEditTarget < lvl.perfectEditTarget)
                    Err($"Rating targets invalid (perfect {lvl.perfectEditTarget}, compact {lvl.compactEditTarget}).");
                if (lvl.solutionEditCount > lvl.compactEditTarget)
                    Err($"solutionEditCount {lvl.solutionEditCount} exceeds compact target {lvl.compactEditTarget}.");
                if (lvl.availableTools == null || lvl.availableTools.Count == 0)
                    Err("Campaign level exposes no editing tools.");
            }

            if (lvl.DualChannel && lvl.id != "11" && lvl.id != "12")
                Err("Dual Channel is only introduced at Level 11 (allowed again at 12).");

            if (isTut && (lvl.tutorialSteps == null || lvl.tutorialSteps.Count < 3))
                Err("Tutorial must define at least 3 coach-light steps.");
        }

        public static string FormatReport(Report r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PUNCHCARD OPERATOR - LEVEL VALIDATION");
            sb.AppendLine(r.Summary);
            if (r.issues.Count == 0) sb.AppendLine("No issues.");
            foreach (var i in r.issues) sb.AppendLine("  FAIL " + i);
            return sb.ToString();
        }
    }
}
