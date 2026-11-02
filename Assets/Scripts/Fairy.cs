using UnityEngine;

public enum NpcState { Disguised, Revealed, Captured }

public class Fairy : MonoBehaviour
{
    public GameObject disguise;   // ‹[‘Ô’†‚ÌŒ©‚½–Ú
    public GameObject trueForm;   // ³‘Ì
    public NpcState state = NpcState.Disguised;
    public System.Action<Fairy> onCaptured;

    void Start() { Apply(); }

    public void Reveal()
    {
        if (state != NpcState.Disguised) return;
        state = NpcState.Revealed;
        Apply();
    }

    public void Capture()
    {
        if (state != NpcState.Revealed) return;
        state = NpcState.Captured;
        onCaptured?.Invoke(this);
        gameObject.SetActive(false);
    }

    void Apply()
    {
        disguise.SetActive(state == NpcState.Disguised);
        trueForm.SetActive(state == NpcState.Revealed);
    }
}