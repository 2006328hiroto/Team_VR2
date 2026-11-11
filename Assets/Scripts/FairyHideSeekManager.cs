using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ゲーム進行：制限時間、発見数カウント、クリア/タイムアップ判定。
/// </summary>
public class FairyHideSeekManager : MonoBehaviour
{
    [Header("Fairies")]
    [SerializeField, Tooltip("空ならシーン内の FairyTarget を自動で集める")]
    FairyTarget[] fairies;

    [Header("Rules")]
    [SerializeField] float timeLimit = 180f;
    [SerializeField] bool autoStart = true;

    [Header("UI (World Space Canvas など)")]
    [SerializeField] TMP_Text statusText;

    [Header("Events")]
    public UnityEvent onGameStart;
    public UnityEvent onFairyFound;
    public UnityEvent onGameClear;
    public UnityEvent onTimeUp;

    float remaining;
    int foundCount;
    bool playing;

    public int FoundCount => foundCount;
    public int TotalCount => fairies != null ? fairies.Length : 0;
    public float Remaining => remaining;
    public float TimeLimit => timeLimit;

    void Awake()
    {
        if (fairies == null || fairies.Length == 0)
            fairies = FindObjectsByType<FairyTarget>(FindObjectsSortMode.None);
    }

    void OnEnable()
    {
        foreach (var f in fairies) if (f != null) f.Found += OnFairyFound;
    }

    void OnDisable()
    {
        foreach (var f in fairies) if (f != null) f.Found -= OnFairyFound;
    }

    void Start()
    {
        if (autoStart) StartGame();
        else UpdateUI();
    }

    public void StartGame()
    {
        foreach (var f in fairies) f.ResetFairy();
        foundCount = 0;
        remaining = timeLimit;
        playing = true;
        onGameStart?.Invoke();
        UpdateUI();
    }

    void Update()
    {
        if (!playing) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            playing = false;
            onTimeUp?.Invoke();
            SetText($"タイムアップ！\n{foundCount} / {fairies.Length} 匹");
            return;
        }
        UpdateUI();
    }

    void OnFairyFound(FairyTarget fairy)
    {
        if (!playing) return;
        foundCount++;
        onFairyFound?.Invoke();

        if (foundCount >= fairies.Length)
        {
            playing = false;
            onGameClear?.Invoke();
            SetText($"全員見つけた！\n残り {remaining:F0} 秒");
        }
        else UpdateUI();
    }

    void UpdateUI()
    {
        SetText($"妖精 {foundCount} / {fairies.Length}\n残り {Mathf.CeilToInt(remaining)} 秒");
    }

    void SetText(string s)
    {
        if (statusText != null) statusText.text = s;
    }
}
