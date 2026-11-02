// @role The hidden phone: hold to raise, passcodes, app loading, Photos, Messages, Contacts, forward/reply tasks, notifications, danger shake
// @calls AudioSynth
// @tune bootSeconds:float:1-10:start-up time (lowering the phone resets it)
// @tune raiseSeconds:float:0.1-1:time to raise the phone
// @tune readCharsPerSecond:float:5-60:message reading speed (only while the phone is up)
// @tune glowIntensity:float:0-4:how much the screen lights your face (and gives you away at night)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    [System.Serializable]
    public class PhoneContact
    {
        public string name;
        public string year;
        [TextArea] public string note;
        public PhoneContact(string n, string y, string no) { name = n; year = y; note = no; }
    }

    public class PhoneMsg
    {
        public string from;
        public string text;
        public bool mine;
        public float shown;   // characters read so far
    }

    /// <summary>Something the level wants the player to do in Messages: reply, or forward to a contact.</summary>
    public class PhoneTask
    {
        public bool forward;
        public string header = "Reply";
        public string[] options;
        public int correct = -1;
        public string wrongText = "Not them. They're not part of this.";
        public string sendText;
        public string toLabel;
        /// <summary>Returns null if the choice can be sent now, or a reason it can't ("No signal...").</summary>
        public System.Func<int, string> check;
        public System.Action<int> onDone;
        public float typeSeconds = 3f;
    }

    /// <summary>
    /// The inherited phone (screen-space UI).
    ///   Hold RIGHT MOUSE = phone up.  Release = put it down (hide).
    ///   LOCK SCREEN  : 4-digit code with the number keys. Backspace deletes. (1952 - the year it all began)
    ///   STARTING UP  : old phone, bootSeconds (5 s). Keep it up the WHOLE time - put it down and it locks again.
    ///   HOME         : Photos, Camera, Contacts, Messages, Search. A / D choose, Left Mouse / Enter open.
    ///                  Every app has its own code (heard somewhere in the school) and its own loading time,
    ///                  with the same rule: put the phone down while it loads and it locks again.
    ///   PHOTOS       : A / D browse, Q back.
    ///   MESSAGES     : new messages are read only while you look at them. Replies / forwards: A / D choose,
    ///                  Left Mouse select, then HOLD Left Mouse to type and send.
    ///   CONTACTS     : A / D browse names left by everyone who had this phone before you.
    ///   CAMERA       : the screen becomes a viewfinder (you see the room through it). Aim the middle of the screen,
    ///                  Left Mouse = shutter (a click anyone close can hear), A / D = flash on / off. Photos go to PHOTOS.
    ///   SEARCH       : the school's catalogue. A / D choose a word you have READ somewhere, HOLD Left Mouse to search.
    ///   SILENT       : the little bell top right. M switches it (only from The Archive on; before, nobody explains it).
    /// </summary>
    public class PhoneController : MonoBehaviour
    {
        public enum PhoneScreen { Lock, Booting, Home, AppLock, AppLoading, Photos, Messages, Contacts, Camera, Search }
        public const int AppPhotos = 0, AppCamera = 1, AppContacts = 2, AppMessages = 3, AppSearch = 4;

        [Header("Codes & timing")]
        public string phoneCode = "1952";
        public string phoneHint = "Hint: the year it all began";
        [TextArea] public string lockNotification = "1 new message\nUnknown: congrats on the worst seat in the whole school :)";
        public float bootSeconds = 5f;
        public float raiseSeconds = 0.25f;
        public float lowerSeconds = 0.3f;
        public string[] appNames = { "Photos", "Camera", "Contacts", "Messages", "Search" };
        public string[] appCodes = { "2100", "1045", "0304", "1240", "0467" };
        public string[] appHints =
        {
            "Hint: lights out (24-hour clock)",
            "Hint: when your class is photographed",
            "Hint: where you sleep (room number)",
            "Hint: when we eat (24-hour clock)",
            "Hint: the stamp in the oldest book (month + year)",
        };

        /// <summary>
        /// One app per chapter. The home screen lists them in the order they are unlocked, and an app's code hint
        /// stays "???" until you reach the chapter where it can be opened.
        /// </summary>
        public static readonly int[] HomeOrder = { AppPhotos, AppMessages, AppContacts, AppCamera, AppSearch };
        static readonly string[] UnlockScene = { "Level01_Lesson", "Level04_PictureDay", "Level03_LightsOut", "Level02_Lunch", "Level05_Archive" };
        public float[] appLoadSeconds = { 7f, 5f, 5f, 5f, 5f };
        public bool[] appUnlocked = new bool[5];

        [Header("Phone look")]
        public string phoneClock = "10:05";

        [Header("Messages")]
        public float readCharsPerSecond = 22f;
        public List<PhoneContact> contacts = DefaultContacts();

        [Header("Photos")]
        [Tooltip("Shows the 1979 photo (The Inspector).")]
        public bool showOldPhoto;

        [Header("Camera (Picture Day)")]
        [Tooltip("The flash starts ON - everyone sees a flash.")]
        public bool cameraFlash = true;
        [Tooltip("Picture Day: the flash switches itself back ON every time the phone comes out.")]
        public bool flashOnEveryRaise;
        [Tooltip("Called on every shutter press: the target in frame (or null) and whether the flash fired.")]
        public System.Action<PhotoTarget, bool> onShutter;

        [Header("Silent mode (the little bell on the screen; only explained in The Archive)")]
        [Tooltip("The M key works (Chapter 5). Before that the bell is just a small icon nobody explains.")]
        public bool silentAvailable;
        public bool silent;
        [Tooltip("The old switch is unreliable: each time the phone comes out, silent mode may quietly switch itself off.")]
        [Range(0f, 1f)] public float silentFailChance;
        /// <summary>True when the last raise quietly switched silent mode off (the level reads and clears it).</summary>
        [HideInInspector] public bool silentJustFailed;
        public System.Action<bool> onSilentChanged;

        [Header("Search (The Archive)")]
        [Tooltip("Words the player has read somewhere (posters, spines, stamps). Search only knows these.")]
        public List<string> searchWords = new List<string>();
        public float searchSeconds = 1.4f;
        /// <summary>Given a word (English), returns the result text to show. Set by the level.</summary>
        public System.Func<string, string> searchFunc;
        public System.Action<string> onSearched;

        [Header("Screen glow (Lights Out)")]
        public Light glowLight;
        public float glowIntensity = 1.6f;
        [HideInInspector] public float glowBoost;

        [Header("State (set by the level)")]
        public bool hasPhone;
        public bool allowRaise;
        [Tooltip("The level holds the phone up for you (the ending).")]
        public bool forceRaise;
        [Tooltip("0..1 - how close the watcher is to noticing. The phone shakes and turns red.")]
        public float danger;

        public static List<PhoneContact> DefaultContacts()
        {
            return new List<PhoneContact>
            {
                new PhoneContact("Seat 31 · '79", "1979", "\"If you are reading this, keep going.\"\n(last seen: gave it back, 1980)"),
                new PhoneContact("Mom (not mine)", "2009", "Do not call. It rings in the front office."),
                new PhoneContact("Don't Reply", "2016", "Seriously. Don't."),
                new PhoneContact("Lent Me An Eraser", "2018", "Still owes me an eraser."),
                new PhoneContact("Bus 14", "2019", "Left in spring. Didn't say goodbye."),
                new PhoneContact("Triangle", "2021", "The one with the chalk triangle. One of us."),
                new PhoneContact("Hums When Scared", "2021", "Window tables. Lunch."),
                new PhoneContact("Never Blinks", "2022", "Room 304. Across from you."),
            };
        }

        public PhoneScreen Screen { get; private set; }
        public bool IsExposed { get { return raise > 0.001f; } }
        public bool IsUp { get { return raise >= 0.999f; } }
        public bool IsHeld { get { return held; } }
        /// <summary>Unlocked and past the start-up screen.</summary>
        public bool ScreenOn { get { return Screen != PhoneScreen.Lock && Screen != PhoneScreen.Booting; } }
        public bool ClueFound { get; private set; }
        public bool OldPhotoFound { get; private set; }
        public bool FirstContactSeen { get; private set; }
        public bool RaisedOnce { get; private set; }
        public int PhotosTaken { get { return taken.Count; } }
        public PhotoTarget Aimed { get { return aimed; } }
        public bool LastSearchDone { get; private set; }

        /// <summary>Adds a word the Search app can look up. Returns false if it was already known.</summary>
        public bool AddSearchWord(string w)
        {
            if (string.IsNullOrEmpty(w) || searchWords.Contains(w)) return false;
            searchWords.Add(w);
            return true;
        }

        public float BootSecondsElapsed { get { return Screen == PhoneScreen.Booting ? loadT : 0f; } }
        public float AppLoadingElapsed { get { return Screen == PhoneScreen.AppLoading ? loadT : 0f; } }
        public PhoneTask Task { get { return task; } }
        public int OpenApp { get { return openApp; } }
        public bool IsUnlocked(int app) { return app >= 0 && app < appUnlocked.Length && appUnlocked[app]; }

        /// <summary>Has the player reached the chapter where this app's code can be worked out?</summary>
        public bool HintAvailable(int app)
        {
            if (app < 0 || app >= UnlockScene.Length) return false;
            int need = GameFlow.LevelIndex(UnlockScene[app]);
            int now = GameFlow.LevelIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            return need >= 0 && now >= need;
        }

        string HintFor(int app)
        {
            if (!HintAvailable(app) || app >= appHints.Length) return Loc.L("Hint: ???", "提示：???");
            return Loc.T(appHints[app]);
        }

        public static string AppName(int app)
        {
            switch (app)
            {
                case AppPhotos: return Loc.L("Photos", "相册");
                case AppCamera: return Loc.L("Camera", "相机");
                case AppContacts: return Loc.L("Contacts", "通讯录");
                case AppMessages: return Loc.L("Messages", "信息");
                default: return Loc.L("Search", "搜索");
            }
        }
        public System.Action onMessageArrived;

        public int UnreadCount
        {
            get
            {
                int n = 0;
                foreach (PhoneMsg m in msgs) if (!m.mine && m.shown < m.text.Length) n++;
                return n;
            }
        }

        float raise, loadT, noticeTimer, viewTimer, typeProgress;
        bool held, typing;
        string entry = "";
        string notice = "";
        int selectedApp, openApp, photoIndex, contactIndex, choice;
        readonly List<PhoneMsg> msgs = new List<PhoneMsg>();
        PhoneTask task;
        // camera + search
        readonly List<string> taken = new List<string>();
        PhotoTarget[] targets;
        PhotoTarget aimed;
        float shutterFlash, searchProgress;
        bool searching;
        int searchIndex;
        string searchResult = "";
        AudioSource sfx;
        GameObject cameraRoot, searchRoot, snapshot;
        Text camLabel, camFlashText, camCount, snapLabel, searchQuery, searchResultText, searchHeader;
        RectTransform searchFill, screenRT;
        GameObject searchBarRoot;
        Image screenBg, crackImg, panelBg, whiteFlash;
        Image[] reticle;
        Image bellBody, bellClapper, bellSlash;
        Text bellText;

        // UI
        RectTransform panel;
        GameObject lockRoot, loadRoot, homeRoot, photosRoot, msgRoot, contactsRoot;
        Text lockTitle, lockDots, loadTitle, loadPercent, homeClock, photoCaption, photoCounter, footer;
        Text msgHeader, taskHeader, taskBody, taskHelp, contactName, contactYear, contactNote, contactCounter;
        RectTransform loadFill, typeFill;
        GameObject taskRoot, typeBarRoot;
        Image[] appTiles;
        Text[] appLabels;
        GameObject[] photos;
        Image[] bubbleBg;
        Text[] bubbleFrom, bubbleText;
        const int Bubbles = 4;
        // new-message notification that peeks up from the bottom of the screen while the phone is down
        RectTransform notif;
        Text notifFrom, notifText, notifCount;
        float notifT;
        const float NotifHidden = -140f, NotifShown = 24f;
        const float HiddenY = -950f, ShownY = -40f;   // low on the screen, partly "under the desk"
        const float PanelScale = 1.1f;   // applied through the canvas scale (not a stretched transform) so text stays sharp
        Image dangerTint;
        float flashT;
        Text lockHint, lockNote;
        GameObject lockNoteRoot;
        static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f);
        static readonly Color ScreenColor = new Color(0.95f, 0.91f, 0.82f);

        // Photos and their captions are defined together here (in code), so a picture and its caption
        // can never get out of step. Index 3 (the chalk triangle) is the clue for the student in front.
        const int cluePhoto = 3;
        const int oldPhoto = 5;
        static readonly string[] photoCaptions =
        {
            "2009. Back row, by the window.\nSame seat. (Nice haircut, me.)",
            "2016. \"Keep the screen low.\nThey watch the light.\"",
            "2018. Proof that he sneezes like a cat.\nDelete before he sees.",
            "2021. The one in front with the triangle\nis one of us. Tap their shoulder twice.",
            "Lights out. 21:00. Exactly.",
            "1979. Back row, by the window. Same seat.\nIn pencil, under the photo: VOSS.",
        };
        int BasePhotos { get { return showOldPhoto ? 6 : 5; } }
        int PhotoCount { get { return BasePhotos + taken.Count; } }

        Canvas phoneCanvas;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            BuildUI();
            Screen = PhoneScreen.Lock;
            SharpenText();
        }

        /// <summary>
        /// Phone screens use a clean sans-serif (the typewriter font stays for the school's own signs and subtitles):
        /// thin monospaced letters at small sizes looked blurry on the phone.
        /// </summary>
        void SharpenText()
        {
            if (phoneCanvas == null) return;
            Font sans = Loc.Chinese ? UIFactory.CjkFont : UIFactory.DefaultFont;
            foreach (Text t in phoneCanvas.GetComponentsInChildren<Text>(true))
            {
                if (sans != null) t.font = sans;
                t.fontSize += 1;
            }
        }

        // ------------------------------------------------------------------ public API for levels
        public void Receive(string from, string text)
        {
            msgs.Add(new PhoneMsg { from = from, text = text, mine = false, shown = 0f });
            if (notifFrom != null && hasPhone)
            {
                notifFrom.text = Loc.T(from).ToUpper();
                int maxChars = Loc.Chinese ? 20 : 44;
                notifText.text = text.Length > maxChars ? text.Substring(0, maxChars - 2) + "..." : text;
                notifT = 4.5f;
            }
            if (onMessageArrived != null) onMessageArrived();
        }

        /// <summary>A message that is already read (history from earlier days).</summary>
        public void AddHistory(string from, string text, bool mine)
        {
            msgs.Add(new PhoneMsg { from = from, text = text, mine = mine, shown = text.Length });
        }

        public void StartTask(PhoneTask t)
        {
            task = t;
            choice = 0;
            typing = false;
            typeProgress = 0f;
        }

        public void CancelTask() { task = null; typing = false; }

        /// <summary>Skip the lock screen (cutscenes): the phone is on and showing an app.</summary>
        public void ForceOpen(int app)
        {
            hasPhone = true;
            if (app >= 0 && app < appUnlocked.Length) appUnlocked[app] = true;
            openApp = app;
            Screen = app == AppMessages ? PhoneScreen.Messages : (app == AppContacts ? PhoneScreen.Contacts : (app == AppPhotos ? PhoneScreen.Photos : PhoneScreen.Home));
            if (app == AppCamera) EnterCamera();
            if (app == AppSearch) Screen = PhoneScreen.Search;
        }

        /// <summary>A strike: the phone jolts and flashes red.</summary>
        public void Flash() { flashT = 1f; }

        // ------------------------------------------------------------------ update
        void Update()
        {
            bool wasDown = raise <= 0.001f;
            held = forceRaise || (hasPhone && allowRaise && GameInput.RightHeld());
            if (held && wasDown && flashOnEveryRaise) cameraFlash = true;   // the old phone forgets: flash back on
            if (held && wasDown && silent && silentFailChance > 0f && Random.value < silentFailChance) { silent = false; silentJustFailed = true; }   // no notice: only the bell shows it
            float speed = held ? 1f / Mathf.Max(0.01f, raiseSeconds) : 1f / Mathf.Max(0.01f, lowerSeconds);
            raise = Mathf.MoveTowards(raise, held ? 1f : 0f, speed * Time.deltaTime);
            if (raise > 0.5f) RaisedOnce = true;
            float e = raise * raise * (3f - 2f * raise);
            // danger: shake + red, stronger the closer he is to noticing
            if (flashT > 0f) flashT -= Time.deltaTime * 1.5f;
            float d = Mathf.Clamp01(danger);
            Vector2 shake = Random.insideUnitCircle * (d * d * 18f + Mathf.Max(0f, flashT) * 26f);
            panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(HiddenY, ShownY, e)) + shake;
            dangerTint.color = new Color(0.85f, 0.05f, 0.05f, Mathf.Max(d * 0.65f, Mathf.Max(0f, flashT) * 0.75f));

            if (glowLight != null)
            {
                if (glowBoost > 0f) glowBoost = Mathf.Max(0f, glowBoost - Time.deltaTime * 3f);
                glowLight.intensity = raise * glowIntensity * (1f + glowBoost * 3f);
                glowLight.enabled = raise > 0.01f;
            }

            // Leaving the phone during a loading screen resets it.
            if (!held && Screen == PhoneScreen.Booting) { Screen = PhoneScreen.Lock; entry = ""; Notice(Loc.L("The phone locked itself again.", "手机又锁上了。")); }
            if (!held && Screen == PhoneScreen.AppLoading) { Screen = PhoneScreen.AppLock; entry = ""; Notice(Loc.L(AppName(openApp) + " locked again.", AppName(openApp) + " 又锁上了。")); }

            if (notif != null)
            {
                if (notifT > 0f) notifT -= Time.deltaTime;
                int unread = UnreadCount;
                bool show = hasPhone && raise < 0.05f && (notifT > 0f || unread > 0);
                float target = show ? NotifShown : NotifHidden;
                Vector2 np = notif.anchoredPosition;
                np.y = Mathf.Lerp(np.y, target, 1f - Mathf.Exp(-Time.deltaTime * 10f));
                notif.anchoredPosition = np;
                notifCount.text = unread > 1 ? Loc.L(unread + " new messages", unread + " 条新信息") : (unread == 1 ? Loc.L("1 new message", "1 条新信息") : "");
                // a quick nudge when it arrives
                notif.localRotation = Quaternion.Euler(0f, 0f, notifT > 4.1f ? Mathf.Sin(Time.time * 60f) * 1.5f : 0f);
            }

            if (noticeTimer > 0f) noticeTimer -= Time.deltaTime;
            if (IsUp) Tick();
            Refresh();
        }

        void Tick()
        {
            float dt = GameSpeed.Dt;   // loading, reading, typing and searching all run at story speed
            if (silentAvailable && GameInput.MuteDown())
            {
                silent = !silent;
                Notice(silent ? Loc.L("Silent mode ON.", "静音模式：开。") : Loc.L("Silent mode OFF.", "静音模式：关。"));
                if (onSilentChanged != null) onSilentChanged(silent);
            }
            switch (Screen)
            {
                case PhoneScreen.Lock:
                case PhoneScreen.AppLock:
                    if (Screen == PhoneScreen.AppLock && GameInput.BackDown()) { Screen = PhoneScreen.Home; entry = ""; break; }
                    int dg = GameInput.DigitDown();
                    if (dg >= 0 && entry.Length < 4) entry += dg.ToString();
                    if (GameInput.BackspaceDown() && entry.Length > 0) entry = entry.Substring(0, entry.Length - 1);
                    if (entry.Length == 4) CheckCode();
                    break;

                case PhoneScreen.Booting:
                    loadT += dt;
                    if (loadT >= bootSeconds) { Screen = PhoneScreen.Home; selectedApp = 0; }
                    break;

                case PhoneScreen.Home:
                    if (GameInput.LeftKeyDown()) selectedApp = (selectedApp + HomeOrder.Length - 1) % HomeOrder.Length;
                    if (GameInput.RightKeyDown()) selectedApp = (selectedApp + 1) % HomeOrder.Length;
                    if (GameInput.LeftDown() || GameInput.EnterDown())
                    {
                        openApp = HomeOrder[selectedApp];   // selectedApp = position on the home screen
                        entry = "";
                        if (IsUnlocked(openApp)) OpenUnlockedApp();
                        else Screen = PhoneScreen.AppLock;
                    }
                    break;

                case PhoneScreen.AppLoading:
                    loadT += dt;
                    if (loadT >= appLoadSeconds[openApp])
                    {
                        appUnlocked[openApp] = true;
                        OpenUnlockedApp();
                    }
                    break;

                case PhoneScreen.Photos:
                    if (GameInput.LeftKeyDown()) { photoIndex = (photoIndex + PhotoCount - 1) % PhotoCount; viewTimer = 0f; }
                    if (GameInput.RightKeyDown()) { photoIndex = (photoIndex + 1) % PhotoCount; viewTimer = 0f; }
                    if (GameInput.BackDown()) Screen = PhoneScreen.Home;
                    viewTimer += dt;
                    if (viewTimer > 1.5f)
                    {
                        if (photoIndex == cluePhoto) ClueFound = true;
                        if (photoIndex == oldPhoto) OldPhotoFound = true;
                    }
                    break;

                case PhoneScreen.Contacts:
                    if (contacts.Count > 0)
                    {
                        if (GameInput.LeftKeyDown()) { contactIndex = (contactIndex + contacts.Count - 1) % contacts.Count; viewTimer = 0f; }
                        if (GameInput.RightKeyDown()) { contactIndex = (contactIndex + 1) % contacts.Count; viewTimer = 0f; }
                    }
                    if (GameInput.BackDown()) Screen = PhoneScreen.Home;
                    viewTimer += dt;
                    if (contactIndex == 0 && viewTimer > 1.5f) FirstContactSeen = true;
                    break;

                case PhoneScreen.Messages:
                    TickMessages(dt);
                    break;

                case PhoneScreen.Camera:
                    if (GameInput.BackDown()) { Screen = PhoneScreen.Home; break; }
                    if (GameInput.LeftKeyDown() || GameInput.RightKeyDown()) { cameraFlash = !cameraFlash; Notice(cameraFlash ? Loc.L("Flash ON.", "闪光灯：开。") : Loc.L("Flash OFF.", "闪光灯：关。")); }
                    if (GameInput.LeftDown()) Shutter();
                    break;

                case PhoneScreen.Search:
                    TickSearch(dt);
                    break;
            }
        }

        // ------------------------------------------------------------------ camera
        void EnterCamera()
        {
            Screen = PhoneScreen.Camera;
            targets = FindObjectsByType<PhotoTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        /// <summary>The ray through the middle of the phone's screen (the viewfinder).</summary>
        Ray AimRay()
        {
            Camera cam = Camera.main;
            if (cam == null || screenRT == null) return new Ray(transform.position, transform.forward);
            Vector3 world = screenRT.TransformPoint(screenRT.rect.center);
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, world);
            return cam.ScreenPointToRay(sp);
        }

        PhotoTarget FindAimed()
        {
            if (targets == null) return null;
            Ray ray = AimRay();
            PhotoTarget best = null;
            float bestA = 999f;
            foreach (PhotoTarget t in targets)
            {
                if (t == null || !t.isActiveAndEnabled) continue;
                Vector3 to = t.transform.position - ray.origin;
                float d = to.magnitude;
                if (d > t.range || d < 0.1f) continue;
                float a = Vector3.Angle(ray.direction, to);
                if (a > 9f || a >= bestA) continue;
                bool blocked = false;
                foreach (RaycastHit h in Physics.RaycastAll(ray.origin, to / d, d - 0.15f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.transform.IsChildOf(transform) || h.transform.IsChildOf(t.transform)) continue;
                    blocked = true;
                    break;
                }
                if (blocked) continue;
                best = t;
                bestA = a;
            }
            return best;
        }

        void Shutter()
        {
            sfx.PlayOneShot(AudioSynth.Click(), 0.9f);
            if (cameraFlash) { shutterFlash = 1f; glowBoost = 1f; }
            PhotoTarget t = aimed;
            if (t != null && !t.taken)
            {
                t.taken = true;
                taken.Add(t.label);
                Notice(Loc.L("Saved to Photos.", "已存入相册。"));
            }
            else if (t != null) Notice(Loc.L("Already have that one.", "这张已经拍过了。"));
            else Notice(Loc.L("Nothing worth keeping in frame.", "取景框里没有值得留下的东西。"));
            if (onShutter != null) onShutter(t, cameraFlash);
        }

        // ------------------------------------------------------------------ search
        void TickSearch(float dt)
        {
            if (GameInput.BackDown() && !searching) { Screen = PhoneScreen.Home; return; }
            int n = searchWords.Count;
            if (!searching)
            {
                if (n > 0)
                {
                    if (GameInput.LeftKeyDown()) { searchIndex = (searchIndex + n - 1) % n; }
                    if (GameInput.RightKeyDown()) { searchIndex = (searchIndex + 1) % n; }
                    if (GameInput.LeftDown()) { searching = true; searchProgress = 0f; searchResult = ""; LastSearchDone = false; }
                }
                return;
            }
            if (GameInput.LeftHeld()) searchProgress = Mathf.Min(1f, searchProgress + dt / Mathf.Max(0.1f, searchSeconds));
            if (searchProgress >= 1f)
            {
                searching = false;
                string w = searchWords[Mathf.Clamp(searchIndex, 0, n - 1)];
                searchResult = searchFunc != null ? searchFunc(w) : null;
                if (string.IsNullOrEmpty(searchResult)) searchResult = Loc.L("0 results.", "0 条结果。");
                LastSearchDone = true;
                sfx.PlayOneShot(AudioSynth.Ping(), 0.4f);
                if (onSearched != null) onSearched(w);
            }
        }

        void OpenUnlockedApp()
        {
            viewTimer = 0f;
            if (openApp == AppPhotos) { Screen = PhoneScreen.Photos; photoIndex = 0; }
            else if (openApp == AppContacts) { Screen = PhoneScreen.Contacts; contactIndex = 0; }
            else if (openApp == AppMessages) Screen = PhoneScreen.Messages;
            else if (openApp == AppCamera) EnterCamera();
            else if (openApp == AppSearch) { Screen = PhoneScreen.Search; searchIndex = Mathf.Max(0, searchWords.Count - 1); }
            else { Screen = PhoneScreen.Home; Notice(Loc.L("This app doesn't open. Yet.", "这个 App 打不开。还打不开。")); }
        }

        void TickMessages(float dt)
        {
            if (GameInput.BackDown() && !forceRaise) { Screen = PhoneScreen.Home; return; }
            // read the first unread message
            foreach (PhoneMsg m in msgs)
            {
                if (m.mine || m.shown >= m.text.Length) continue;
                m.shown = Mathf.Min(m.text.Length, m.shown + readCharsPerSecond * dt);
                return;
            }
            if (task == null) return;
            if (!typing)
            {
                int n = task.options != null ? task.options.Length : 0;
                if (n > 0)
                {
                    if (GameInput.LeftKeyDown()) choice = (choice + n - 1) % n;
                    if (GameInput.RightKeyDown()) choice = (choice + 1) % n;
                }
                if (GameInput.LeftDown() && n > 0)
                {
                    if (task.correct >= 0 && choice != task.correct) { Notice(Loc.T(task.wrongText)); return; }
                    if (task.check != null)
                    {
                        string err = task.check(choice);
                        if (!string.IsNullOrEmpty(err)) { Notice(err); return; }
                    }
                    typing = true;
                    typeProgress = 0f;
                }
                return;
            }
            if (GameInput.LeftHeld()) typeProgress = Mathf.Min(1f, typeProgress + dt / Mathf.Max(0.1f, task.typeSeconds));
            if (typeProgress >= 1f)
            {
                PhoneTask done = task;
                int c = choice;
                string sent = done.forward ? done.sendText : done.options[c];
                msgs.Add(new PhoneMsg { from = "me", text = sent, mine = true, shown = sent.Length });
                task = null;
                typing = false;
                Notice(Loc.L("Delivered.", "已送达。"));
                if (done.onDone != null) done.onDone(c);
            }
        }

        void CheckCode()
        {
            if (Screen == PhoneScreen.Lock)
            {
                if (entry == phoneCode) { Screen = PhoneScreen.Booting; loadT = 0f; Notice(Loc.L("Don't put it down.", "别放下手机。")); }
                else Notice(Loc.L("Wrong passcode.", "密码错误。"));
            }
            else
            {
                string code = (openApp >= 0 && openApp < appCodes.Length) ? appCodes[openApp] : "";
                if (!string.IsNullOrEmpty(code) && entry == code) { Screen = PhoneScreen.AppLoading; loadT = 0f; Notice(Loc.L("Don't put it down.", "别放下手机。")); }
                else Notice(Loc.L("Wrong passcode.", "密码错误。"));
            }
            entry = "";
        }

        void Notice(string s) { notice = s; noticeTimer = 2.5f; }

        // ------------------------------------------------------------------ drawing
        void Refresh()
        {
            bool isLock = Screen == PhoneScreen.Lock || Screen == PhoneScreen.AppLock;
            bool isLoad = Screen == PhoneScreen.Booting || Screen == PhoneScreen.AppLoading;
            lockRoot.SetActive(isLock);
            loadRoot.SetActive(isLoad);
            homeRoot.SetActive(Screen == PhoneScreen.Home);
            photosRoot.SetActive(Screen == PhoneScreen.Photos);
            msgRoot.SetActive(Screen == PhoneScreen.Messages);
            contactsRoot.SetActive(Screen == PhoneScreen.Contacts);
            bool cam = Screen == PhoneScreen.Camera;
            cameraRoot.SetActive(cam);
            searchRoot.SetActive(Screen == PhoneScreen.Search);
            // the camera turns the screen into glass: you see the room through it
            screenBg.color = cam ? new Color(1f, 1f, 1f, 0.03f) : ScreenColor;
            panelBg.color = cam ? new Color(0f, 0f, 0f, 0f) : new Color(0.05f, 0.05f, 0.06f);
            crackImg.enabled = !cam;
            if (shutterFlash > 0f) shutterFlash = Mathf.Max(0f, shutterFlash - Time.deltaTime * 2.5f);
            whiteFlash.color = new Color(1f, 1f, 1f, shutterFlash * 0.9f);

            string hint = "";
            if (isLock)
            {
                lockTitle.text = Screen == PhoneScreen.Lock ? Loc.L("Enter passcode", "输入密码") : Loc.L(AppName(openApp) + " is locked\nEnter passcode", AppName(openApp) + " 已上锁\n输入密码");
                string dots = "";
                for (int i = 0; i < 4; i++) dots += (i < entry.Length ? "*" : "_") + (i < 3 ? "  " : "");
                lockDots.text = dots;
                lockHint.text = Screen == PhoneScreen.Lock ? Loc.T(phoneHint) : HintFor(openApp);
                lockNoteRoot.SetActive(Screen == PhoneScreen.Lock);
                lockNote.text = LockNotificationText();
                UIFactory.FitFont(lockNote, lockNote.text);
                hint = Screen == PhoneScreen.Lock ? Loc.L("Number keys: type    Backspace: delete", "数字键：输入    Backspace：删除") : Loc.L("Number keys: type    Q: back", "数字键：输入    Q：返回");
            }
            else if (isLoad)
            {
                float total = Screen == PhoneScreen.Booting ? bootSeconds : appLoadSeconds[openApp];
                float p = Mathf.Clamp01(loadT / Mathf.Max(0.1f, total));
                loadTitle.text = Screen == PhoneScreen.Booting ? Loc.L("STARTING UP", "正在开机") : Loc.L("OPENING " + AppName(openApp).ToUpper(), "正在打开 " + AppName(openApp));
                loadPercent.text = Mathf.FloorToInt(p * 100f) + "%";
                loadFill.anchorMax = new Vector2(p, 1f);
                hint = Loc.L("Keep holding Right Mouse.", "按住鼠标右键别松开。");
            }
            else if (Screen == PhoneScreen.Home)
            {
                homeClock.text = phoneClock;
                int unread = UnreadCount;
                for (int i = 0; i < appTiles.Length; i++)
                {
                    int app = HomeOrder[i];
                    bool on = i == selectedApp;
                    appTiles[i].color = on ? new Color(0.20f, 0.20f, 0.22f) : new Color(0f, 0f, 0f, 0.12f);
                    appLabels[i].color = on ? new Color(0.98f, 0.9f, 0.7f) : Ink;
                    string extra = !IsUnlocked(app) ? Loc.L("\n(locked)", "\n（已上锁）") : (app == AppMessages && unread > 0 ? Loc.L("\n(" + unread + " new)", "\n（" + unread + " 条新）") : "");
                    appLabels[i].text = AppName(app) + extra;
                }
                hint = Loc.L("A / D: choose app    Left Mouse: open", "A / D：选择    鼠标左键：打开");
            }
            else if (Screen == PhoneScreen.Photos)
            {
                bool snap = photoIndex >= BasePhotos;
                for (int i = 0; i < photos.Length; i++) photos[i].SetActive(!snap && i == photoIndex);
                snapshot.SetActive(snap);
                if (snap)
                {
                    string label = Loc.T(taken[Mathf.Clamp(photoIndex - BasePhotos, 0, taken.Count - 1)]);
                    snapLabel.text = label;
                    photoCaption.text = Loc.L("Today. ", "今天。") + label;
                }
                else photoCaption.text = photoIndex < photoCaptions.Length ? Loc.T(photoCaptions[photoIndex]) : "";
                UIFactory.FitFont(photoCaption, photoCaption.text);
                photoCounter.text = (photoIndex + 1) + " / " + PhotoCount;
                hint = Loc.L("A / D: browse    Q: back", "A / D：翻看    Q：返回");
            }
            else if (Screen == PhoneScreen.Contacts)
            {
                if (contacts.Count > 0)
                {
                    PhoneContact c = contacts[Mathf.Clamp(contactIndex, 0, contacts.Count - 1)];
                    contactName.text = Loc.T(c.name);
                    contactYear.text = Loc.L("saved in " + c.year, "存于 " + c.year + " 年");
                    contactNote.text = Loc.T(c.note);
                    UIFactory.FitFont(contactNote, contactNote.text);
                    UIFactory.FitFont(contactName, contactName.text, 14);
                    contactCounter.text = (contactIndex + 1) + " / " + contacts.Count;
                }
                hint = Loc.L("A / D: browse    Q: back", "A / D：翻看    Q：返回");
            }
            else if (Screen == PhoneScreen.Messages)
            {
                hint = RefreshMessages();
            }
            else if (cam)
            {
                if (IsUp) aimed = FindAimed();
                bool has = aimed != null;
                camLabel.text = has ? (aimed.taken ? Loc.L("(already taken)  ", "（已拍过）  ") : "") + Loc.T(aimed.label) : Loc.L("...", "……");
                Color rc = has && !aimed.taken ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 1f, 1f, 0.7f);
                foreach (Image r in reticle) r.color = rc;
                camFlashText.text = cameraFlash ? Loc.L("FLASH  ON", "闪光灯  开") : Loc.L("FLASH  OFF", "闪光灯  关");
                camFlashText.color = cameraFlash ? new Color(1f, 0.55f, 0.35f) : new Color(0.8f, 0.85f, 0.8f);
                camCount.text = Loc.L(taken.Count + " taken", "已拍 " + taken.Count + " 张");
                hint = Loc.L("Aim the middle    Left Mouse: shutter    A / D: flash    Q: back", "对准中间    左键：拍照    A / D：闪光灯    Q：返回");
            }
            else if (Screen == PhoneScreen.Search)
            {
                int n = searchWords.Count;
                string w = n > 0 ? Loc.T(searchWords[Mathf.Clamp(searchIndex, 0, n - 1)]) : Loc.L("(no words yet - read something)", "（还没有搜索词——先去读点什么）");
                searchQuery.text = n > 1 ? "<  " + w + "  >" : w;
                searchBarRoot.SetActive(searching);
                searchFill.anchorMax = new Vector2(searchProgress, 1f);
                searchResultText.text = searching ? Loc.L("searching...", "搜索中……") : searchResult;
                UIFactory.FitFont(searchResultText, searchResultText.text);
                searchHeader.text = AppName(AppSearch) + Loc.L("   (" + n + " words)", "   （" + n + " 个词）");
                hint = n == 0 ? Loc.L("Q: back", "Q：返回") : (searching ? Loc.L("Keep HOLDING Left Mouse", "按住鼠标左键别松") : Loc.L("A / D: word    HOLD Left Mouse: search    Q: back", "A / D：选词    按住左键：搜索    Q：返回"));
            }
            if (silentAvailable && noticeTimer <= 0f && !cam) hint = Loc.L("M: silent mode", "M：静音模式") + (string.IsNullOrEmpty(hint) ? "" : "    " + hint);
            footer.text = noticeTimer > 0f ? notice : hint;
            UIFactory.FitFont(footer, footer.text);
            // the little bell, top right, on every screen
            Color bell = silent ? new Color(0.75f, 0.2f, 0.15f) : (cam ? new Color(1f, 1f, 1f, 0.8f) : new Color(0.3f, 0.3f, 0.32f));
            bellBody.color = bellClapper.color = bell;
            bellSlash.enabled = silent;
            bellText.text = silent ? Loc.L("SILENT", "静音") : "";
            footer.color = noticeTimer > 0f ? (cam ? new Color(1f, 0.6f, 0.45f) : new Color(0.75f, 0.2f, 0.15f)) : (cam ? new Color(0.92f, 0.92f, 0.9f) : new Color(0.35f, 0.35f, 0.37f));
        }

        string LockNotificationText()
        {
            foreach (PhoneMsg m in msgs)
            {
                if (m.mine || m.shown >= m.text.Length) continue;
                int n = UnreadCount;
                int maxChars = Loc.Chinese ? 30 : 46;
                string preview = m.text.Length > maxChars ? m.text.Substring(0, maxChars) + "..." : m.text;
                return Loc.L(n + (n == 1 ? " new message\n" : " new messages\n"), n + " 条新信息\n") + Loc.T(m.from) + ": " + preview;
            }
            return Loc.T(lockNotification);
        }

        string RefreshMessages()
        {
            int unread = UnreadCount;
            msgHeader.text = AppName(AppMessages) + (unread > 0 ? Loc.L("   (" + unread + " new)", "   （" + unread + " 条新）") : "");

            // show messages up to the one being read
            int last = msgs.Count - 1;
            for (int i = 0; i < msgs.Count; i++)
            {
                if (!msgs[i].mine && msgs[i].shown < msgs[i].text.Length) { last = i; break; }
            }
            int first = Mathf.Max(0, last - Bubbles + 1);
            for (int s = 0; s < Bubbles; s++)
            {
                int i = first + s;
                bool on = i <= last && i < msgs.Count;
                bubbleBg[s].gameObject.SetActive(on);
                if (!on) continue;
                PhoneMsg m = msgs[i];
                int shown = Mathf.FloorToInt(m.shown);
                bool reading = !m.mine && shown < m.text.Length;
                bubbleFrom[s].text = m.mine ? Loc.L("me", "我") : Loc.T(m.from);
                UIFactory.FitFont(bubbleText[s], m.text + "_");   // sized by the whole message, so it never jumps while reading
                bubbleText[s].text = (reading ? m.text.Substring(0, shown) + "_" : m.text);
                bubbleBg[s].color = m.mine ? new Color(0.22f, 0.24f, 0.26f, 0.95f) : new Color(0f, 0f, 0f, 0.09f);
                bubbleText[s].color = m.mine ? new Color(0.95f, 0.92f, 0.82f) : Ink;
                bubbleFrom[s].color = m.mine ? new Color(0.9f, 0.75f, 0.5f) : new Color(0.45f, 0.3f, 0.2f);
                bubbleFrom[s].alignment = m.mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                bubbleText[s].alignment = m.mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            }

            bool showTask = task != null && unread == 0;
            taskRoot.SetActive(showTask);
            if (unread > 0) return Loc.L("reading...  (keep the phone up)", "正在读……（别放下手机）");
            if (!showTask) return Loc.L("Q: back", "Q：返回");
            typeBarRoot.SetActive(typing);
            if (!typing)
            {
                taskHeader.text = Loc.T(task.header);
                string opt = task.options != null && task.options.Length > 0 ? Loc.T(task.options[Mathf.Clamp(choice, 0, task.options.Length - 1)]) : "";
                taskBody.text = "<  " + opt + "  >";
                UIFactory.FitFont(taskBody, taskBody.text);
                taskHelp.text = Loc.L("A / D: choose    Left Mouse: select", "A / D：选择    鼠标左键：确定");
                return "";
            }
            string text = task.forward ? task.sendText : task.options[choice];
            taskHeader.text = task.forward ? Loc.L("To: ", "发给：") + Loc.T(task.options[choice]) : (string.IsNullOrEmpty(task.toLabel) ? Loc.L("Reply", "回复") : Loc.T(task.toLabel));
            int n = Mathf.FloorToInt(typeProgress * text.Length);
            UIFactory.FitFont(taskBody, text + "_");
            taskBody.text = text.Substring(0, Mathf.Clamp(n, 0, text.Length)) + "_";
            typeFill.anchorMax = new Vector2(typeProgress, 1f);
            taskHelp.text = Loc.L("HOLD Left Mouse: type & send", "按住鼠标左键：输入并发送");
            return "";
        }

        // ------------------------------------------------------------------ UI
        /// <summary>The phone's own notification: a dark banner with the sender and the first words, plus "hold right mouse".</summary>
        void BuildNotification(Transform canvasRoot)
        {
            notif = UIFactory.Rect("Notification", canvasRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, NotifHidden), new Vector2(480f, 104f));
            UIFactory.AddImage(notif, new Color(0.06f, 0.06f, 0.07f, 0.96f));
            UIFactory.AddImage(UIFactory.Rect("Edge", notif, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f)), new Color(0.95f, 0.86f, 0.62f));
            RectTransform icon = UIFactory.TopLeft("Icon", notif, 16f, 18f, 46f, 46f);
            UIFactory.AddImage(icon, new Color(0.95f, 0.86f, 0.62f));
            UIFactory.AddText(UIFactory.Stretch("Glyph", icon), "...", 26, new Color(0.1f, 0.1f, 0.12f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            Text app = UIFactory.AddText(UIFactory.TopLeft("App", notif, 76f, 10f, 300f, 22f), Loc.L("MESSAGES  -  now", "信息  -  刚刚"), 15, new Color(0.65f, 0.65f, 0.68f), TextAnchor.UpperLeft);
            app.fontStyle = FontStyle.Bold;
            notifCount = UIFactory.AddText(UIFactory.TopLeft("Count", notif, 230f, 10f, 234f, 22f), "", 15, new Color(0.95f, 0.86f, 0.62f), TextAnchor.UpperRight);
            notifFrom = UIFactory.AddText(UIFactory.TopLeft("From", notif, 76f, 32f, 392f, 24f), "", 18, Color.white, TextAnchor.UpperLeft);
            notifFrom.fontStyle = FontStyle.Bold;
            notifText = UIFactory.AddText(UIFactory.TopLeft("Text", notif, 76f, 56f, 392f, 24f), "", 17, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            notifText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.AddText(UIFactory.TopLeft("Hint", notif, 76f, 80f, 392f, 20f), Loc.L("HOLD RIGHT MOUSE to read", "按住鼠标右键查看"), 14, new Color(0.95f, 0.86f, 0.62f, 0.8f), TextAnchor.UpperLeft);
        }

        void BuildUI()
        {
            Canvas canvas = UIFactory.MakeCanvas("PhoneCanvas", 30);
            canvas.transform.SetParent(transform, false);
            canvas.pixelPerfect = true;
            // bigger phone = a larger canvas scale factor, so glyphs are rendered at their real on-screen size
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f) / PanelScale;
            phoneCanvas = canvas;

            panel = UIFactory.Rect("Phone", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                   new Vector2(0f, HiddenY), new Vector2(440f, 780f));
            BuildNotification(canvas.transform);
            panelBg = UIFactory.AddImage(panel, new Color(0.05f, 0.05f, 0.06f));
            // the bezel as four bars, so the camera can make the glass see-through and the frame stays
            Color bz = new Color(0.05f, 0.05f, 0.06f);
            UIFactory.AddImage(UIFactory.Rect("Bezel_T", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 18f)), bz);
            UIFactory.AddImage(UIFactory.Rect("Bezel_B", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 18f)), bz);
            UIFactory.AddImage(UIFactory.Rect("Bezel_L", panel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(18f, 0f)), bz);
            UIFactory.AddImage(UIFactory.Rect("Bezel_R", panel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(18f, 0f)), bz);
            RectTransform screen = UIFactory.Rect("Screen", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-36f, -36f));
            screen.gameObject.AddComponent<RectMask2D>();   // nothing (the crack, long texts) draws outside the glass
            screenBg = UIFactory.AddImage(screen, ScreenColor);
            screenRT = screen;

            // a crack across the screen protector - someone else's damage
            RectTransform crack = UIFactory.Centered("Crack", screen, 40f, 150f, 380f, 2f);
            crack.localRotation = Quaternion.Euler(0f, 0f, 38f);
            crackImg = UIFactory.AddImage(crack, new Color(0f, 0f, 0f, 0.18f));

            // --- passcode (phone lock + app lock)
            lockRoot = UIFactory.Stretch("Passcode", screen).gameObject;
            RectTransform note = UIFactory.TopLeft("Notification", lockRoot.transform, 14f, 14f, 376f, 78f);
            UIFactory.AddImage(note, new Color(0f, 0f, 0f, 0.12f));
            lockNote = UIFactory.AddText(UIFactory.Rect("T", note, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -10f)), "", 17, Ink, TextAnchor.MiddleLeft);
            lockNoteRoot = note.gameObject;
            lockTitle = UIFactory.AddText(UIFactory.TopLeft("Title", lockRoot.transform, 18f, 100f, 368f, 70f), "", 26, Ink, TextAnchor.MiddleCenter);
            lockDots = UIFactory.AddText(UIFactory.TopLeft("Dots", lockRoot.transform, 18f, 168f, 368f, 50f), "", 44, Ink, TextAnchor.MiddleCenter);
            lockHint = UIFactory.AddText(UIFactory.TopLeft("Hint", lockRoot.transform, 18f, 222f, 368f, 34f), "", 18, new Color(0.55f, 0.3f, 0.15f), TextAnchor.MiddleCenter);
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "" };
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "") continue;
                int col = i % 3, row = i / 3;
                RectTransform k = UIFactory.TopLeft("Key" + keys[i], lockRoot.transform, 52f + col * 104f, 272f + row * 84f, 88f, 70f);
                UIFactory.AddImage(k, new Color(0f, 0f, 0f, 0.08f));
                UIFactory.AddText(UIFactory.Stretch("T", k), keys[i], 32, Ink, TextAnchor.MiddleCenter);
            }

            // --- loading (boot + app)
            loadRoot = UIFactory.Stretch("Loading", screen).gameObject;
            UIFactory.AddImage((RectTransform)loadRoot.transform, new Color(0.04f, 0.04f, 0.05f));
            Color dim = new Color(0.8f, 0.78f, 0.72f);
            loadTitle = UIFactory.AddText(UIFactory.TopLeft("Title", loadRoot.transform, 18f, 260f, 368f, 50f), "", 26, dim, TextAnchor.MiddleCenter);
            RectTransform bar = UIFactory.TopLeft("Bar", loadRoot.transform, 60f, 330f, 284f, 10f);
            UIFactory.AddImage(bar, new Color(1f, 1f, 1f, 0.15f));
            loadFill = UIFactory.Rect("Fill", bar, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.AddImage(loadFill, new Color(0.95f, 0.75f, 0.45f));
            loadPercent = UIFactory.AddText(UIFactory.TopLeft("Percent", loadRoot.transform, 18f, 350f, 368f, 40f), "", 22, dim, TextAnchor.MiddleCenter);

            // --- home
            homeRoot = UIFactory.Stretch("Home", screen).gameObject;
            homeClock = UIFactory.AddText(UIFactory.TopLeft("Clock", homeRoot.transform, 18f, 40f, 368f, 100f), "", 72, Ink, TextAnchor.MiddleCenter);
            appTiles = new Image[appNames.Length];
            appLabels = new Text[appNames.Length];
            for (int i = 0; i < HomeOrder.Length; i++)
            {
                int app = HomeOrder[i];
                int col = i % 3, row = i / 3;
                RectTransform t = UIFactory.TopLeft("App_" + appNames[app], homeRoot.transform, 22f + col * 122f, 200f + row * 150f, 110f, 130f);
                appTiles[i] = UIFactory.AddImage(t, Color.clear);
                RectTransform icon = UIFactory.TopLeft("Icon", t, 25f, 12f, 60f, 60f);
                UIFactory.AddImage(icon, Color.HSVToRGB(app * 0.17f, 0.35f, 0.65f));
                UIFactory.AddText(UIFactory.Stretch("Order", icon), (i + 1).ToString(), 22, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter);
                appLabels[i] = UIFactory.AddText(UIFactory.TopLeft("Label", t, 0f, 76f, 110f, 54f), AppName(app), 16, Ink, TextAnchor.UpperCenter);
            }

            // --- photos
            photosRoot = UIFactory.Stretch("Photos", screen).gameObject;
            RectTransform frame = UIFactory.TopLeft("Frame", photosRoot.transform, 18f, 40f, 368f, 320f);
            UIFactory.AddImage(frame, new Color(0.1f, 0.1f, 0.1f));
            photos = new GameObject[6];
            photos[0] = PhotoClass(frame, "Photo_Class2009", new Color(0.55f, 0.58f, 0.6f), new Color(0.18f, 0.18f, 0.2f), new Color(0.25f, 0.27f, 0.32f), null);
            photos[1] = PhotoHand(frame);
            photos[2] = PhotoSneeze(frame);
            photos[3] = PhotoTriangle(frame);
            photos[4] = PhotoNight(frame);
            photos[5] = PhotoClass(frame, "Photo_Class1979", new Color(0.62f, 0.52f, 0.38f), new Color(0.3f, 0.22f, 0.15f), new Color(0.4f, 0.31f, 0.21f), Loc.L("VOSS", "沃斯"));
            snapshot = UIFactory.Stretch("Photo_Snapshot", frame).gameObject;
            Box(snapshot.transform, 0f, 0f, 368f, 320f, new Color(0.32f, 0.33f, 0.3f));
            Box(snapshot.transform, 0f, 250f, 368f, 70f, new Color(0.22f, 0.22f, 0.2f));
            Box(snapshot.transform, 84f, 60f, 200f, 150f, new Color(0.8f, 0.78f, 0.7f));
            snapLabel = UIFactory.AddText(UIFactory.TopLeft("Label", snapshot.transform, 90f, 66f, 188f, 138f), "", 20, Ink, TextAnchor.MiddleCenter);
            snapLabel.fontStyle = FontStyle.Bold;
            snapshot.SetActive(false);
            photoCaption = UIFactory.AddText(UIFactory.TopLeft("Caption", photosRoot.transform, 18f, 375f, 368f, 140f), "", 18, Ink, TextAnchor.UpperLeft);
            photoCounter = UIFactory.AddText(UIFactory.TopLeft("Counter", photosRoot.transform, 18f, 8f, 368f, 30f), "", 16, Ink, TextAnchor.MiddleRight);

            // --- messages
            msgRoot = UIFactory.Stretch("Messages", screen).gameObject;
            msgHeader = UIFactory.AddText(UIFactory.TopLeft("Header", msgRoot.transform, 18f, 10f, 368f, 34f), AppName(AppMessages), 22, Ink, TextAnchor.MiddleLeft);
            msgHeader.fontStyle = FontStyle.Bold;
            UIFactory.AddImage(UIFactory.TopLeft("Divider", msgRoot.transform, 18f, 46f, 368f, 2f), new Color(0f, 0f, 0f, 0.25f));
            bubbleBg = new Image[Bubbles];
            bubbleFrom = new Text[Bubbles];
            bubbleText = new Text[Bubbles];
            for (int i = 0; i < Bubbles; i++)
            {
                RectTransform b = UIFactory.TopLeft("Bubble" + i, msgRoot.transform, 14f, 56f + i * 112f, 376f, 106f);
                bubbleBg[i] = UIFactory.AddImage(b, new Color(0f, 0f, 0f, 0.09f));
                bubbleFrom[i] = UIFactory.AddText(UIFactory.TopLeft("From", b, 10f, 4f, 356f, 22f), "", 15, Ink, TextAnchor.UpperLeft);
                bubbleFrom[i].fontStyle = FontStyle.Bold;
                bubbleText[i] = UIFactory.AddText(UIFactory.TopLeft("Text", b, 10f, 26f, 356f, 78f), "", 17, Ink, TextAnchor.UpperLeft);
            }
            taskRoot = UIFactory.TopLeft("Task", msgRoot.transform, 14f, 504f, 376f, 160f).gameObject;
            UIFactory.AddImage((RectTransform)taskRoot.transform, new Color(0.2f, 0.12f, 0.08f, 0.12f));
            taskHeader = UIFactory.AddText(UIFactory.TopLeft("Header", taskRoot.transform, 10f, 6f, 356f, 26f), "", 17, new Color(0.45f, 0.25f, 0.12f), TextAnchor.UpperLeft);
            taskHeader.fontStyle = FontStyle.Bold;
            taskBody = UIFactory.AddText(UIFactory.TopLeft("Body", taskRoot.transform, 10f, 34f, 356f, 80f), "", 19, Ink, TextAnchor.UpperCenter);
            typeBarRoot = UIFactory.TopLeft("TypeBar", taskRoot.transform, 10f, 118f, 356f, 8f).gameObject;
            UIFactory.AddImage((RectTransform)typeBarRoot.transform, new Color(0f, 0f, 0f, 0.15f));
            typeFill = UIFactory.Rect("Fill", typeBarRoot.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.AddImage(typeFill, new Color(0.85f, 0.45f, 0.15f));
            taskHelp = UIFactory.AddText(UIFactory.TopLeft("Help", taskRoot.transform, 10f, 132f, 356f, 24f), "", 14, new Color(0.35f, 0.35f, 0.37f), TextAnchor.UpperCenter);
            taskRoot.SetActive(false);

            // --- contacts
            contactsRoot = UIFactory.Stretch("Contacts", screen).gameObject;
            UIFactory.AddText(UIFactory.TopLeft("Header", contactsRoot.transform, 18f, 10f, 368f, 34f), AppName(AppContacts), 22, Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            contactCounter = UIFactory.AddText(UIFactory.TopLeft("Counter", contactsRoot.transform, 18f, 10f, 368f, 34f), "", 16, Ink, TextAnchor.MiddleRight);
            RectTransform card = UIFactory.TopLeft("Card", contactsRoot.transform, 18f, 70f, 368f, 420f);
            UIFactory.AddImage(card, new Color(0f, 0f, 0f, 0.08f));
            RectTransform avatar = UIFactory.TopLeft("Avatar", card, 134f, 24f, 100f, 100f);
            UIFactory.AddImage(avatar, new Color(0.6f, 0.58f, 0.52f));
            UIFactory.AddText(UIFactory.Stretch("Q", avatar), "?", 54, new Color(0.95f, 0.92f, 0.85f), TextAnchor.MiddleCenter);
            contactName = UIFactory.AddText(UIFactory.TopLeft("Name", card, 10f, 140f, 348f, 44f), "", 25, Ink, TextAnchor.MiddleCenter);
            contactName.fontStyle = FontStyle.Bold;
            contactYear = UIFactory.AddText(UIFactory.TopLeft("Year", card, 10f, 186f, 348f, 28f), "", 16, new Color(0.45f, 0.3f, 0.2f), TextAnchor.MiddleCenter);
            contactNote = UIFactory.AddText(UIFactory.TopLeft("Note", card, 20f, 236f, 328f, 160f), "", 19, Ink, TextAnchor.UpperCenter);

            // --- camera: a see-through viewfinder
            cameraRoot = UIFactory.Stretch("Camera", screen).gameObject;
            Color w = new Color(1f, 1f, 1f, 0.8f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = new Vector2(i % 2, i / 2);
                RectTransform c = UIFactory.Rect("Corner" + i, cameraRoot.transform, a, a, a, new Vector2(a.x > 0 ? -24f : 24f, a.y > 0 ? -60f : 120f), new Vector2(46f, 46f));
                UIFactory.AddImage(UIFactory.Rect("H", c, a, a, a, Vector2.zero, new Vector2(46f, 4f)), w);
                UIFactory.AddImage(UIFactory.Rect("V", c, a, a, a, Vector2.zero, new Vector2(4f, 46f)), w);
            }
            reticle = new Image[4];
            reticle[0] = UIFactory.AddImage(UIFactory.Centered("Ret_L", cameraRoot.transform, -26f, 0f, 22f, 3f), w);
            reticle[1] = UIFactory.AddImage(UIFactory.Centered("Ret_R", cameraRoot.transform, 26f, 0f, 22f, 3f), w);
            reticle[2] = UIFactory.AddImage(UIFactory.Centered("Ret_T", cameraRoot.transform, 0f, 26f, 3f, 22f), w);
            reticle[3] = UIFactory.AddImage(UIFactory.Centered("Ret_B", cameraRoot.transform, 0f, -26f, 3f, 22f), w);
            RectTransform lb = UIFactory.TopLeft("LabelBG", cameraRoot.transform, 18f, 470f, 368f, 46f);
            UIFactory.AddImage(lb, new Color(0f, 0f, 0f, 0.55f));
            camLabel = UIFactory.AddText(UIFactory.Stretch("Label", lb), "", 19, Color.white, TextAnchor.MiddleCenter);
            RectTransform tb = UIFactory.TopLeft("TopBG", cameraRoot.transform, 0f, 0f, 404f, 40f);
            UIFactory.AddImage(tb, new Color(0f, 0f, 0f, 0.55f));
            camFlashText = UIFactory.AddText(UIFactory.TopLeft("Flash", tb, 14f, 6f, 200f, 28f), "", 17, Color.white, TextAnchor.MiddleLeft);
            camFlashText.fontStyle = FontStyle.Bold;
            camCount = UIFactory.AddText(UIFactory.TopLeft("Count", tb, 190f, 6f, 200f, 28f), "", 16, Color.white, TextAnchor.MiddleRight);
            RectTransform fb = UIFactory.TopLeft("FooterBG", cameraRoot.transform, 0f, 672f, 404f, 72f);
            UIFactory.AddImage(fb, new Color(0f, 0f, 0f, 0.55f));
            cameraRoot.SetActive(false);

            // --- search: the school's catalogue
            searchRoot = UIFactory.Stretch("Search", screen).gameObject;
            searchHeader = UIFactory.AddText(UIFactory.TopLeft("Header", searchRoot.transform, 18f, 10f, 368f, 34f), AppName(AppSearch), 22, Ink, TextAnchor.MiddleLeft);
            searchHeader.fontStyle = FontStyle.Bold;
            UIFactory.AddImage(UIFactory.TopLeft("Divider", searchRoot.transform, 18f, 46f, 368f, 2f), new Color(0f, 0f, 0f, 0.25f));
            UIFactory.AddText(UIFactory.TopLeft("Catalogue", searchRoot.transform, 18f, 54f, 368f, 24f), Loc.L("CLEARVIEW LIBRARY CATALOGUE  -  since 04/1967", "明鉴图书馆目录  -  始于 1967 年 4 月"), 14, new Color(0.45f, 0.3f, 0.2f), TextAnchor.MiddleLeft);
            RectTransform qbox = UIFactory.TopLeft("Query", searchRoot.transform, 18f, 86f, 368f, 56f);
            UIFactory.AddImage(qbox, new Color(0f, 0f, 0f, 0.1f));
            searchQuery = UIFactory.AddText(UIFactory.Stretch("Text", qbox), "", 21, Ink, TextAnchor.MiddleCenter);
            searchQuery.fontStyle = FontStyle.Bold;
            searchBarRoot = UIFactory.TopLeft("Bar", searchRoot.transform, 18f, 150f, 368f, 8f).gameObject;
            UIFactory.AddImage((RectTransform)searchBarRoot.transform, new Color(0f, 0f, 0f, 0.15f));
            searchFill = UIFactory.Rect("Fill", searchBarRoot.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.AddImage(searchFill, new Color(0.85f, 0.45f, 0.15f));
            searchResultText = UIFactory.AddText(UIFactory.TopLeft("Result", searchRoot.transform, 22f, 172f, 360f, 480f), "", 18, Ink, TextAnchor.UpperLeft);
            searchRoot.SetActive(false);

            footer = UIFactory.AddText(UIFactory.TopLeft("Footer", screen, 18f, 680f, 368f, 50f), "", 16, Ink, TextAnchor.UpperCenter);

            dangerTint = UIFactory.AddImage(UIFactory.Stretch("DangerRed", screen), new Color(0.85f, 0.05f, 0.05f, 0f));
            // a tiny bell in the corner of every screen: the silent switch nobody ever explains
            RectTransform bellR = UIFactory.TopLeft("Bell", screen, 382f, 3f, 20f, 18f);
            bellBody = UIFactory.AddImage(UIFactory.TopLeft("Body", bellR, 5f, 1f, 10f, 11f), new Color(0.3f, 0.3f, 0.32f));
            bellClapper = UIFactory.AddImage(UIFactory.TopLeft("Clapper", bellR, 2f, 12f, 16f, 3f), new Color(0.3f, 0.3f, 0.32f));
            RectTransform slash = UIFactory.Centered("Slash", bellR, 0f, 0f, 24f, 3f);
            slash.localRotation = Quaternion.Euler(0f, 0f, 40f);
            bellSlash = UIFactory.AddImage(slash, new Color(0.75f, 0.2f, 0.15f));
            bellSlash.enabled = false;
            bellText = UIFactory.AddText(UIFactory.TopLeft("BellText", screen, 270f, 2f, 108f, 20f), "", 13, new Color(0.75f, 0.2f, 0.15f), TextAnchor.MiddleRight);
            bellText.fontStyle = FontStyle.Bold;
            whiteFlash = UIFactory.AddImage(UIFactory.Stretch("ShutterFlash", screen), new Color(1f, 1f, 1f, 0f));
            whiteFlash.raycastTarget = false;
        }

        // Photos are drawn with simple shapes - replace with real images later.
        static RectTransform Box(Transform parent, float x, float y, float w, float h, Color c)
        {
            RectTransform r = UIFactory.TopLeft("Shape", parent, x, y, w, h);
            UIFactory.AddImage(r, c);
            return r;
        }

        GameObject PhotoClass(RectTransform frame, string name, Color wall, Color head, Color body, string pencil)
        {
            GameObject p = UIFactory.Stretch(name, frame).gameObject;
            Box(p.transform, 0f, 0f, 368f, 320f, wall);
            Box(p.transform, 0f, 250f, 368f, 70f, new Color(wall.r * 0.6f, wall.g * 0.6f, wall.b * 0.6f));
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 5; i++)
                {
                    float x = 40f + i * 62f + row * 6f, y = 60f + row * 62f;
                    Box(p.transform, x, y, 22f, 22f, head);
                    Box(p.transform, x - 6f, y + 24f, 34f, 30f, body);
                }
            // circled: back row, window side
            float cx = 34f, cy = 54f;
            Color y1 = pencil == null ? new Color(1f, 0.8f, 0.2f) : new Color(0.15f, 0.12f, 0.1f);
            Box(p.transform, cx, cy, 34f, 3f, y1); Box(p.transform, cx, cy + 62f, 34f, 3f, y1);
            Box(p.transform, cx, cy, 3f, 64f, y1); Box(p.transform, cx + 32f, cy, 3f, 64f, y1);
            if (pencil != null)
            {
                Text t = UIFactory.AddText(UIFactory.TopLeft("Pencil", p.transform, 14f, 268f, 200f, 40f), pencil, 26, new Color(0.18f, 0.14f, 0.1f), TextAnchor.MiddleLeft);
                t.fontStyle = FontStyle.Italic;
                Box(p.transform, 0f, 0f, 368f, 320f, new Color(0.4f, 0.3f, 0.1f, 0.12f));   // age
            }
            return p;
        }

        GameObject PhotoHand(RectTransform frame)
        {
            GameObject p = UIFactory.Stretch("Photo_Hand2016", frame).gameObject;
            Box(p.transform, 0f, 0f, 368f, 320f, new Color(0.22f, 0.16f, 0.11f));
            Box(p.transform, 0f, 40f, 368f, 18f, new Color(0.4f, 0.3f, 0.2f));
            Box(p.transform, 130f, 150f, 120f, 70f, new Color(0.75f, 0.6f, 0.48f));
            Box(p.transform, 160f, 110f, 46f, 84f, new Color(0.05f, 0.05f, 0.06f));
            Box(p.transform, 164f, 114f, 38f, 70f, new Color(0.95f, 0.85f, 0.6f));
            return p;
        }

        GameObject PhotoSneeze(RectTransform frame)
        {
            GameObject p = UIFactory.Stretch("Photo_Sneeze2018", frame).gameObject;
            Box(p.transform, 0f, 0f, 368f, 320f, new Color(0.3f, 0.36f, 0.32f));        // blackboard
            Box(p.transform, 140f, 90f, 90f, 230f, new Color(0.55f, 0.18f, 0.16f));     // teacher, blurred mid-sneeze
            Box(p.transform, 158f, 40f, 54f, 54f, new Color(0.8f, 0.65f, 0.55f));
            Box(p.transform, 150f, 30f, 70f, 10f, new Color(1f, 1f, 1f, 0.35f));       // motion blur
            Box(p.transform, 230f, 50f, 40f, 4f, new Color(1f, 1f, 1f, 0.6f));
            Box(p.transform, 236f, 62f, 30f, 4f, new Color(1f, 1f, 1f, 0.5f));
            return p;
        }

        GameObject PhotoTriangle(RectTransform frame)
        {
            GameObject p = UIFactory.Stretch("Photo_Triangle2021", frame).gameObject;
            Box(p.transform, 0f, 0f, 368f, 320f, new Color(0.5f, 0.5f, 0.5f));
            Box(p.transform, 74f, 40f, 220f, 200f, new Color(0.45f, 0.33f, 0.22f));   // chair back
            Box(p.transform, 90f, 240f, 20f, 80f, new Color(0.35f, 0.25f, 0.17f));
            Box(p.transform, 258f, 240f, 20f, 80f, new Color(0.35f, 0.25f, 0.17f));
            // chalk triangle: three bars around the centre of the chair back
            Triangle(p.transform, new Vector2(184f, -140f), 110f, 6f, new Color(0.95f, 0.95f, 0.9f));
            return p;
        }

        GameObject PhotoNight(RectTransform frame)
        {
            GameObject p = UIFactory.Stretch("Photo_Night", frame).gameObject;
            Box(p.transform, 0f, 0f, 368f, 320f, new Color(0.03f, 0.03f, 0.05f));
            Box(p.transform, 250f, 60f, 60f, 80f, new Color(0.95f, 0.8f, 0.45f));
            return p;
        }

        /// <summary>Outline triangle centred at 'center' (top-left space: x right, y negative down).</summary>
        public static void Triangle(Transform parent, Vector2 center, float side, float thick, Color c)
        {
            float h = side * 0.866f;
            Vector2 a = center + new Vector2(-side * 0.5f, -h / 3f);
            Vector2 b = center + new Vector2(side * 0.5f, -h / 3f);
            Vector2 top = center + new Vector2(0f, h * 2f / 3f);
            Bar(parent, a, b, thick, c);
            Bar(parent, b, top, thick, c);
            Bar(parent, top, a, thick, c);
        }

        static void Bar(Transform parent, Vector2 from, Vector2 to, float thick, Color c)
        {
            Vector2 mid = (from + to) * 0.5f;
            Vector2 d = to - from;
            RectTransform r = UIFactory.Rect("Bar", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), mid, new Vector2(d.magnitude, thick));
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            UIFactory.AddImage(r, c);
        }
    }
}
