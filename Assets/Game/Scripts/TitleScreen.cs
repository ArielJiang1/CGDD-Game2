// @role Title page: New Game (asks your name) / Continue / Chapters / Quit
// @calls GameFlow, AmbientAudio
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Title page (builds its own UI): a CCTV-style frame (REC dot, timestamp, scanlines) over the school's motto.
    ///  New Game -> "What is your name?" (the teacher reads it in the roll call) -> Chapter 1.
    ///  Continue -> the furthest chapter reached.   Chapters -> any chapter (for playtests / demos).   Quit.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        static readonly Color Paper = new Color(0.93f, 0.90f, 0.82f);
        static readonly Color Red = new Color(0.86f, 0.18f, 0.14f);

        GameObject namePanel, chapterPanel;
        InputField nameField;
        Image recDot;
        Text stamp, warning;
        RectTransform scan;
        AmbientAudio audioOut;

        void Start()
        {
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameFlow.LoadSavedName();
            UIFactory.EnsureEventSystem();
            audioOut = AmbientAudio.Create(transform);
            audioOut.droneVolume = 0.1f;
            audioOut.ventVolume = 0.06f;
            Build();
        }

        void Update()
        {
            if (recDot != null) recDot.enabled = Mathf.Repeat(Time.time, 1.2f) < 0.7f;
            if (stamp != null) stamp.text = Loc.L("CAM 01   SEAT 31   ", "监控 01   座位 31   ") + System.DateTime.Now.ToString("HH:mm:ss");
            if (scan != null) scan.anchoredPosition = new Vector2(0f, -Mathf.Repeat(Time.time * 140f, 1200f));
            if (warning != null) warning.enabled = Mathf.Repeat(Time.time, 2f) < 1.6f;
            if (namePanel != null && namePanel.activeSelf && GameInput.EnterDown()) Begin();
            if (GameInput.EscDown()) { if (namePanel != null) namePanel.SetActive(false); if (chapterPanel != null) chapterPanel.SetActive(false); }
        }

        void Build()
        {
            Canvas canvas = UIFactory.MakeCanvas("TitleCanvas", 10);
            Transform root = canvas.transform;
            UIFactory.AddImage(UIFactory.Stretch("BG", root), new Color(0.11f, 0.13f, 0.13f));
            // pale institutional "wall" band + dado, like the corridors
            UIFactory.AddImage(UIFactory.Rect("Dado", root, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 300f)), new Color(0.2f, 0.3f, 0.28f));
            UIFactory.AddImage(UIFactory.Rect("DadoLine", root, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(0f, 6f)), new Color(0.55f, 0.5f, 0.32f));

            Text est = UIFactory.AddText(UIFactory.Centered("Est", root, 0f, 330f, 1400f, 40f), Loc.L("CLEARVIEW ACADEMY  -  EST. 1952", "明鉴学院  -  创立于 1952"), 26, new Color(0.68f, 0.74f, 0.7f), TextAnchor.MiddleCenter);
            est.fontStyle = FontStyle.Bold;
            Text title = UIFactory.AddText(UIFactory.Centered("Title", root, 0f, 220f, 1400f, 190f), "COVER", 180, Paper, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            UIFactory.AddShadow(title, new Color(0f, 0f, 0f, 0.6f), 5f);
            UIFactory.AddText(UIFactory.Centered("Sub", root, 0f, 105f, 1400f, 40f), Loc.L("a phone, passed down through the same seat", "一部手机，在同一个座位上代代相传"), 28, new Color(0.8f, 0.78f, 0.72f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Italic;
            warning = UIFactory.AddText(UIFactory.Centered("Motto", root, 0f, 50f, 1400f, 40f), Loc.L("WHAT IS SEEN, IS SAFE.", "被看见的，就是安全的。"), 26, Red, TextAnchor.MiddleCenter);
            warning.fontStyle = FontStyle.Bold;

            float y = -40f;
            Button start = UIFactory.MakeButton("NewGame", root, Loc.L("New Game", "新游戏"), 0f, y, 420f, 74f);
            start.onClick.AddListener(ShowNamePanel);
            y -= 88f;
            if (GameFlow.ReachedLevel > 0)
            {
                Button cont = UIFactory.MakeButton("Continue", root, Loc.L("Continue  -  Ch. ", "继续  -  第 ") + (GameFlow.ReachedLevel + 1) + Loc.L("", "章"), 0f, y, 420f, 74f);
                cont.onClick.AddListener(GameFlow.ContinueGame);
                y -= 88f;
            }
            Button chapters = UIFactory.MakeButton("Chapters", root, Loc.L("Chapters", "章节"), 0f, y, 420f, 74f);
            chapters.onClick.AddListener(() => chapterPanel.SetActive(true));
            y -= 88f;
            Button quit = UIFactory.MakeButton(Loc.L("Quit", "退出"), root, "Quit", 0f, y, 420f, 74f);
            quit.onClick.AddListener(Quit);

            UIFactory.AddText(UIFactory.Rect("Credits", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1600f, 34f)),
                Loc.L("A game by Ariel & Nina  -  CGDD 2026  -  WASD / Mouse / E / Hold Right Mouse: phone", "Ariel & Nina 作品  -  CGDD 2026  -  WASD / 鼠标 / E / 按住右键：手机"), 20, new Color(0.72f, 0.74f, 0.7f), TextAnchor.MiddleCenter);

            // CCTV overlay
            RectTransform scanRoot = UIFactory.Stretch("Scan", root);
            scan = UIFactory.Rect("Line", scanRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 90f));
            UIFactory.AddImage(scan, new Color(1f, 1f, 1f, 0.025f)).raycastTarget = false;
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = new Vector2(i % 2, i / 2);
                RectTransform c = UIFactory.Rect("Corner" + i, root, a, a, a, new Vector2(a.x > 0 ? -40f : 40f, a.y > 0 ? -40f : 40f), new Vector2(70f, 70f));
                Image h = UIFactory.AddImage(UIFactory.Rect("H", c, a, a, a, Vector2.zero, new Vector2(70f, 4f)), new Color(1f, 1f, 1f, 0.5f));
                Image v = UIFactory.AddImage(UIFactory.Rect("V", c, a, a, a, Vector2.zero, new Vector2(4f, 70f)), new Color(1f, 1f, 1f, 0.5f));
                h.raycastTarget = v.raycastTarget = false;
            }
            recDot = UIFactory.AddImage(UIFactory.Rect("Rec", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -66f), new Vector2(22f, 22f)), Red);
            recDot.raycastTarget = false;
            UIFactory.AddText(UIFactory.Rect("RecText", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(102f, -60f), new Vector2(200f, 34f)), Loc.L("REC", "录制"), 26, Red, TextAnchor.MiddleLeft).raycastTarget = false;
            stamp = UIFactory.AddText(UIFactory.Rect("Stamp", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -60f), new Vector2(600f, 34f)), "", 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleRight);
            stamp.raycastTarget = false;

            // language: English / 中文 (saved; the title reloads so every text and font switches)
            Button lang = UIFactory.MakeButton("Language", root, Loc.Chinese ? "语言：中文   >   English" : "Language: English   >   中文", 0f, y - 96f, 520f, 60f);
            Text langText = lang.GetComponentInChildren<Text>();
            if (langText != null) { langText.font = UIFactory.CjkFont; langText.fontSize = 24; }
            lang.onClick.AddListener(() => { Loc.Chinese = !Loc.Chinese; UnityEngine.SceneManagement.SceneManager.LoadScene(GameFlow.TitleScene); });

            BuildNamePanel(root);
            BuildChapterPanel(root);
        }

        void ShowNamePanel()
        {
            namePanel.SetActive(true);
            nameField.text = GameFlow.playerName == "Student" ? "" : GameFlow.playerName;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nameField.gameObject);
            nameField.ActivateInputField();
        }

        void Begin()
        {
            string n = nameField.text != null ? nameField.text.Trim() : "";
            GameFlow.SaveName(n);
            GameFlow.StartNewGame();
        }

        RectTransform Panel(string name, Transform root)
        {
            RectTransform panel = UIFactory.Stretch(name, root);
            Image bg = UIFactory.AddImage(panel, new Color(0.04f, 0.045f, 0.05f, 0.97f));
            bg.raycastTarget = true;   // blocks the buttons behind it
            return panel;
        }

        void BuildNamePanel(Transform root)
        {
            RectTransform panel = Panel("NamePanel", root);
            UIFactory.AddText(UIFactory.Centered("Notice", panel, 0f, 270f, 1400f, 90f),
                Loc.L("NOTICE TO NEW STUDENTS: Devices are not permitted. Every device is monitored.\nThis is for your safety. Thank you for being safe.", "新生须知：禁止携带电子设备。所有设备均受监控。\n这是为了你的安全。感谢你保持安全。"), 24, new Color(0.75f, 0.3f, 0.26f), TextAnchor.MiddleCenter);
            UIFactory.AddText(UIFactory.Centered("Ask", panel, 0f, 140f, 1200f, 90f), Loc.L("Roll call is in a minute.\nWhat is your name?", "马上要点名了。\n你叫什么名字？"), 40, Paper, TextAnchor.MiddleCenter);

            RectTransform box = UIFactory.Centered("NameField", panel, 0f, 10f, 560f, 80f);
            Image fieldBg = UIFactory.AddImage(box, Paper);
            fieldBg.raycastTarget = true;
            Text txt = UIFactory.AddText(UIFactory.Rect("Text", box, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, -10f)), "", 38, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleCenter);
            txt.supportRichText = false;
            Text ph = UIFactory.AddText(UIFactory.Rect("Placeholder", box, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, -10f)), Loc.L("type your name", "输入你的名字"), 34, new Color(0.45f, 0.43f, 0.4f), TextAnchor.MiddleCenter);
            ph.fontStyle = FontStyle.Italic;
            nameField = box.gameObject.AddComponent<InputField>();
            nameField.textComponent = txt;
            nameField.placeholder = ph;
            nameField.targetGraphic = fieldBg;
            nameField.characterLimit = 16;

            Button go = UIFactory.MakeButton("EnterClassButton", panel, Loc.L("Go to class", "去上课"), 0f, -120f, 360f, 84f);
            go.onClick.AddListener(Begin);
            UIFactory.AddText(UIFactory.Centered("EnterHint", panel, 0f, -200f, 800f, 40f), Loc.L("(Enter)    -    Esc: back", "（回车）    -    Esc：返回"), 22, new Color(0.6f, 0.6f, 0.62f), TextAnchor.MiddleCenter);
            namePanel = panel.gameObject;
            namePanel.SetActive(false);
        }

        void BuildChapterPanel(Transform root)
        {
            RectTransform panel = Panel("ChapterPanel", root);
            UIFactory.AddText(UIFactory.Centered("Head", panel, 0f, 300f, 1200f, 70f), Loc.L("CHAPTERS", "章节"), 56, Paper, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            for (int i = 0; i < GameFlow.Levels.Length; i++)
            {
                int idx = i;
                Button b = UIFactory.MakeButton("Chapter" + i, panel, Loc.T(GameFlow.LevelTitles[i]), 0f, 170f - i * 100f, 640f, 80f);
                b.onClick.AddListener(() => GameFlow.LoadLevel(idx));
            }
            Button back = UIFactory.MakeButton(Loc.L("Back", "返回"), panel, "Back", 0f, -290f, 300f, 74f);
            back.onClick.AddListener(() => chapterPanel.SetActive(false));
            chapterPanel = panel.gameObject;
            chapterPanel.SetActive(false);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
