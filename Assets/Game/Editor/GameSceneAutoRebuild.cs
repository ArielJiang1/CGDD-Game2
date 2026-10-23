using UnityEditor;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// After every code update Unity reloads scripts. This checks whether the game scenes (Title, 4 chapters, End) were
    /// built with the current GameSceneBuilder.ScenesVersion, and if not, offers to rebuild them right away.
    /// (Stored per computer, so each teammate's Unity asks once.)
    /// </summary>
    [InitializeOnLoad]
    public static class GameSceneAutoRebuild
    {
        static string Key { get { return "PhoneGame.ScenesVersion." + Application.dataPath.GetHashCode(); } }

        static GameSceneAutoRebuild()
        {
            EditorApplication.delayCall += Check;
        }

        public static void MarkBuilt() { EditorPrefs.SetInt(Key, GameSceneBuilder.ScenesVersion); }

        static void Check()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (EditorPrefs.GetInt(Key, 0) >= GameSceneBuilder.ScenesVersion) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Blockout/Scenes/Blockout_School.unity") == null) return;

            int choice = EditorUtility.DisplayDialogComplex("Game scenes are out of date",
                "The game code was updated, but the game scenes (Title, Chapters 1-4, End) were built with an older version.\n\n" +
                "Rebuild them now? (Hand edits inside those scenes will be replaced. Blockout_School is not touched.)",
                "Rebuild now", "Later", "Don't ask for this version");
            if (choice == 0) GameSceneBuilder.BuildAll(true);
            else if (choice == 2) MarkBuilt();
        }
    }
}
