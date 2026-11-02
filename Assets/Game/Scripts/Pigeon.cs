// @role A courtyard pigeon outside the classroom windows: pecks, looks around, hops a little. Not monitored. ...Yet.
// @calls AudioSynth
// @tune hopRadius:float:0-1.5:how far it hops from where it started (m)
// @tune cooVolume:float:0-1:coo loudness (about as present as a footstep)
// ArchiveDirector makes the library flock agitated (the librarian comes to shoo them).
using System.Collections.Generic;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// The lesson says the courtyard pigeons are not monitored, the PA says they are not your friends.
    /// So they are there: on the window ledges, on the grass, on the founder's oak. Built by GameSceneBuilder.
    /// </summary>
    public class Pigeon : MonoBehaviour
    {
        [Tooltip("The head group (it dips to peck and turns to look around).")]
        public Transform head;
        [Tooltip("How far it hops from where it started (m). 0 = it stays put and only pecks.")]
        public float hopRadius = 0.6f;
        [Tooltip("Which directions it may hop in (a ledge pigeon only hops along the ledge).")]
        public Vector3 hopAxes = new Vector3(1f, 0f, 1f);
        [Tooltip("How loud its coo is (soft: about as present as a footstep).")]
        public float cooVolume = 0.2f;
        [Tooltip("This one stares at you whenever you're near (\"One of them is looking right at me.\").")]
        public bool starer;

        /// <summary>Every pigeon in the scene (the library uses this to stir up the flock).</summary>
        public static readonly List<Pigeon> All = new List<Pigeon>();

        AudioSource voice;
        float nextCoo, agitated;

        Vector3 home, from, to;
        Quaternion headRest;
        float t, next;
        int state;   // 0 idle (looking around), 1 peck, 2 hop

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>The flock gets noisy for a while: flapping, cooing, hopping about.</summary>
        public void Agitate(float seconds) { agitated = Mathf.Max(agitated, seconds); nextCoo = Random.Range(0f, 0.6f); }
        public bool Agitated { get { return agitated > 0f; } }

        /// <summary>Shooed: one last flap, then quiet.</summary>
        public void Calm()
        {
            agitated = 0f;
            if (voice != null) voice.PlayOneShot(AudioSynth.Flap(), cooVolume * 1.4f);
            nextCoo = Random.Range(10f, 20f);
        }

        void Start()
        {
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 0.8f;
            voice.maxDistance = 9f;
            voice.dopplerLevel = 0f;
            nextCoo = Random.Range(3f, 14f);
            home = transform.position;
            from = to = home;
            next = Random.Range(0.4f, 2.5f);
            if (head != null) headRest = head.localRotation;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            if (agitated > 0f) agitated -= dt;
            nextCoo -= dt;
            if (nextCoo <= 0f && voice != null)
            {
                bool loud = agitated > 0f;
                if (loud && Random.value < 0.5f) voice.PlayOneShot(AudioSynth.Flap(), cooVolume * 1.6f);
                else { voice.pitch = Random.Range(0.92f, 1.08f); voice.PlayOneShot(AudioSynth.Coo(), cooVolume * (loud ? 1.8f : 1f)); }
                nextCoo = loud ? Random.Range(0.5f, 1.3f) : Random.Range(8f, 20f);
                if (loud && state == 0) next = 0f;   // fidget
            }
            if (starer && state == 0)
            {
                Camera c = Camera.main;
                if (c != null && Vector3.Distance(c.transform.position, transform.position) < 12f)
                {
                    // turn the body towards you, then the head looks straight at you. It does not blink.
                    Vector3 d = c.transform.position - transform.position; d.y = 0f;
                    if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), dt * 2.5f);
                    if (head != null) head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(c.transform.position - head.position), dt * 6f);
                    t = 0f;
                    return;
                }
            }
            if (state == 0)
            {
                if (head != null) head.localRotation = headRest * Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.3f + home.x * 3f + home.z) * 28f, 0f);
                if (t < next) return;
                t = 0f;
                if (hopRadius > 0.01f && Random.value < 0.4f)
                {
                    Vector2 r = Random.insideUnitCircle * hopRadius;
                    from = transform.position;
                    to = home + Vector3.Scale(new Vector3(r.x, 0f, r.y), hopAxes);
                    state = 2;
                }
                else state = 1;
            }
            else if (state == 1)
            {
                float a = Mathf.Abs(Mathf.Sin(t * Mathf.PI / 0.28f));
                if (head != null) head.localRotation = headRest * Quaternion.Euler(55f * a, 0f, 0f);
                if (t > 0.56f) Idle();
            }
            else
            {
                Vector3 d = to - from;
                d.y = 0f;
                if (d.sqrMagnitude > 0.0004f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), dt * 10f);
                float k = Mathf.Clamp01(t / 0.35f);
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y = home.y + Mathf.Sin(k * Mathf.PI) * 0.07f;
                transform.position = p;
                if (k >= 1f) Idle();
            }
        }

        void Idle()
        {
            state = 0;
            t = 0f;
            next = Random.Range(0.7f, 3.5f);
            if (head != null) head.localRotation = headRest;
        }
    }
}
