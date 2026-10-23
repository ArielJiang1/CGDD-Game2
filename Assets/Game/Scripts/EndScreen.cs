// @role Page after each chapter: CAUGHT (retry), chapter complete (continue), or one of the two endings
// @calls GameFlow, AmbientAudio, AudioSynth
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// The page between chapters (builds its own UI). Reads GameFlow.lastResult / lastLevel / endingChoice.
    ///  - Caught      -> what happened in that chapter. [Retry chapter] [Title]
    ///  - Chapter won -> a short "what you did". [Continue] [Title]
    ///  - Last chapter-> ENDING (The Seat / Lights) + epilogue + credits. [Play Again] [Title]
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        static readonly Color Paper = new Color(0.93f, 0.90f, 0.82f);
        static readonly Color Red = new Color(0.86f, 0.18f, 0.14f);
        static readonly Color Warm = new Color(1f, 0.85f, 0.55f);

        static readonly string[] WinHead = { "SOMEONE ANSWERED", "THE STAIRS GO DOWN", "SOMEONE FLASHED BACK" };
        static readonly string[] WinBody =
        {
            "The one in front of you is one of them.\nAfter the bell: the stairwell.",
            "A message, passed hand to hand.\nThe door marked STAFF was never locked.\nIt was only watched.",
            "Three lights across the yard.\nOther dorms. Other rooms. Other phones.\nYou are not the first. You are not the only.",
        };
        static readonly string[] CaughtHead = { "CAUGHT", "CONFISCATED", "LIGHTS OUT", "INSPECTED" };
        static readonly string[] CaughtBody =
        {
            "Three warnings.\nThe phone is his now.",
            "The Lunch Monitor files a report.\nThe stairs stay closed. Lunch continues.",
            "He took it with the flashlight still on your face.\nSweet dreams.",
            "One item found.\n\"You'll thank me. Just like I was.\"",
        };

        Text head, body, small;
        AmbientAudio audioOut;

        void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UIFactory.EnsureEventSystem();
            audioOut = AmbientAudio.Create(transform);
            audioOut.droneVolume = 0.08f;
            audioOut.ventVolume = 0.03f;

            int lvl = Mathf.Max(0, GameFlow.LevelIndex(GameFlow.lastLevel));
            bool won = GameFlow.lastResult == LessonResult.Win;
            bool final = won && GameFlow.IsLastLevel(GameFlow.lastLevel);

            Canvas canvas = UIFactory.MakeCanvas("EndCanvas", 10);
            Transform root = canvas.transform;
            UIFactory.AddImage(UIFactory.Stretch("BG", root), final && GameFlow.endingChoice == 2 ? new Color(0.06f, 0.08f, 0.1f) : new Color(0.05f, 0.05f, 0.055f));
            Color accent = won ? Warm : Red;
            UIFactory.AddImage(UIFactory.Rect("Bar", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f)), accent);
            Text label = UIFactory.AddText(UIFactory.Centered("Label", root, 0f, 330f, 1600f, 40f), "", 24, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter);
            head = UIFactory.AddText(UIFactory.Centered("Headline", root, 0f, 230f, 1700f, 140f), "", 96, accent, TextAnchor.MiddleCenter);
            head.fontStyle = FontStyle.Bold;
            body = UIFactory.AddText(UIFactory.Centered("Body", root, 0f, 30f, 1500f, 260f), "", 32, Paper, TextAnchor.MiddleCenter);
            small = UIFactory.AddText(UIFactory.Centered("Small", root, 0f, -150f, 1500f, 110f), "", 22, new Color(0.62f, 0.62f, 0.64f), TextAnchor.MiddleCenter);
            UIFactory.AddText(UIFactory.Rect("Footer", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1600f, 34f)),
                "CLEARVIEW ACADEMY  -  WHAT IS SEEN, IS SAFE.", 18, new Color(0.7f, 0.22f, 0.2f), TextAnchor.MiddleCenter);

            string h, b, s = "";
            Button primary;
            if (final)
            {
                bool lights = GameFlow.endingChoice == 2;
                label.text = "ENDING " + (lights ? "2 / 2" : "1 / 2");
                h = lights ? "LIGHTS" : "THE SEAT";
                b = lights
                    ? "Every hidden screen in the room lit up at once.\nThat afternoon, the room at the bottom of the stairs was full.\nHe closed the door. From the outside."
                    : "The phone stays behind the panel.\nNext year, someone new sits in seat thirty-one.\nThey will find it. They always do.";
                s = (lights ? "The PA said nothing happened today. For once, it was lying." : "What is seen, is safe. What is hidden, is passed on.")
                    + "\n\nCOVER  -  a game by Ariel & Nina  -  CGDD 2026";
                primary = UIFactory.MakeButton("PrimaryButton", root, "Play Again", -220f, -300f, 380f, 90f);
                primary.onClick.AddListener(GameFlow.StartNewGame);
                audioOut.Play(lights ? AudioSynth.Confirm() : AudioSynth.Chime(), 0.4f);
            }
            else if (won)
            {
                label.text = "CHAPTER " + (lvl + 1) + "  COMPLETE";
                h = WinHead[Mathf.Clamp(lvl, 0, WinHead.Length - 1)];
                b = WinBody[Mathf.Clamp(lvl, 0, WinBody.Length - 1)];
                if (lvl + 1 < GameFlow.LevelTitles.Length) s = "NEXT:  CHAPTER " + GameFlow.LevelTitles[lvl + 1];
                primary = UIFactory.MakeButton("PrimaryButton", root, "Continue", -220f, -300f, 380f, 90f);
                primary.onClick.AddListener(GameFlow.NextLevel);
                audioOut.Play(AudioSynth.Confirm(), 0.35f);
            }
            else
            {
                label.text = "CHAPTER " + (lvl + 1) + "  -  GAME OVER";
                h = CaughtHead[Mathf.Clamp(lvl, 0, CaughtHead.Length - 1)];
                b = CaughtBody[Mathf.Clamp(lvl, 0, CaughtBody.Length - 1)];
                s = "Try again from the start of this chapter.";
                primary = UIFactory.MakeButton("PrimaryButton", root, "Retry", -220f, -300f, 380f, 90f);
                primary.onClick.AddListener(GameFlow.RetryLevel);
                audioOut.Play(AudioSynth.Stinger(), 0.5f);
            }
            Button title = UIFactory.MakeButton("TitleButton", root, "Title", 220f, -300f, 380f, 90f);
            title.onClick.AddListener(GameFlow.GoToTitle);
            StartCoroutine(Type(h, b, s));
        }

        IEnumerator Type(string h, string b, string s)
        {
            yield return TypeInto(head, h, 18f);
            yield return new WaitForSeconds(0.25f);
            yield return TypeInto(body, b, 60f);
            small.text = s;
        }

        IEnumerator TypeInto(Text t, string s, float cps)
        {
            for (int i = 1; i <= s.Length; i++)
            {
                t.text = s.Substring(0, i);
                if (char.IsLetterOrDigit(s[i - 1]) && i % 2 == 0) audioOut.Play(AudioSynth.Tick(), 0.08f, Random.Range(0.9f, 1.1f));
                if (GameInput.AnyKeyDown()) { t.text = s; yield break; }
                yield return new WaitForSeconds(1f / cps);
            }
        }
    }
}
