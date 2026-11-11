using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 妖精の森エフェクト（画像素材なし・すべてコードで生成）。
/// ほたる / きらめき / 羽ばたく妖精 / 妖精の軌跡の粉 を UI Image で描く。
/// 結果画面のパネル内（全面ストレッチ）に置いて、GameResultUI から Play / Stop される。
///  クリア : 妖精たちがカードの周りを飛び回る
///  失敗   : 妖精たちが少しずつ上へ昇り、森へ帰っていく
/// </summary>
public class FairyForestFX : MonoBehaviour
{
    [SerializeField] int fireflyCount = 50, starCount = 14, fairyCount = 4, dustPool = 70;
    [SerializeField, Tooltip("妖精の大きさ（キャンバス単位）")] float fairySize = 11f;
    [SerializeField, Tooltip("1280px幅を基準にした全体スケール")] float scaleMul = 1.5f;

    // ---------- 手続き生成スプライト ----------
    static Sprite soft, wing, star;
    public static Sprite SoftCircle => soft != null ? soft : (soft = MakeSoft());
    static Sprite Wing => wing != null ? wing : (wing = MakeWing());
    static Sprite Star => star != null ? star : (star = MakeStar());

    static Sprite Bake(int w, int h, System.Func<float, float, float> alpha, Vector2 pivot)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1, 1, 1,
                    Mathf.Clamp01(alpha((x + .5f) / w * 2f - 1f, (y + .5f) / h * 2f - 1f))));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 100f);
    }
    static Sprite MakeSoft() => Bake(64, 64, (x, y) =>
    { float d = Mathf.Sqrt(x * x + y * y); float a = 1f - d; return a * a * (0.6f + 0.4f * a); }, new Vector2(.5f, .5f));
    static Sprite MakeWing() => Bake(64, 32, (x, y) =>               // 根元(左端)が支点の楕円
    { float nx = x * .5f + .5f; float d = Mathf.Sqrt(Mathf.Pow(nx * 2f - 1f, 2) + y * y * 1.2f); return Mathf.Pow(1f - d, .6f) * .85f * Mathf.Clamp01(nx * 6f); }, new Vector2(0f, .5f));
    static Sprite MakeStar() => Bake(64, 64, (x, y) =>
    { float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
      float cross = Mathf.Max((1f - ax * 14f) * (1f - ay), (1f - ay * 14f) * (1f - ax));
      float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 3f);
      return Mathf.Max(cross, core); }, new Vector2(.5f, .5f));

    // ---------- 内部データ ----------
    class Fly  { public Image img; public float x, y, r, sp, ph, tw, up; }
    class Sta  { public Image img; public float x, y, s, ph, tw; }
    class Fai  { public Image glow, core, wl, wr; public float ph, sp, rx, ry, sz; }
    class Dust { public Image img; public Vector2 pos, vel; public float life; }

    RectTransform area, root;
    Fly[] flies; Sta[] stars; Fai[] fairies; Dust[] dust;
    int dustIdx;
    bool playing, clear, built;
    float t;
    Color accent = new Color(1f, .9f, .5f);

    public void Play(bool isClear, Color accentColor)
    {
        Build();
        clear = isClear; accent = accentColor; t = 0f;
        foreach (var d in dust) { d.life = 0f; d.img.enabled = false; }
        root.gameObject.SetActive(true);
        playing = true;
    }

    public void Stop()
    {
        playing = false;
        if (root != null) root.gameObject.SetActive(false);
    }

    void Build()
    {
        if (built) return;
        area = (RectTransform)transform;
        root = new GameObject("FX_Root", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(area, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;

        flies = new Fly[fireflyCount];
        for (int i = 0; i < flies.Length; i++)
            flies[i] = new Fly { img = Make("Firefly", SoftCircle), x = Random.value, y = Random.value,
                r = Random.Range(1.2f, 3.2f), sp = Random.Range(.3f, .9f), ph = Random.Range(0f, 6.28f),
                tw = Random.Range(.8f, 2.2f), up = Random.Range(.2f, 1f) };

        stars = new Sta[starCount];
        for (int i = 0; i < stars.Length; i++)
            stars[i] = new Sta { img = Make("Star", Star), x = Random.Range(.05f, .95f), y = Random.Range(.4f, .95f),
                s = Random.Range(3f, 8f), ph = Random.Range(0f, 6.28f), tw = Random.Range(1f, 2.4f) };

        fairies = new Fai[fairyCount];
        for (int i = 0; i < fairies.Length; i++)
            fairies[i] = new Fai { glow = Make("Glow", SoftCircle), wl = Make("WingL", Wing), wr = Make("WingR", Wing),
                core = Make("Core", SoftCircle), ph = i * 1.57f, sp = Random.Range(.35f, .55f),
                rx = Random.Range(.26f, .4f), ry = Random.Range(.18f, .3f), sz = fairySize * Random.Range(.7f, 1f) };

        dust = new Dust[dustPool];
        for (int i = 0; i < dust.Length; i++) dust[i] = new Dust { img = Make("Dust", SoftCircle) };

        built = true;
    }

    Image Make(string n, Sprite s)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
        var img = go.GetComponent<Image>();
        img.sprite = s; img.raycastTarget = false; img.enabled = false;
        return img;
    }

    static void Put(Image img, Vector2 pos, Vector2 size, Color c)
    {
        img.enabled = c.a > 0.01f;
        var rt = img.rectTransform;
        rt.anchoredPosition = pos; rt.sizeDelta = size; img.color = c;
    }

    void Update()
    {
        if (!playing) return;
        float dt = Time.unscaledDeltaTime; t += dt;
        float W = area.rect.width, H = area.rect.height, k = W / 1280f * scaleMul;
        Color a = accent;

        // ほたる：ゆらゆら上へ漂い、明滅
        foreach (var f in flies)
        {
            float x = f.x * W - W / 2 + Mathf.Sin(t * f.sp + f.ph) * 28f * k;
            float y = Mathf.Repeat(f.y * H - t * f.up * 9f * k, H) - H / 2;
            float al = .35f + .65f * Mathf.Abs(Mathf.Sin(t * f.tw + f.ph));
            Put(f.img, new Vector2(x, y), Vector2.one * f.r * 12f * k, new Color(a.r, a.g, a.b, al * .9f));
        }

        // 星のきらめき
        foreach (var s in stars)
        {
            float al = .25f + .75f * Mathf.Abs(Mathf.Sin(t * s.tw + s.ph));
            Put(s.img, new Vector2(s.x * W - W / 2, s.y * H - H / 2), Vector2.one * s.s * 5f * k, new Color(1, 1, 1, al));
        }

        // 妖精：クリア=周回 / 失敗=昇って消える
        float away = clear ? 0f : Mathf.Clamp01((t - 2.5f) / 7f);
        foreach (var f in fairies)
        {
            float ang = t * f.sp + f.ph;
            Vector2 p = new Vector2(Mathf.Cos(ang) * W * f.rx, Mathf.Sin(ang * 1.3f) * H * f.ry + away * away * H * .9f);
            float al = 1f - away * .9f, sz = f.sz * k;
            float flap = Mathf.Abs(Mathf.Sin(t * 20f + f.ph));

            Put(f.glow, p, Vector2.one * sz * 10f, new Color(a.r, a.g, a.b, .55f * al));
            Vector2 wsz = new Vector2(sz * 3.4f, sz * 2.8f * (.4f + .6f * flap));
            Color wc = Color.Lerp(Color.white, a, .25f); wc.a = .8f * al;
            Put(f.wr, p, wsz, wc); Put(f.wl, p, wsz, wc);
            f.wr.rectTransform.localEulerAngles = new Vector3(0, 0, 20f + 35f * flap);
            f.wl.rectTransform.localEulerAngles = new Vector3(0, 0, -(20f + 35f * flap));
            f.wl.rectTransform.localScale = new Vector3(-1, 1, 1);
            Put(f.core, p, Vector2.one * sz * 1.4f, new Color(1, 1, 1, al));

            // 軌跡の粉
            var d = dust[dustIdx = (dustIdx + 1) % dust.Length];
            d.pos = p; d.vel = new Vector2(Random.Range(-8f, 8f), -Random.Range(6f, 22f)) * k; d.life = 1f;
        }

        foreach (var d in dust)
        {
            if (d.life <= 0f) { d.img.enabled = false; continue; }
            d.life -= dt * .75f; d.pos += d.vel * dt;
            Put(d.img, d.pos, Vector2.one * (3f + 3f * d.life) * 4f * k, new Color(a.r, a.g, a.b, Mathf.Max(0, d.life) * .6f));
        }
    }
}
