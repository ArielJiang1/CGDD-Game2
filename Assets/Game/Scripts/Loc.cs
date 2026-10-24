// @role Language setting (English / 中文): inline pairs L(en, zh) for story text, and a table T(en) for data text (lecture lines, posters, photo captions)
using System.Collections.Generic;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// The game's language. Chosen on the title page, saved in PlayerPrefs.
    ///   Loc.L("English", "中文")  - story text written next to its translation (subtitles, thoughts, phone messages)
    ///   Loc.T("English")          - looks the English up in LocTable (text stored in scenes or data lists)
    /// While building scenes in the editor everything is stored in English; it is translated when the game runs.
    /// </summary>
    public static class Loc
    {
        const string Key = "cover_lang";
        static int lang = -1;

        public static bool Chinese
        {
            get { if (lang < 0) lang = PlayerPrefs.GetInt(Key, 0); return lang == 1 && Application.isPlaying; }
            set { lang = value ? 1 : 0; PlayerPrefs.SetInt(Key, lang); PlayerPrefs.Save(); }
        }

        public static string L(string en, string zh) { return Chinese && !string.IsNullOrEmpty(zh) ? zh : en; }

        public static string T(string en)
        {
            if (!Chinese || string.IsNullOrEmpty(en)) return en;
            string zh;
            return LocTable.Table.TryGetValue(en, out zh) ? zh : en;
        }

        public static string[] T(string[] en)
        {
            if (en == null) return null;
            string[] o = new string[en.Length];
            for (int i = 0; i < en.Length; i++) o[i] = T(en[i]);
            return o;
        }
    }
}
