// @role Shared chapter rules: one subtitle at a time, danger meter, 3 hidden warnings, objective panel, prompts, chapter cards
// @calls PlayerRig, PhoneController, DangerFX, AmbientAudio, AudioSynth, Interactable, GameFlow
// @tune chances:int:1-5:warnings before the phone is taken (never shown as a number)
// @tune noticeSeconds:float:0.5-6:seconds a watcher must see the phone before a warning
// @tune dangerDecayPerSecond:float:0.1-1:how fast danger drains once nobody is looking
// @tune graceAfterWarning:float:0-6:seconds of safety right after a warning
// @tune typeCharsPerSecond:float:20-120:subtitle typewriter speed
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Everything the four chapters share:
    ///  - ONE subtitle at a time: authority / students / PA at the top, the player's thoughts bottom-left.
    ///    Text is typed in (typewriter), with a small "voice" blip per speaker.
    ///  - Interaction ray (look at something + E), prompts.
    ///  - Danger: while a watcher can see what you're doing, the phone shakes and turns red, the screen edges
    ///    go red and the music turns sour. Full = a warning. Three warnings, never shown as numbers.
    ///  - Warning feedback: red "WARNING" flash, a low hit, and sudden silence.
    ///  - Chapter cards, fades, finishing the level.
    /// </summary>
    public abstract class LevelDirectorBase : MonoBehaviour
    {
        [Header("Core references (filled by the scene builder)")]
        public PlayerRig rig;
        public PhoneController phone;
        public NavMeshSurface navSurface;
        public Transform phoneExposurePoint;

        [Header("Rules")]
        public int chances = 3;
        [Tooltip("Seconds of being seen with the phone until he notices (phone fully red).")]
        public float noticeSeconds = 2.5f;
        public float dangerDecayPerSecond = 0.35f;
        public float graceAfterWarning = 3f;
        public float interactDistance = 1.8f;

        [Header("Subtitle timing (seconds)")]
        public float teacherLineSeconds = 7.5f;
        public float thoughtSeconds = 7.5f;
        public float gapBetweenLines = 1.4f;
        public float typeCharsPerSecond = 55f;
        public bool voiceBlips = true;

        protected enum Who { Authority, Student, Thought, PA }
        protected class Line { public Who who; public string speaker; public string text; public float seconds; public float gap; }

        protected int strikes;
        protected float danger, grace;
        protected bool ended;
        protected AmbientAudio ambient;
        protected DangerFX dangerFx;
        /// <summary>Label shown above the authority's lines ("TEACHER", "LUNCH MONITOR"...).</summary>
        protected string authorityLabel = "TEACHER";
        protected float authorityPitch = 0.8f;
        protected string Name { get { return GameFlow.playerName; } }

        readonly Queue<Line> queue = new Queue<Line>();
        Line current;
        float currentLeft, gapLeft, reveal;
        int revealedShown;
        string currentFull = "";

        // HUD
        protected Image fade, questionTimer;
        protected Text speechText, speechWho, thoughtText, prompt, caption, questionText, questionStatus;
        protected GameObject questionBox, speechBox, thoughtBox, promptBox;
        Image speechBg, speechAccent, paStripe;
        Text warningText;
        float warningT;
        GameObject chapterRoot;
        Text chapterLabelText, chapterTitleText, chapterTimeText, chapterSloganText;
        Image lidTop, lidBottom, lidDark;
        Text objectiveText, objectiveLabel;
        Image objectiveBg, objectiveDivider;
        RectTransform objectivePanel;
        float objectiveFlash;
        string actionShown = "";
        static readonly Color PanelBg = new Color(0.05f, 0.055f, 0.06f, 0.9f);
        static readonly Color Amber = new Color(1f, 0.8f, 0.45f);
        const float PanelW = 380f, PanelPad = 20f;
        float lids;

        static readonly Color SpeechBg = new Color(0.07f, 0.08f, 0.09f, 0.88f);
        static readonly Color SpeechInk = new Color(0.96f, 0.94f, 0.9f);
        static readonly Color Red = new Color(0.78f, 0.12f, 0.1f);

        // ------------------------------------------------------------------ lifecycle
        protected virtual void Awake()
        {
            RemoveOtherCameras();
            BuildHUD();
            ambient = AmbientAudio.Create(transform);
            dangerFx = DangerFX.Create(transform);
        }

        protected virtual void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (navSurface != null) navSurface.BuildNavMesh();
        }

        protected virtual void Update()
        {
            if (GameInput.EscDown()) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (GameInput.LeftDown() && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (GameInput.SkipDown() && !ended) { StopAllCoroutines(); StartCoroutine(FinishLevel("skip", 0.4f)); }
            TickDialogue();
            TickHUD();
            PlacePrompt();
            LevelAlways();
            if (ended)
            {
                if (phone != null) phone.danger = 0f;
                danger = Mathf.MoveTowards(danger, 0f, Time.deltaTime);
                ambient.dangerLevel = 0f;
                dangerFx.level = 0f;
                return;
            }
            LevelUpdate();
            ambient.dangerLevel = danger;
            dangerFx.level = danger;
        }

        /// <summary>Runs every frame, even after the level has ended.</summary>
        protected virtual void LevelAlways() { }

        /// <summary>Runs every frame while the level is being played.</summary>
        protected abstract void LevelUpdate();

        /// <summary>Third warning: what happens when they take the phone. Must end with GameFlow.EndLevel.</summary>
        protected abstract IEnumerator OnCaught();

        /// <summary>Gameplay is always seen through the player's eyes; any other camera (e.g. a FlyCamera) is removed.</summary>
        void RemoveOtherCameras()
        {
            Transform mine = rig != null ? rig.transform : transform;
            foreach (Camera c in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.transform.IsChildOf(mine)) continue;
                GameObject root = c.gameObject;
                foreach (MonoBehaviour mb in c.GetComponentsInParent<MonoBehaviour>(true))
                    if (mb != null && mb.GetType().Name == "FlyCameraController") { root = mb.gameObject; break; }
                root.SetActive(false);
                Destroy(root);
            }
            foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!l.transform.IsChildOf(mine)) Destroy(l);
            if (rig != null && rig.eye != null)
            {
                Camera own = rig.eye.GetComponent<Camera>();
                if (own != null) { own.enabled = true; own.gameObject.tag = "MainCamera"; }
            }
        }

        // ------------------------------------------------------------------ dialogue: one line at a time
        protected void Say(Who who, string speaker, string text, float seconds, bool interrupt = false, float gap = -1f)
        {
            Line l = new Line { who = who, speaker = speaker, text = text, seconds = seconds, gap = gap >= 0f ? gap : gapBetweenLines };
            if (interrupt) { queue.Clear(); Show(l); }
            else queue.Enqueue(l);
        }

        protected void Authority(string text, float seconds = -1f, bool interrupt = false, float gap = -1f)
        {
            Say(Who.Authority, authorityLabel, text, seconds > 0f ? seconds : teacherLineSeconds, interrupt, gap);
        }

        protected void Thought(string text, float seconds = -1f, bool interrupt = false, float gap = -1f)
        {
            Say(Who.Thought, "", text, seconds > 0f ? seconds : thoughtSeconds, interrupt, gap);
        }

        protected void PA(string text, float seconds = -1f, bool interrupt = false, float gap = -1f)
        {
            Say(Who.PA, "PA SYSTEM", text, seconds > 0f ? seconds : teacherLineSeconds, interrupt, gap);
        }

        protected void StudentSay(string speaker, string text, float seconds, bool interrupt = false, float gap = -1f)
        {
            Say(Who.Student, speaker, text, seconds, interrupt, gap);
        }

        void Show(Line l)
        {
            current = l;
            currentLeft = l.seconds;
            currentFull = l.text ?? "";
            reveal = 0f;
            revealedShown = 0;
            bool speech = l.who != Who.Thought;
            speechBox.SetActive(speech);
            thoughtBox.SetActive(!speech);
            if (speech)
            {
                // PA announcements use the same dark subtitle as everyone else (a white box read like a phone notification)
                bool pa = l.who == Who.PA;
                speechBg.color = SpeechBg;
                speechAccent.color = pa ? new Color(0.6f, 0.62f, 0.66f) : Red;
                paStripe.enabled = false;
                speechWho.text = pa ? "LOUDSPEAKER  (school announcement)" : (l.speaker ?? "").ToUpper();
                speechWho.color = pa ? new Color(0.72f, 0.74f, 0.78f) : new Color(1f, 0.45f, 0.38f);
                speechText.color = SpeechInk;
                speechText.text = "";
                if (pa) ambient.Play(AudioSynth.Chime(), 0.35f);
            }
            else thoughtText.text = "";
        }

        protected void ClearLine()
        {
            gapLeft = current != null ? current.gap : gapBetweenLines;
            current = null;
            speechBox.SetActive(false);
            thoughtBox.SetActive(false);
        }

        void TickDialogue()
        {
            if (current != null)
            {
                int total = RichText.VisibleLength(currentFull);
                if (revealedShown < total)
                {
                    // PA lines wait for the chime before they start typing
                    float delay = current.who == Who.PA ? 0.6f : 0f;
                    reveal += Time.deltaTime * typeCharsPerSecond;
                    int shown = Mathf.Clamp(Mathf.FloorToInt(reveal - delay * typeCharsPerSecond), 0, total);
                    if (shown != revealedShown)
                    {
                        for (int i = revealedShown; i < shown; i++) Voice(current, i);
                        revealedShown = shown;
                        string s = shown >= total ? currentFull : RichText.Reveal(currentFull, shown);
                        if (current.who == Who.Thought) thoughtText.text = s; else speechText.text = s;
                    }
                }
                currentLeft -= Time.deltaTime;
                if (currentLeft <= 0f) ClearLine();
                return;
            }
            if (gapLeft > 0f) { gapLeft -= Time.deltaTime; return; }
            if (queue.Count > 0) Show(queue.Dequeue());
        }

        void Voice(Line l, int index)
        {
            if (!voiceBlips) return;
            char ch = RichText.VisibleChar(currentFull, index);
            if (!char.IsLetter(ch)) return;
            if (l.who == Who.Thought)
            {
                if (index % 3 == 0) ambient.Play(AudioSynth.Tick(), 0.08f, Random.Range(0.9f, 1.1f));
                return;
            }
            if (index % 3 != 0) return;
            float p = l.who == Who.Authority ? authorityPitch : (l.who == Who.PA ? 1.25f : 1.35f);
            ambient.Play(AudioSynth.Blip(), l.who == Who.PA ? 0.12f : 0.17f, p * Random.Range(0.93f, 1.07f));
        }

        protected bool DialogueIdle { get { return current == null && queue.Count == 0 && gapLeft <= 0f; } }

        protected IEnumerator WaitIdle() { while (!DialogueIdle) yield return null; }

        /// <summary>When something else should keep a reminder quiet (questions, roll call...).</summary>
        protected virtual bool ReminderBlocked() { return false; }

        /// <summary>Repeats a thought every 'every' seconds (only when nothing else is being said) until 'done'.</summary>
        protected IEnumerator Reminder(string text, System.Func<bool> done, float every)
        {
            while (!ended && !done())
            {
                while (!ended && !done() && (!DialogueIdle || ReminderBlocked())) yield return null;
                if (ended || done()) yield break;
                Thought(text);
                float t = 0f;
                while (t < every && !done()) { t += Time.deltaTime; yield return null; }
            }
        }

        /// <summary>Rules in the player's thoughts are highlighted so they stand out.</summary>
        protected static string Rule(string s) { return "<b><color=#ff9a6e>" + s + "</color></b>"; }

        // ------------------------------------------------------------------ interaction
        /// <summary>Raycast from the eye; returns the prompt to show (or null) and uses the thing on [E].</summary>
        protected string RaycastInteract()
        {
            if (rig == null || rig.eye == null) return null;
            if (phone != null && phone.IsExposed) return null;
            RaycastHit[] hits = Physics.RaycastAll(rig.eye.position, rig.eye.forward, interactDistance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(rig.transform)) continue;
                Interactable it = hit.collider.GetComponentInParent<Interactable>();
                if (it != null && it.active && it.onUse != null)
                {
                    if (GameInput.InteractDown()) it.onUse();
                    return "[E]  " + it.prompt;
                }
                if (!hit.collider.isTrigger) return null;   // something solid is in front
            }
            return null;
        }

        /// <summary>
        /// What you can do right now ([E] ..., phone hints): under the crosshair. While the phone is up the box
        /// slides to the right of the phone, so it never covers the screen.
        /// </summary>
        protected void SetPrompt(string p)
        {
            string t = p ?? "";
            if (t == actionShown) return;
            actionShown = t;
            prompt.text = t;
            promptBox.SetActive(t.Length > 0);
            if (t.Length > 0)
            {
                float w = Mathf.Clamp(prompt.preferredWidth + 48f, 260f, 640f);
                RectTransform rt = (RectTransform)promptBox.transform;
                rt.sizeDelta = new Vector2(w, 10f);
                rt.sizeDelta = new Vector2(w, Mathf.Ceil(prompt.preferredHeight) + 22f);
            }
        }

        void PlacePrompt()
        {
            if (promptBox == null || !promptBox.activeSelf) return;
            RectTransform rt = (RectTransform)promptBox.transform;
            bool phoneUp = phone != null && phone.IsExposed;
            Vector2 want = phoneUp ? new Vector2(270f + rt.sizeDelta.x * 0.5f, -150f) : new Vector2(0f, -90f);
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, want, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
        }

        /// <summary>Sizes the panel to its text, so nothing ever runs outside the background.</summary>
        void LayoutPanel()
        {
            if (objectivePanel == null) return;
            bool hasObj = !string.IsNullOrEmpty(objectiveText.text);
            objectiveLabel.gameObject.SetActive(hasObj);
            objectiveDivider.gameObject.SetActive(hasObj);
            objectiveText.gameObject.SetActive(hasObj);
            float y = 12f;
            if (hasObj)
            {
                y = 46f;
                float oh = Mathf.Ceil(objectiveText.preferredHeight) + 2f;
                objectiveText.rectTransform.anchoredPosition = new Vector2(PanelPad, -y);
                objectiveText.rectTransform.sizeDelta = new Vector2(PanelW - PanelPad * 2f, oh);
                y += oh + 12f;
            }
            objectivePanel.sizeDelta = new Vector2(PanelW, y);
            objectivePanel.gameObject.SetActive(hasObj);
        }

        // ------------------------------------------------------------------ danger & warnings
        /// <summary>
        /// rate = how fast he notices (1/seconds); 0 = nobody is looking (it drains back).
        /// Returns true when the danger is full: a warning must be given.
        /// </summary>
        protected bool AccumulateDanger(float rate)
        {
            if (grace > 0f) grace -= Time.deltaTime;
            if (rate > 0f && grace <= 0f) danger += Time.deltaTime * rate;
            else danger -= Time.deltaTime * dangerDecayPerSecond;
            danger = Mathf.Clamp01(danger);
            if (phone != null) phone.danger = phone.IsExposed ? danger : 0f;
            if (danger >= 1f)
            {
                danger = 0f;
                grace = graceAfterWarning;
                if (phone != null) phone.Flash();
                return true;
            }
            return false;
        }

        /// <summary>The warning count spoken after the line (never shown as a number on screen).</summary>
        protected virtual string StrikeSuffix(int n, bool pa)
        {
            if (pa) return n == 1 ? "  This is your first reminder. You will receive three." : "  This is your second reminder. There is no fourth reminder.";
            return n == 1 ? "  That's one warning. You get three." : "  That's two. One more, and whatever you're hiding is mine.";
        }

        /// <summary>One of the three chances is used. Only the voices tell the player.</summary>
        protected void Strike(string line, bool pa = false)
        {
            if (ended) return;
            strikes++;
            warningT = 1.6f;
            dangerFx.Flash();
            ambient.Play(AudioSynth.Stinger(), 0.7f);
            ambient.Duck(1.6f);
            if (strikes >= chances) { ended = true; StartCoroutine(OnCaught()); return; }
            if (pa) PA(line + StrikeSuffix(strikes, true), 7.5f, true);
            else Authority(line + StrikeSuffix(strikes, false), 7f, true);
        }

        // ------------------------------------------------------------------ cards, fades, endings
        protected IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Color c = fade.color; c.a = Mathf.Lerp(from, to, t / seconds); fade.color = c;
                yield return null;
            }
            Color e = fade.color; e.a = to; fade.color = e;
        }

        /// <summary>Black card with typed chapter info, then fades into the level. Any key skips the wait.</summary>
        protected IEnumerator ChapterCard(string label, string title, string time, float hold = 2.2f)
        {
            Color fc = fade.color; fc.a = 1f; fade.color = fc;
            chapterRoot.SetActive(true);
            chapterLabelText.text = ""; chapterTitleText.text = ""; chapterTimeText.text = ""; chapterSloganText.text = "";
            yield return TypeInto(chapterLabelText, label, 30f);
            yield return TypeInto(chapterTitleText, title, 18f);
            yield return TypeInto(chapterTimeText, time, 30f);
            chapterSloganText.text = "CLEARVIEW ACADEMY  -  WHAT IS SEEN, IS SAFE.";
            float t = 0f;
            while (t < hold && !GameInput.AnyKeyDown()) { t += Time.deltaTime; yield return null; }
            chapterRoot.SetActive(false);
            yield return Fade(1f, 0f, 1.2f);
        }

        IEnumerator TypeInto(Text t, string s, float cps)
        {
            for (int i = 1; i <= s.Length; i++)
            {
                t.text = s.Substring(0, i);
                if (char.IsLetterOrDigit(s[i - 1])) ambient.Play(AudioSynth.Tick(), 0.12f, Random.Range(0.9f, 1.1f));
                float w = 1f / cps;
                while (w > 0f) { w -= Time.deltaTime; yield return null; }
            }
        }

        /// <summary>Fade out and report a win to the game flow.</summary>
        protected IEnumerator FinishLevel(string reason, float fadeSeconds = 1.4f)
        {
            ended = true;
            SetObjective("");
            yield return Fade(0f, 1f, fadeSeconds);
            GameFlow.EndLevel(LessonResult.Win, reason);
        }

        protected IEnumerator Caught(string reason, float fadeSeconds = 1.2f)
        {
            ended = true;
            SetObjective("");
            yield return Fade(0f, 1f, fadeSeconds);
            GameFlow.EndLevel(LessonResult.Caught, reason);
        }

        /// <summary>
        /// The current goal, always visible top-left (what to do + where). Changing it flashes the box and chimes,
        /// so the player never has to guess the next step. null/empty hides it.
        /// </summary>
        protected void SetObjective(string text)
        {
            if (objectiveText == null) return;
            string t = text ?? "";
            if (objectiveText.text == t) return;
            objectiveText.text = t;
            LayoutPanel();
            if (t.Length > 0) { objectiveFlash = 1.2f; ambient.Play(AudioSynth.Confirm(), 0.18f, 1.3f); }
        }

        /// <summary>Buzz in your pocket (phone notification).</summary>
        protected void PhoneBuzz()
        {
            ambient.Play(AudioSynth.Vibrate(), 0.5f);
        }

        /// <summary>Eyelids: 0 = open, 1 = shut (lying down in bed).</summary>
        protected void SetLids(float v) { lids = Mathf.Clamp01(v); }

        protected void ShowQuestion(string text)
        {
            questionText.text = text;
            questionStatus.text = "";
            questionTimer.rectTransform.anchorMax = Vector2.one;
            questionBox.SetActive(true);
        }

        // ------------------------------------------------------------------ HUD
        void TickHUD()
        {
            if (warningT > 0f)
            {
                warningT -= Time.unscaledDeltaTime;
                bool on = Mathf.Repeat(warningT, 0.4f) > 0.15f;
                warningText.enabled = warningT > 0f && on;
            }
            else warningText.enabled = false;

            if (objectiveFlash > 0f)
            {
                objectiveFlash -= Time.unscaledDeltaTime;
                bool on = Mathf.Repeat(objectiveFlash, 0.3f) > 0.12f;
                objectiveBg.color = on ? new Color(0.42f, 0.09f, 0.07f, 0.94f) : PanelBg;
            }
            else if (objectiveBg != null) objectiveBg.color = PanelBg;

            float h = Mathf.Lerp(0f, 560f, lids);
            lidTop.rectTransform.sizeDelta = new Vector2(0f, h);
            lidBottom.rectTransform.sizeDelta = new Vector2(0f, h);
            lidDark.color = new Color(0f, 0f, 0f, lids * 0.55f);
        }

        void BuildHUD()
        {
            Canvas canvas = UIFactory.MakeCanvas("HUD", 40);   // above the phone, so subtitles are never covered
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            // eyelids (lying in bed)
            lidDark = UIFactory.AddImage(UIFactory.Stretch("LidDark", root), new Color(0f, 0f, 0f, 0f));
            lidTop = UIFactory.AddImage(UIFactory.Rect("LidTop", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero), Color.black);
            lidBottom = UIFactory.AddImage(UIFactory.Rect("LidBottom", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero), Color.black);

            UIFactory.AddImage(UIFactory.Centered("Crosshair", root, 0f, 0f, 6f, 6f), new Color(1f, 1f, 1f, 0.75f));

            // OBJECTIVE + what you can do now - top-left panel that grows with its text
            objectivePanel = UIFactory.Rect("Objective", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -170f), new Vector2(PanelW, 100f));
            objectiveBg = UIFactory.AddImage(objectivePanel, PanelBg);
            UIFactory.AddImage(UIFactory.Rect("Bar", objectivePanel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f)), Amber);
            objectiveLabel = UIFactory.AddText(UIFactory.TopLeft("Label", objectivePanel, PanelPad, 12f, PanelW - 2f * PanelPad, 22f), "OBJECTIVE", 16, Amber, TextAnchor.UpperLeft);
            objectiveLabel.fontStyle = FontStyle.Bold;
            objectiveDivider = UIFactory.AddImage(UIFactory.TopLeft("Divider", objectivePanel, PanelPad, 37f, PanelW - 2f * PanelPad, 1f), new Color(1f, 0.8f, 0.45f, 0.35f));
            objectiveText = UIFactory.AddText(UIFactory.TopLeft("Text", objectivePanel, PanelPad, 46f, PanelW - 2f * PanelPad, 40f), "", 19, new Color(0.95f, 0.94f, 0.9f), TextAnchor.UpperLeft);
            objectiveText.lineSpacing = 1.1f;
            // interaction / keyboard prompt - under the crosshair (moves beside the phone while it is up)
            RectTransform pb = UIFactory.Centered("Prompt", root, 0f, -90f, 420f, 52f);
            UIFactory.AddImage(pb, new Color(0.05f, 0.055f, 0.06f, 0.82f));
            UIFactory.AddImage(UIFactory.Rect("Bar", pb, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f)), Amber);
            prompt = UIFactory.AddText(UIFactory.Rect("Text", pb, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(2f, 0f), new Vector2(-28f, -12f)), "", 22, Color.white, TextAnchor.MiddleCenter);
            prompt.fontStyle = FontStyle.Bold;
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            promptBox = pb.gameObject;
            promptBox.SetActive(false);
            objectivePanel.gameObject.SetActive(false);

            // SPEECH / PA - top
            RectTransform sb = UIFactory.Rect("Speech", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(1500f, 118f));
            speechBg = UIFactory.AddImage(sb, SpeechBg);
            speechAccent = UIFactory.AddImage(UIFactory.Rect("Accent", sb, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(10f, 0f)), Red);
            paStripe = UIFactory.AddImage(UIFactory.Rect("PaStripe", sb, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f)), new Color(0.62f, 0.85f, 0.76f));
            speechWho = UIFactory.AddText(UIFactory.TopLeft("Who", sb, 30f, 10f, 1200f, 30f), "", 21, new Color(1f, 0.45f, 0.38f), TextAnchor.UpperLeft);
            speechWho.fontStyle = FontStyle.Bold;
            speechText = UIFactory.AddText(UIFactory.Rect("Text", sb, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(6f, -12f), new Vector2(-60f, -44f)), "", 28, SpeechInk, TextAnchor.MiddleCenter);
            speechBox = sb.gameObject;
            speechBox.SetActive(false);

            // WARNING flash
            warningText = UIFactory.AddText(UIFactory.Rect("Warning", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(900f, 90f)), "WARNING", 72, new Color(1f, 0.12f, 0.08f), TextAnchor.MiddleCenter);
            warningText.fontStyle = FontStyle.Bold;
            UIFactory.AddShadow(warningText, new Color(0f, 0f, 0f, 0.9f), 3f);
            warningText.enabled = false;

            // PLAYER thoughts / instructions - bottom left (the phone sits bottom centre)
            RectTransform tb = UIFactory.Rect("Thoughts", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(660f, 160f));
            UIFactory.AddImage(tb, new Color(0.08f, 0.06f, 0.02f, 0.82f));
            UIFactory.AddImage(UIFactory.Rect("Accent", tb, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f)), new Color(1f, 0.8f, 0.45f, 0.8f));
            thoughtText = UIFactory.AddText(UIFactory.Rect("Text", tb, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, -26f)), "", 26, new Color(1f, 0.86f, 0.5f), TextAnchor.MiddleLeft);
            thoughtText.fontStyle = FontStyle.Italic;
            thoughtBox = tb.gameObject;
            thoughtBox.SetActive(false);

            // question / roll-call answer / choices - left middle
            RectTransform qb = UIFactory.Rect("Question", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, -70f), new Vector2(640f, 310f));
            UIFactory.AddImage(qb, new Color(0.05f, 0.05f, 0.06f, 0.92f));
            UIFactory.AddImage(UIFactory.Rect("Bar", qb, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f)), Red);
            questionText = UIFactory.AddText(UIFactory.TopLeft("Q", qb, 24f, 22f, 592f, 200f), "", 26, Color.white, TextAnchor.UpperLeft);
            questionStatus = UIFactory.AddText(UIFactory.TopLeft("Status", qb, 24f, 236f, 592f, 40f), "", 22, new Color(1f, 0.7f, 0.4f), TextAnchor.UpperLeft);
            questionStatus.fontStyle = FontStyle.Bold;
            RectTransform tbar = UIFactory.TopLeft("TimerBG", qb, 24f, 284f, 592f, 10f);
            UIFactory.AddImage(tbar, new Color(1f, 1f, 1f, 0.15f));
            questionTimer = UIFactory.AddImage(UIFactory.Rect("Timer", tbar, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero), new Color(1f, 0.4f, 0.3f));
            questionBox = qb.gameObject;
            questionBox.SetActive(false);

            fade = UIFactory.AddImage(UIFactory.Stretch("Fade", root), Color.black);
            caption = UIFactory.AddText(UIFactory.Centered("Caption", root, 0f, 0f, 1400f, 100f), "", 40, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter);

            // chapter card (on top of the fade)
            RectTransform cr = UIFactory.Stretch("ChapterCard", root);
            chapterRoot = cr.gameObject;
            chapterLabelText = UIFactory.AddText(UIFactory.Centered("Label", cr, 0f, 120f, 1400f, 50f), "", 28, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter);
            chapterTitleText = UIFactory.AddText(UIFactory.Centered("Title", cr, 0f, 30f, 1600f, 120f), "", 92, new Color(0.94f, 0.92f, 0.86f), TextAnchor.MiddleCenter);
            chapterTitleText.fontStyle = FontStyle.Bold;
            chapterTimeText = UIFactory.AddText(UIFactory.Centered("Time", cr, 0f, -70f, 1400f, 50f), "", 30, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            chapterSloganText = UIFactory.AddText(UIFactory.Rect("Slogan", cr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1400f, 40f)), "", 20, new Color(0.75f, 0.2f, 0.18f), TextAnchor.MiddleCenter);
            chapterRoot.SetActive(false);
        }
    }
}
