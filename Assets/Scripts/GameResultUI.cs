using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 妖精の森 風 結果画面。World Space Canvas のパネルにアタッチ。
///  やわらかく色づく → 光のもやが広がる → カードがふわっと浮かぶ
///  → きらきらパーティクル → 少し遅れて「もういちど さがす」
/// 再開：ボタン(VRはレイ) または Rキー。
/// ⋮ メニューの Preview 〜 で再生せずに確認できる。
/// </summary>
public class GameResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] FairyHideSeekManager manager;
    [SerializeField] CanvasGroup panel;
    [SerializeField, Tooltip("全体の色づけ用Image（パネル全体）")] Image tintImage;
    [SerializeField, Tooltip("カード背後の光のもや（円形グラデーション画像）")] Image glowImage;
    [SerializeField, Tooltip("タイトル等をまとめたカード")] CanvasGroup card;
    [SerializeField] TMP_Text titleText, subText;
    [SerializeField, Tooltip("再開ボタンをまとめたCanvasGroup")] CanvasGroup promptGroup;
    [SerializeField] Button restartButton;
    [SerializeField, Tooltip("妖精・ほたる等のエフェクト（FairyForestFX）")] FairyForestFX fx;

    [Header("Text")]
    [SerializeField] string clearTitle = "妖精たちを見つけた！";
    [SerializeField] string failTitle = "妖精たちは\n森へ帰っていった…";

    [Header("Colors (クリア = 黄金の木漏れ日 / 失敗 = 月夜の森)")]
    [SerializeField] Color clearTint = new Color(0.10f, 0.30f, 0.22f);
    [SerializeField] Color clearAccent = new Color(1f, 0.91f, 0.66f);
    [SerializeField] Color failTint = new Color(0.10f, 0.14f, 0.34f);
    [SerializeField] Color failAccent = new Color(0.84f, 0.89f, 1f);

    [Header("Timing")]
    [SerializeField] float tintTime = 1.6f;
    [SerializeField] float cardDelay = 0.3f;
    [SerializeField] float cardTime = 1.6f;
    [SerializeField] float promptDelay = 3f;
    [SerializeField, Range(0f, 1f)] float tintAlpha = 0.72f;
    [SerializeField] float bobHeight = 6f;

    const string BestKey = "FairyBestTime";
    bool showing, canRestart;
    Vector2 cardBasePos;

    void Awake()
    {
        if (card != null) cardBasePos = ((RectTransform)card.transform).anchoredPosition;
        ResetVisuals();
        if (glowImage != null && glowImage.sprite == null) glowImage.sprite = FairyForestFX.SoftCircle;
        if (restartButton != null) restartButton.onClick.AddListener(TryRestart);
    }

    void OnEnable()
    {
        if (manager == null) return;
        manager.onGameClear.AddListener(ShowClear);
        manager.onTimeUp.AddListener(ShowTimeUp);
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.onGameClear.RemoveListener(ShowClear);
        manager.onTimeUp.RemoveListener(ShowTimeUp);
    }

    void Update()
    {
        if (showing && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            TryRestart();
    }

    public void ShowClear() =>
        Show(true, manager.FoundCount, manager.TotalCount, manager.TimeLimit - manager.Remaining);

    public void ShowTimeUp() =>
        Show(false, manager.FoundCount, manager.TotalCount, manager.TimeLimit);

    void Show(bool clear, int found, int total, float elapsed)
    {
        Color tint = clear ? clearTint : failTint;
        Color accent = clear ? clearAccent : failAccent;
        titleText.text = clear ? clearTitle : failTitle;
        titleText.color = accent;
        if (fx != null) fx.Play(clear, accent);
        if (glowImage != null) { var g = accent; g.a = 0f; glowImage.color = g; }
        if (tintImage != null) { tint.a = 0f; tintImage.color = tint; }

        string line = $"妖精　{found} / {total}　　{elapsed:F1} 秒";
        float best = PlayerPrefs.GetFloat(BestKey, float.MaxValue);
        if (clear && elapsed < best)
        {
            PlayerPrefs.SetFloat(BestKey, elapsed);
            PlayerPrefs.Save();
            line += "　　NEW RECORD";
        }
        subText.text = line;

        showing = true; canRestart = false;
        StopAllCoroutines();
        StartCoroutine(Sequence(clear));
    }

    IEnumerator Sequence(bool clear)
    {
        panel.interactable = true; panel.blocksRaycasts = true;
        var cardRt = (RectTransform)card.transform;

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;

            float a = Smooth(t / tintTime);
            SetAlpha(tintImage, a * tintAlpha);
            SetAlpha(glowImage, a * 0.45f);

            // カードがふわっと浮かび上がる
            float c = Smooth((t - cardDelay) / cardTime);
            card.alpha = c;
            float rise = (1f - c) * -30f;
            float bob = t > cardDelay + cardTime ? Mathf.Sin(t * 1.6f) * bobHeight : 0f;
            cardRt.anchoredPosition = cardBasePos + new Vector2(0f, rise + bob);
            cardRt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, c);

            if (t >= promptDelay)
            {
                SetGroup(promptGroup, Smooth((t - promptDelay) / 1.4f), true);
                canRestart = true;
            }
            yield return null;
        }
    }

    void TryRestart()
    {
        if (!canRestart) return;
        showing = false; canRestart = false;
        StopAllCoroutines();
        if (fx != null) fx.Stop();
        StartCoroutine(Hide());
        manager.StartGame();
    }

    IEnumerator Hide()
    {
        panel.interactable = false; panel.blocksRaycasts = false;
        float t = 0f; const float dur = 0.8f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            panel.alpha = 1f - t / dur;
            yield return null;
        }
        ResetVisuals();
    }

    void ResetVisuals()
    {
        panel.alpha = 1f; panel.interactable = false; panel.blocksRaycasts = false;
        SetAlpha(tintImage, 0f); SetAlpha(glowImage, 0f);
        if (card != null)
        {
            card.alpha = 0f;
            ((RectTransform)card.transform).anchoredPosition = cardBasePos;
            card.transform.localScale = Vector3.one;
        }
        SetGroup(promptGroup, 0f, false);
    }

    static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color; c.a = a; img.color = c;
    }

    static void SetGroup(CanvasGroup g, float a, bool on)
    {
        if (g == null) return;
        g.alpha = a; g.interactable = on; g.blocksRaycasts = on;
    }

    [ContextMenu("Preview Clear")] void PreviewClear() => Show(true, 5, 5, 87.3f);
    [ContextMenu("Preview Time Up")] void PreviewTimeUp() => Show(false, 3, 5, 180f);
}
