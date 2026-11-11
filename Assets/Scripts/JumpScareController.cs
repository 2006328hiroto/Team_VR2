using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// ジャンプスケア（上位者）。シーンに1つ置く。Trigger() で発動。
///  暗転＋遠くの鐘 → ちらつき → 闇の奥に後光がうっすら浮かぶ（予兆）
///  → 目の前に突っ込む＋聖歌と金切り声＋フラッシュ＋最大振動 → ぶつ切りで消える → 照明が戻る
/// 顔・音は未設定なら自動生成。自前素材を入れればそちらを使う。
/// ※VR酔い対策でカメラは揺らさない。フラッシュは1回だけ。
/// </summary>
public class JumpScareController : MonoBehaviour
{
    [Header("Assets（空なら自動生成）")]
    [SerializeField, Tooltip("【リアルにするならこれ】透過PNGの顔画像（正面・背景透明）。空なら手続き生成")] Texture2D scareTexture;
    [SerializeField, Tooltip("3Dモデルを使う場合はこちら（画像より優先）")] GameObject scarePrefab;
    [SerializeField] AudioClip scareSound;
    [SerializeField, Tooltip("暗転中に鳴る鐘。空なら合成")] AudioClip bellSound;

    [Header("Scene")]
    [SerializeField] Light[] lights;
    [SerializeField] Transform playerCamera;

    [Header("Timing")]
    [SerializeField] float blackout = 2.2f;
    [SerializeField] float omenTime = 1.0f;
    [SerializeField] float lungeTime = 0.14f;
    [SerializeField] float holdTime = 1.2f;
    [SerializeField] float lightReturnTime = 2.5f;

    [Header("Look")]
    [SerializeField] float startDistance = 3.4f;
    [SerializeField] float endDistance = 0.6f;
    [SerializeField] float faceHeight = 1.9f;
    [SerializeField] float shake = 0.025f;

    [Header("Safety")]
    [SerializeField, Range(0.1f, 1f)] float volume = 0.8f;
    [SerializeField] bool enableJumpScare = true;
    [SerializeField] bool onlyOncePerSession = false;

    public bool IsPlaying { get; private set; }
    bool played;
    AudioClip hitClip, bellClip;
    Texture2D faceTex;
    float aspect = 1f;

    public void SetEnabled(bool on) => enableJumpScare = on;

    public void Trigger()
    {
        if (!enableJumpScare || IsPlaying || (onlyOncePerSession && played)) return;
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
        if (playerCamera == null) return;
        played = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        IsPlaying = true;
        if (lights == null || lights.Length == 0) lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        var original = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) { original[i] = lights[i].intensity; lights[i].intensity = 0f; }
        float ambient = RenderSettings.ambientIntensity; RenderSettings.ambientIntensity = 0f;

        // ---- 1. 暗転＋遠くの鐘 ----
        PlayClip(bellSound != null ? bellSound : (bellClip != null ? bellClip : (bellClip = MakeBell())), volume * 0.55f);
        yield return new WaitForSeconds(blackout * 0.6f);
        SetLights(original, 0.2f); yield return new WaitForSeconds(0.07f);
        SetLights(original, 0f);   yield return new WaitForSeconds(blackout * 0.4f);

        // ---- 2. 予兆：闇の奥に後光がうっすら ----
        var face = SpawnFace(); var flash = SpawnFlash();
        float t = 0f;
        while (t < omenTime)
        {
            t += Time.deltaTime; float k = t / omenTime;
            PlaceFace(face, Mathf.Lerp(startDistance, startDistance - 0.5f, k), 0.55f, 0f);
            SetFaceAlpha(face, Mathf.SmoothStep(0f, 0.22f, k));
            yield return null;
        }

        // ---- 3. 突っ込む ----
        SetFaceAlpha(face, 1f);
        PlayClip(hitClip != null ? hitClip : scareSound != null ? scareSound : (hitClip = MakeHit()), volume);
        Haptics(1f, 0.7f);
        t = 0f;
        while (t < lungeTime)
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / lungeTime);
            PlaceFace(face, Mathf.Lerp(startDistance - 0.5f, endDistance, k * k * k), Mathf.Lerp(0.55f, 1f, k), 0f);
            SetFlash(flash, 1f);
            yield return null;
        }

        // ---- 4. 張り付いて震える ----
        t = 0f;
        while (t < holdTime)
        {
            t += Time.deltaTime;
            SetFlash(flash, Mathf.Clamp01(1f - t / 0.4f) * 0.9f);
            PlaceFace(face, endDistance, 1f + Mathf.Sin(t * 38f) * 0.015f, shake);
            if (t > holdTime * 0.5f) Haptics(0.6f, 0.1f);
            yield return null;
        }

        // ---- 5. ぶつ切り → 暗闇 → 照明が戻る ----
        Destroy(face); Destroy(flash);
        yield return new WaitForSeconds(1.2f);
        t = 0f;
        while (t < lightReturnTime)
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / lightReturnTime);
            SetLights(original, k); RenderSettings.ambientIntensity = ambient * k;
            yield return null;
        }
        SetLights(original, 1f); RenderSettings.ambientIntensity = ambient;
        IsPlaying = false;
    }

    void SetLights(float[] o, float k) { for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = o[i] * k; }

    // ---------- 顔 ----------
    GameObject SpawnFace()
    {
        GameObject go;
        if (scarePrefab != null) go = Instantiate(scarePrefab);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            if (faceTex == null)
            {
                faceTex = scareTexture != null ? scareTexture : MakeFace(384);
                aspect = (float)faceTex.width / faceTex.height;
            }
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { mainTexture = faceTex, color = new Color(1, 1, 1, 0) };
        }
        PlaceFace(go, startDistance, 0.55f, 0f);
        return go;
    }

    void SetFaceAlpha(GameObject go, float a)
    {
        if (scarePrefab != null) return;
        go.GetComponent<Renderer>().material.color = new Color(1, 1, 1, a);
    }

    void PlaceFace(GameObject go, float dist, float scale, float jitter)
    {
        Vector3 pos = playerCamera.position + playerCamera.forward * dist + Random.insideUnitSphere * jitter;
        Vector3 away = pos - playerCamera.position; away.y = 0f;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(away.sqrMagnitude > .001f ? away : playerCamera.forward, Vector3.up);
        float h = faceHeight * scale;
        go.transform.localScale = scarePrefab != null ? Vector3.one * h : new Vector3(h * aspect, h, 1f);
    }

    GameObject SpawnFlash()
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(playerCamera, false);
        q.transform.localPosition = new Vector3(0, 0, 0.3f);
        q.transform.localScale = Vector3.one * 2f;
        q.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"));
        SetFlash(q, 0f);
        return q;
    }

    static void SetFlash(GameObject q, float a)
    {
        if (q == null) return;
        Color c = Color.Lerp(new Color(.55f, .02f, .04f), new Color(1f, .9f, .7f), a); // 聖なる白金 → 血の赤
        c.a = a; q.GetComponent<Renderer>().material.color = c;
    }

    // ---------- 上位者の顔（手続き生成）----------
    static Color Over(Color d, Color s)
    {
        float a = s.a + d.a * (1 - s.a); if (a <= 0f) return Color.clear;
        return new Color((s.r * s.a + d.r * d.a * (1 - s.a)) / a, (s.g * s.a + d.g * d.a * (1 - s.a)) / a, (s.b * s.a + d.b * d.a * (1 - s.a)) / a, a);
    }
    static float P(float x, float y) => Mathf.PerlinNoise(x, y);
    static float C01(float x) => Mathf.Clamp01(x);

    static Texture2D MakeFace(int n)
    {
        // 余分な目（額・こめかみ・頬）を無数に
        var rnd = new System.Random(13); var eyes = new List<Vector3>();
        while (eyes.Count < 13)
        {
            float ex = .5f + ((float)rnd.NextDouble() - .5f) * .30f, ey = .47f + (float)rnd.NextDouble() * .38f;
            float fe = (ex - .5f) * (ex - .5f) / .0289f + (ey - .6f) * (ey - .6f) / .1296f;
            if (fe > .72f || (Mathf.Abs(ey - .665f) < .07f && Mathf.Abs(Mathf.Abs(ex - .5f) - .065f) < .055f)) continue;
            eyes.Add(new Vector3(ex, ey, .013f + (float)rnd.NextDouble() * .012f));
        }

        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f), dx = u - .5f;
                Color c = Color.clear;

                // 後光：光の輪と放射状の光条
                float hd = Mathf.Sqrt(dx * dx + (v - .64f) * (v - .64f));
                c = Over(c, new Color(1, .82f, .5f, Mathf.Pow(Mathf.Max(0, .30f * (1 - hd / .5f)), 1.5f)));
                float r1 = Mathf.Abs(hd - .40f); if (r1 < .02f) c = Over(c, new Color(1, .9f, .62f, Mathf.Pow(1 - r1 / .02f, 2) * .9f));
                float r2 = Mathf.Abs(hd - .355f); if (r2 < .006f) c = Over(c, new Color(1, .9f, .62f, (1 - r2 / .006f) * .6f));
                float ang = Mathf.Atan2(v - .64f, dx);
                c = Over(c, new Color(1, .88f, .6f, Mathf.Pow(Mathf.Max(0, Mathf.Sin(ang * 24f)), 12f) * Mathf.Max(0, 1 - hd / .5f) * .5f));

                // ベールとローブ（黒い布の襞・金の縁取り・血の染み）
                float d1 = dx * dx / .0729f + (v - .55f) * (v - .55f) / .2209f;
                float d2 = dx * dx / .2025f + (v - .15f) * (v - .15f) / .1024f;
                float m = Mathf.Min(d1, d2);
                if (m < 1f)
                {
                    float b = .04f + .05f * P(u * 8, v * 3) + .03f * Mathf.Sin(u * 70f + P(u * 5, v * 5) * 6f);
                    Color h = new Color(b * 1.1f, b * .8f, b * .9f, C01((1 - m) * 10f));
                    if (m > .88f && m < .9f) h = Color.Lerp(h, new Color(.9f, .72f, .35f, 1), .6f);
                    if (P(u * 6 + 9, v * 6) > .62f) h = Color.Lerp(h, new Color(.35f, .02f, .03f, h.a), .6f);
                    c = Over(c, h);
                }

                // 顔：細長い青白い蝋のような肌、血管
                float fe = dx * dx / .0289f + (v - .60f) * (v - .60f) / .1296f;
                if (fe < 1f)
                {
                    float s = (1f - .55f * Mathf.Pow(fe, 1.5f)) * (.9f + .2f * P(u * 22, v * 22));
                    Color sk = new Color(.88f * s, .85f * s, .78f * s, C01((1 - fe) * 16f));
                    float vn = Mathf.Abs(P(u * 26 + 4, v * 26) - .5f);
                    if (vn < .018f) sk = Color.Lerp(sk, new Color(.55f, .12f, .14f, sk.a), (1 - vn / .018f) * .55f);
                    c = Over(c, sk);

                    // 無数の目
                    foreach (var e in eyes)
                    {
                        float d = Mathf.Sqrt((u - e.x) * (u - e.x) + (v - e.y) * (v - e.y));
                        if (d < e.z * 1.3f) c = Over(c, new Color(.55f, .1f, .1f, C01((e.z * 1.3f - d) / (e.z * .3f)) * .9f));
                        if (d < e.z) c = Over(c, new Color(.93f, .9f, .8f, C01((e.z - d) / (e.z * .15f))));
                        if (d < e.z * .45f) c = Over(c, new Color(.04f, .02f, .02f, 1));
                    }
                    // 大きな眼窩（黒い虚ろ）
                    foreach (float ex in new[] { .435f, .565f })
                    {
                        float ee = (u - ex) * (u - ex) / .001024f + (v - .665f) * (v - .665f) / .0025f;
                        if (ee < 1f) c = Over(c, new Color(0, 0, 0, C01((1 - ee) * 5f)));
                        if ((u - ex) * (u - ex) + (v - .665f) * (v - .665f) < .00004f) c = Over(c, new Color(1, .95f, .9f, 1));
                    }
                    // 裂けた縦長の口と針のような歯
                    float me = dx * dx / .003f + (v - .27f) * (v - .27f) / .0289f;
                    if (me < 1f)
                    {
                        c = Over(c, new Color(.05f, 0, 0, C01((1 - me) * 8f)));
                        if (me > .55f && Mathf.Sin(v * 160f) > .2f) c = Over(c, new Color(.9f, .88f, .8f, .9f));
                    }
                }

                // 血の涙・口から垂れる血
                if (c.a > .5f)
                {
                    foreach (float ex in new[] { .435f, .565f })
                        if (v < .64f && v > .18f)
                        {
                            float w = (P(v * 20, ex * 50) - .5f) * .012f, d = u - ex - w;
                            c = Over(c, new Color(.42f, .02f, .03f, Mathf.Exp(-(d / .007f) * (d / .007f)) * C01(.95f - (.64f - v) * 1.3f) * .9f));
                        }
                    foreach (float off in new[] { -.02f, 0f, .028f })
                        if (v < .10f && v > .02f)
                        {
                            float d = u - .5f - off - (P(v * 25, off * 70) - .5f) * .01f;
                            c = Over(c, new Color(.4f, .02f, .03f, Mathf.Exp(-(d / .006f) * (d / .006f)) * C01((v - .02f) / .08f) * .85f));
                        }
                }
                px[y * n + x] = c;
            }
        tex.SetPixels(px); tex.Apply();
        return tex;
    }

    // ---------- 音 ----------
    void PlayClip(AudioClip clip, float vol)
    {
        var go = new GameObject("ScareAudio");
        go.transform.position = playerCamera.position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip; src.spatialBlend = 0f; src.volume = vol; src.Play();
        Destroy(go, clip.length + .5f);
    }

    // 遠くで鳴る鐘（非整数倍音）
    static AudioClip MakeBell()
    {
        const int sr = 44100; var d = new float[sr * 4];
        float[] R = { .56f, .92f, 1.19f, 1.71f, 2f, 2.74f, 3f, 3.76f }, A = { 1, .7f, .55f, .45f, .5f, .3f, .25f, .18f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)sr, s = 0;
            for (int k = 0; k < 8; k++) s += A[k] * Mathf.Sin(6.283f * 196f * R[k] * t) * Mathf.Exp(-t * (.7f + k * .35f));
            d[i] = (float)System.Math.Tanh(s * .35f) * Mathf.Min(1f, t / .003f);
        }
        var c = AudioClip.Create("Bell", d.Length, 1, sr, false); c.SetData(d, 0); return c;
    }

    // 聖歌のような不協和音＋金切り声＋重低音＋湿った破裂音
    static AudioClip MakeHit()
    {
        const int sr = 44100; var d = new float[(int)(sr * 2.4f)];
        float[] voices = { 110f, 164.8f, 220f, 233.1f, 329.6f };      // 220と233.1：短2度のぶつかり
        float ph = 0, lp = 0; var rng = new System.Random(5);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)sr, choir = 0;
            foreach (float f in voices)
            {
                float vib = 1f + .006f * Mathf.Sin(6.283f * 5.2f * t + f);
                for (int h = 1; h <= 3; h++) choir += Mathf.Sin(6.283f * f * vib * h * t) / h;
            }
            choir *= Mathf.Exp(-t * 1.0f) * .12f;
            ph += (900f + 1500f * Mathf.Min(1f, t / .7f)) / sr;
            float nz = (float)(rng.NextDouble() * 2 - 1);
            float shriek = (float)System.Math.Tanh(3f * (((ph % 1f) * 2f - 1f) * .7f + nz * .5f)) * Mathf.Exp(-t * 2.4f) * .55f;
            float boom = Mathf.Sin(6.283f * 40f * t) * Mathf.Exp(-t * 4f) * 1.3f;
            lp += (nz - lp) * .08f;
            float wet = lp * Mathf.Exp(-t * 9f) * 1.5f;
            d[i] = (float)System.Math.Tanh((choir + shriek + boom + wet) * Mathf.Min(1f, t / .004f) * 1.1f);
        }
        var c = AudioClip.Create("Hit", d.Length, 1, sr, false); c.SetData(d, 0); return c;
    }

    static void Haptics(float amp, float dur)
    {
        foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand })
        {
            var dev = InputDevices.GetDeviceAtXRNode(node);
            if (dev.isValid && dev.TryGetHapticCapabilities(out var cap) && cap.supportsImpulse) dev.SendHapticImpulse(0, amp, dur);
        }
    }
}
