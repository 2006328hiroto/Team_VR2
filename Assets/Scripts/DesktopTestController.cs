using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// VRヘッドセットなしでテストするためのデスクトップ操作。
///   WASD     : 移動
///   Shift    : ダッシュ
///   マウス    : 視点回転
///   左クリック : 妖精を見つける（VRのレイ＋トリガー相当）
///   Esc      : カーソル解除 / 画面クリックで再ロック
/// XR Origin にアタッチ。VR機器が接続されているときは自動で無効になる。
/// </summary>
public class DesktopTestController : MonoBehaviour
{
    public enum Mode { Auto, ForceOn, ForceOff }

    [Header("Mode")]
    [SerializeField, Tooltip("Auto: VR機器が無いときだけ有効")]
    Mode mode = Mode.Auto;

    [Header("References")]
    [SerializeField, Tooltip("XR Origin 内の Main Camera")]
    Transform cameraTransform;
    [SerializeField, Tooltip("カメラに付いている TrackedPoseDriver（デスクトップ時は無効化する）")]
    Behaviour cameraPoseDriver;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float runSpeed = 3f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField, Tooltip("デスクトップ時の目の高さ(m)")]
    float eyeHeight = 1.6f;

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float pitchLimit = 80f;

    [Header("Interaction")]
    [SerializeField] float interactDistance = 10f;
    [SerializeField] LayerMask interactMask = ~0;

    CharacterController controller;
    bool active, warnedKeyboard;
    float pitch;
    float verticalVelocity;
    FairyTarget hovered;

    void Reset()
    {
        var cam = GetComponentInChildren<Camera>();
        if (cam != null) cameraTransform = cam.transform;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        active = mode == Mode.ForceOn ||
                 (mode == Mode.Auto && !XRSettings.isDeviceActive);

        if (!active)
        {
            Debug.Log("[DesktopTest] 無効のまま：VR機器を検出したか Mode が ForceOff です。Mode を ForceOn にしてください。");
            enabled = false;
            return;
        }

        // カメラを上書きする TrackedPoseDriver を自動で止める（手動で設定しなくてよい）
        if (cameraTransform != null)
            foreach (var b in cameraTransform.GetComponents<Behaviour>())
                if (b.GetType().Name.Contains("TrackedPoseDriver")) b.enabled = false;
        if (cameraPoseDriver != null) cameraPoseDriver.enabled = false;
        pitch = 0f;
        LockCursor(true);
        Debug.Log("[DesktopTest] デスクトップテストモード有効（WASD / マウス / 左クリック）");
    }

    void Update()
    {
        if (Keyboard.current == null && !warnedKeyboard)
        {
            warnedKeyboard = true;
            Debug.LogWarning("[DesktopTest] キーボードを検出できません。Project Settings > Player > Active Input Handling を Input System Package (New) か Both にしてください。");
        }

        // カーソル解除中は画面クリックで再ロックするだけ
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor(true);
            return;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
            return;
        }

        Look();
        Move();
        UpdateHoverAndClick();
    }

    void LateUpdate()
    {
        if (!active || cameraTransform == null) return;
        // TrackedPoseDriver を止めているのでカメラ姿勢を自前で決める
        cameraTransform.localPosition = new Vector3(0f, eyeHeight, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Look()
    {
        if (Mouse.current == null) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        transform.Rotate(0f, delta.x, 0f, Space.World);
        pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
    }

    void Move()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1f;
        if (kb.sKey.isPressed) input.y -= 1f;
        if (kb.dKey.isPressed) input.x += 1f;
        if (kb.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        float speed = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

        if (controller != null)
        {
            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }
        else
        {
            transform.position += move * Time.deltaTime;
        }
    }

    void UpdateHoverAndClick()
    {
        if (cameraTransform == null) return;

        FairyTarget target = null;
        HiddenScareButton scareBtn = null;
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out var hit, interactDistance, interactMask,
                            QueryTriggerInteraction.Collide))
        {
            target = hit.collider.GetComponentInParent<FairyTarget>();
            scareBtn = hit.collider.GetComponentInParent<HiddenScareButton>();
        }

        if (target != hovered)
        {
            if (hovered != null) hovered.SetHighlight(false);
            hovered = target;
            if (hovered != null) hovered.SetHighlight(true);
        }

        if (hovered != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            hovered.Discover();
            hovered = null;
        }

        if (scareBtn != null && Mouse.current.leftButton.wasPressedThisFrame)
            scareBtn.Press();
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // 画面中央の照準とヘルプ表示（エディタ・開発ビルドのみ）
    void OnGUI()
    {
        if (!active) return;
        float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
        GUI.Box(new Rect(cx - 2, cy - 2, 4, 4), GUIContent.none);
        GUI.Label(new Rect(10, 10, 420, 24),
            "WASD:移動  Shift:ダッシュ  マウス:視点  左クリック:見つける  Esc:カーソル解除");
    }
}
