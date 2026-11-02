// @role 2x story speed (Tab, or the title page): dialogue, messages, typing, loading bars and scripted waits run twice as fast - walking and watchers don't
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Story speed. 2x makes everything you WAIT for faster: subtitles (typing + how long they stay), phone messages,
    /// start-up / app loading, typing a reply, scripted pauses between story beats (e.g. after lights out).
    /// It does NOT speed up walking, the watchers or the class-photo countdown (that would change the challenge).
    /// Saved, so it stays on between chapters. Toggle with Tab in a chapter, or on the title page.
    /// </summary>
    public static class GameSpeed
    {
        const string Key = "cover_speed2x";
        static int fast = -1;

        public static bool Fast
        {
            get { if (fast < 0) fast = PlayerPrefs.GetInt(Key, 0); return fast == 1; }
            set { fast = value ? 1 : 0; PlayerPrefs.SetInt(Key, fast); PlayerPrefs.Save(); }
        }

        public static float Factor { get { return Fast ? 2f : 1f; } }

        /// <summary>Time.deltaTime at story speed.</summary>
        public static float Dt { get { return Time.deltaTime * Factor; } }
    }
}
