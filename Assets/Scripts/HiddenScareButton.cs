using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 隠しボタン。教室のどこかに小さく置く。押す（トリガー / デスクトップ左クリック）とジャンプスケア発動。
/// XRSimpleInteractable と Collider を同じオブジェクトに付ける。
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class HiddenScareButton : MonoBehaviour
{
    [SerializeField] JumpScareController scare;
    [SerializeField, Tooltip("カチッという小さな音（任意）")] AudioClip clickSound;
    [SerializeField, Tooltip("押したら動くボタン部分（任意）")] Transform pressVisual;
    [SerializeField] float pressDepth = 0.01f;
    [SerializeField, Tooltip("一度押したら二度と反応しない")] bool once = true;

    XRSimpleInteractable interactable;
    bool used;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();
    void OnEnable() => interactable.selectEntered.AddListener(OnSelect);
    void OnDisable() => interactable.selectEntered.RemoveListener(OnSelect);

    void OnSelect(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _) => Press();

    /// <summary>VR・デスクトップ共通の押下処理</summary>
    public void Press()
    {
        if ((once && used) || scare == null || scare.IsPlaying) return;
        used = true;
        if (clickSound != null) AudioSource.PlayClipAtPoint(clickSound, transform.position, 0.5f);
        if (pressVisual != null) pressVisual.localPosition += Vector3.back * pressDepth;
        scare.Trigger();
    }
}
