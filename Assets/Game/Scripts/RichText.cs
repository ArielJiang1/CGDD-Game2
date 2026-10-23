using System.Collections.Generic;
using System.Text;

namespace PhoneGame
{
    /// <summary>
    /// Typewriter reveal that understands Unity rich-text tags (b, i, color):
    /// tags are never cut in half, and any tag still open at the cut is closed.
    /// </summary>
    public static class RichText
    {
        public static int VisibleLength(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '<')
                {
                    int j = s.IndexOf('>', i);
                    if (j > i) { i = j; continue; }
                }
                n++;
            }
            return n;
        }

        public static string Reveal(string s, int visible)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder();
            Stack<string> open = new Stack<string>();
            int shown = 0, i = 0;
            while (i < s.Length && shown < visible)
            {
                if (s[i] == '<')
                {
                    int j = s.IndexOf('>', i);
                    if (j > i)
                    {
                        string tag = s.Substring(i, j - i + 1);
                        sb.Append(tag);
                        if (tag.StartsWith("</")) { if (open.Count > 0) open.Pop(); }
                        else
                        {
                            string name = tag.Substring(1, tag.Length - 2);
                            int eq = name.IndexOf('=');
                            if (eq >= 0) name = name.Substring(0, eq);
                            open.Push(name);
                        }
                        i = j + 1;
                        continue;
                    }
                }
                sb.Append(s[i]);
                shown++;
                i++;
            }
            while (open.Count > 0) sb.Append("</").Append(open.Pop()).Append('>');
            return sb.ToString();
        }

        /// <summary>The visible character at position 'index' (tags skipped), or '\0'.</summary>
        public static char VisibleChar(string s, int index)
        {
            if (string.IsNullOrEmpty(s)) return '\0';
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '<')
                {
                    int j = s.IndexOf('>', i);
                    if (j > i) { i = j; continue; }
                }
                if (n == index) return s[i];
                n++;
            }
            return '\0';
        }
    }
}
