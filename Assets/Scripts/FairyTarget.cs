using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// 隠れている妖精。XRSimpleInteractable と同じオブジェクトにアタッチ。
/// レイを向けるとハイライト、トリガーで「見つけた！」→ パーティクル再生。
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class FairyTarget : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField, Tooltip("見つけた瞬間に消す妖精モデル")]
    GameObject fairyVisual;
    [SerializeField, Tooltip("レイが当たっている間だけ表示する光など（任意）")]
    GameObject hoverHighlight;

    [Header("Found Effect")]
    [SerializeField, Tooltip("発見時に生成する Particle System のプレハブ")]
    GameObject foundEffectPrefab;
    [SerializeField] float effectLifetime = 3f;
    [SerializeField] AudioClip foundSound;

    [Header("Haptics")]
    [SerializeField, Range(0f, 1f)] float hapticAmplitude = 0.6f;
    [SerializeField] float hapticDuration = 0.2f;

    public bool IsFound { get; private set; }
    public event Action<FairyTarget> Found;

    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (hoverHighlight != null) hoverHighlight.SetActive(false);
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnHoverEntered(HoverEnterEventArgs args) => SetHighlight(true);
    void OnHoverExited(HoverExitEventArgs args) => SetHighlight(false);

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        Discover(args.interactorObject as XRBaseInputInteractor);
    }

    /// <summary>ハイライトの切り替え（VR・デスクトップテスト共通）</summary>
    public void SetHighlight(bool on)
    {
        if (hoverHighlight == null) return;
        hoverHighlight.SetActive(on && !IsFound);
    }

    /// <summary>
    /// 発見処理。VRではXRIのSelectから、デスクトップテストではクリックから呼ばれる。
    /// </summary>
    public void Discover(XRBaseInputInteractor interactor = null)
    {
        if (IsFound) return;
        IsFound = true;

        // コントローラーを震わせる（VR時のみ）
        if (interactor != null)
            interactor.SendHapticImpulse(hapticAmplitude, hapticDuration);

        // エフェクト＆サウンド
        if (foundEffectPrefab != null)
        {
            var fx = Instantiate(foundEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, effectLifetime);
        }
        if (foundSound != null)
            AudioSource.PlayClipAtPoint(foundSound, transform.position);

        // 見た目を消して再選択できないようにする
        SetHighlight(false);
        if (fairyVisual != null) fairyVisual.SetActive(false);
        interactable.enabled = false;

        Found?.Invoke(this);
    }

    /// <summary>リスタート用</summary>
    public void ResetFairy()
    {
        IsFound = false;
        interactable.enabled = true;
        if (fairyVisual != null) fairyVisual.SetActive(true);
    }
}
