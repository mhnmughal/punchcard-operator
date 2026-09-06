using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PunchcardOperator
{
    [Serializable]
    public class LevelProgress
    {
        public string id;
        public bool completed;
        public int bestRating;       // (int)LevelRating
        public int bestEditCount = -1;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public bool tutorialComplete;
        public int highestUnlockedIndex;   // highest campaign orderIndex the player may enter
        public bool systemCertified;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;

        public LevelProgress GetOrCreate(string id)
        {
            var p = levels.Find(l => l.id == id);
            if (p == null) { p = new LevelProgress { id = id }; levels.Add(p); }
            return p;
        }
    }

    /// <summary>JSON save to persistentDataPath. Safe defaults on missing/corrupt data.
    /// Offline only - no network, no cloud.</summary>
    public static class SaveSystem
    {
        public const string FileName = "punchcard_operator_save.json";
        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        private static SaveData _cache;
        public static SaveData Data => _cache ?? (_cache = Load());

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    var json = File.ReadAllText(Path);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null && data.levels != null)
                    {
                        _cache = data;
                        return data;
                    }
                    Debug.LogWarning("[Punchcard] Save file unreadable - starting fresh.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Punchcard] Save load failed ({e.Message}) - starting fresh.");
            }
            _cache = new SaveData();
            return _cache;
        }

        public static void Save()
        {
            try
            {
                var json = JsonUtility.ToJson(Data, true);
                var tmp = Path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(Path)) File.Delete(Path);
                File.Move(tmp, Path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Punchcard] Save failed: {e.Message}");
            }
        }

        public static void ResetProgress()
        {
            _cache = new SaveData();
            Save();
        }
    }

    /// <summary>Progression rules: tutorial gate, sequential unlock, non-regression of
    /// ratings and unlocks, final certification.</summary>
    public static class ProgressionManager
    {
        public static bool IsUnlocked(PunchCardLevelDefinition def)
        {
            if (def == null) return false;
            if (def.isTutorial) return true;
            if (!SaveSystem.Data.tutorialComplete) return false;
            return def.orderIndex <= SaveSystem.Data.highestUnlockedIndex;
        }

        public static void CompleteTutorial()
        {
            if (!SaveSystem.Data.tutorialComplete)
            {
                SaveSystem.Data.tutorialComplete = true;
                SaveSystem.Save();
            }
        }

        public static LevelRating BestRating(string id)
        {
            var p = SaveSystem.Data.levels.Find(l => l.id == id);
            return p != null ? (LevelRating)p.bestRating : LevelRating.None;
        }

        /// <summary>Records a campaign result. Never downgrades a rating, never relocks.</summary>
        public static void RecordCompletion(PunchCardLevelDefinition def, LevelRating rating, int editCount, LevelDatabase db)
        {
            if (def == null || def.isTutorial) return;

            var p = SaveSystem.Data.GetOrCreate(def.id);
            p.completed = true;
            if ((int)rating > p.bestRating)
            {
                p.bestRating = (int)rating;
                p.bestEditCount = editCount;
            }
            else if (rating != LevelRating.None && (p.bestEditCount < 0 || editCount < p.bestEditCount))
            {
                p.bestEditCount = editCount;
            }

            int nextIndex = def.orderIndex + 1;
            if (nextIndex > SaveSystem.Data.highestUnlockedIndex)
                SaveSystem.Data.highestUnlockedIndex = nextIndex;

            if (db != null)
            {
                var campaign = db.Campaign;
                if (campaign.Count > 0 && def.orderIndex >= campaign[campaign.Count - 1].orderIndex)
                    SaveSystem.Data.systemCertified = true;
            }

            SaveSystem.Save();
        }
    }
}
