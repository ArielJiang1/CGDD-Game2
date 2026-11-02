// @role Scene flow: Title -> Chapter 1-6 (Monday to Friday) -> End page (win: next chapter, caught: retry); saves progress and the player name
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhoneGame
{
    public enum LessonResult { None, Win, Caught, TimeUp }

    /// <summary>
    /// Scene-to-scene flow. Static, so it survives scene loads without a DontDestroyOnLoad object.
    /// Title -> Chapter 1..6 -> End (after every chapter: win = Continue, caught = Retry this chapter).
    /// The last chapter has two endings (endingChoice 1 = THE SEAT, 2 = LIGHTS).
    /// Progress (highest chapter reached + the player's name) is saved in PlayerPrefs.
    /// </summary>
    public static class GameFlow
    {
        public const string TitleScene = "Title";
        public const string EndScene = "End";
        public static readonly string[] Levels = { "Level01_Lesson", "Level02_Lunch", "Level03_LightsOut", "Level04_PictureDay", "Level05_Archive", "Level06_Inspector" };
        public static readonly string[] LevelTitles = { "1   THE LESSON", "2   LUNCH", "3   LIGHTS OUT", "4   PICTURE DAY", "5   THE ARCHIVE", "6   INSPECTION DAY" };

        public static bool skipIntro;
        /// <summary>Typed on the title page. The teacher uses it in the roll call and when he calls on you.</summary>
        public static string playerName = "Student";
        public static LessonResult lastResult = LessonResult.None;
        public static string lastLevel = "Level01_Lesson";
        public static string lastReason = "";
        /// <summary>0 = not finished, 1 = "The Seat" (hid it), 2 = "Lights" (sent it to everyone).</summary>
        public static int endingChoice;

        const string KeyReached = "cover_reached";
        const string KeyName = "cover_name";
        static bool nameLoaded;

        public static void LoadSavedName()
        {
            if (nameLoaded) return;
            nameLoaded = true;
            string n = PlayerPrefs.GetString(KeyName, "");
            if (!string.IsNullOrEmpty(n)) playerName = n;
        }

        /// <summary>Highest chapter index (0-based) the player may start from.</summary>
        public static int ReachedLevel
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(KeyReached, 0), 0, Levels.Length - 1); }
            set { PlayerPrefs.SetInt(KeyReached, Mathf.Clamp(value, 0, Levels.Length - 1)); PlayerPrefs.Save(); }
        }

        public static void SaveName(string n)
        {
            playerName = string.IsNullOrEmpty(n) ? "Student" : n;
            nameLoaded = true;
            PlayerPrefs.SetString(KeyName, playerName);
            PlayerPrefs.Save();
        }

        public static int LevelIndex(string scene) { return System.Array.IndexOf(Levels, scene); }
        public static bool IsLastLevel(string scene) { return LevelIndex(scene) == Levels.Length - 1; }

        public static void StartNewGame()
        {
            ReachedLevel = 0;
            LoadLevel(0);
        }

        public static void ContinueGame() { LoadLevel(ReachedLevel); }

        public static void LoadLevel(int i)
        {
            LoadSavedName();
            skipIntro = false;
            endingChoice = 0;
            Load(Levels[Mathf.Clamp(i, 0, Levels.Length - 1)]);
        }

        /// <summary>After a win: the next chapter.</summary>
        public static void NextLevel()
        {
            int i = LevelIndex(lastLevel);
            if (i < 0 || i >= Levels.Length - 1) { GoToTitle(); return; }
            LoadLevel(i + 1);
        }

        /// <summary>Inside-style: back to the start of the SAME chapter.</summary>
        public static void RetryLevel()
        {
            skipIntro = true;
            endingChoice = 0;
            Load(lastLevel);
        }

        public static void GoToTitle() { Load(TitleScene); }

        public static void EndLevel(LessonResult result, string reason)
        {
            lastResult = result;
            lastReason = reason;
            lastLevel = SceneManager.GetActiveScene().name;
            int i = LevelIndex(lastLevel);
            if (result == LessonResult.Win && i >= 0 && i + 1 < Levels.Length && ReachedLevel < i + 1) ReachedLevel = i + 1;
            Load(EndScene);
        }

        static void Load(string scene)
        {
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}
