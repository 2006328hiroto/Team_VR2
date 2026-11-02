using UnityEngine;
using UnityEngine.InputSystem;

public class ClickTest : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var fairy = hit.collider.GetComponentInParent<Fairy>();
                if (fairy == null) return;
                if (fairy.state == NpcState.Disguised) fairy.Reveal();
                else if (fairy.state == NpcState.Revealed) fairy.Capture();
            }
        }
    }
}