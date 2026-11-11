using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

/// <summary>
/// XR Origin にアタッチ。移動・回転の快適設定をまとめて適用する。
/// （Continuous Move / Snap Turn Provider は XR Origin Rig プレハブに既に付いている想定）
/// </summary>
public class PlayerLocomotionSetup : MonoBehaviour
{
    [Header("Providers")]
    [SerializeField] ContinuousMoveProvider moveProvider;
    [SerializeField] SnapTurnProvider snapTurnProvider;

    [Header("Comfort Settings")]
    [SerializeField, Tooltip("歩く速度(m/s)。VR酔い対策で遅めが無難")]
    float moveSpeed = 1.5f;
    [SerializeField, Tooltip("スナップターンの角度")]
    float snapTurnAngle = 45f;
    [SerializeField, Tooltip("重力ON。床コライダー必須")]
    bool useGravity = true;

    void Reset()
    {
        moveProvider = GetComponentInChildren<ContinuousMoveProvider>();
        snapTurnProvider = GetComponentInChildren<SnapTurnProvider>();
    }

    void Awake()
    {
        if (moveProvider != null)
        {
            moveProvider.moveSpeed = moveSpeed;
            moveProvider.useGravity = useGravity;
            moveProvider.enableFly = false; // 空を飛ばない
        }

        if (snapTurnProvider != null)
        {
            snapTurnProvider.turnAmount = snapTurnAngle;
        }
    }
}
