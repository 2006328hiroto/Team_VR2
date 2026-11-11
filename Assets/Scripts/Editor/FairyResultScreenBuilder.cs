#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メニュー Tools > Fairy Hide&Seek > Create Result Screen で、
/// 結果画面（Canvas・パネル・テキスト・ボタン・エフェクト）をシーンに自動作成し、
/// GameResultUI の参照も全部つなぐ。
/// </summary>
public static class FairyResultScreenBuilder
{
    [MenuItem("Tools/Fairy Hide&Seek/Create Result Screen")]
    static void Create()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<FairyHideSeekManager>();

        // ---- Canvas (World Space) ----
        var root = new GameObject("ResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create Result Screen");
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var crt = (RectTransform)root.transform;
        crt.sizeDelta = new Vector2(1280, 720);
        crt.localScale = Vector3.one * 0.0015f;
        crt.position = new Vector3(0f, 1.5f, 2f);

        // VR用：XRIのTrackedDeviceGraphicRaycasterがあれば差し替える（レイでボタンを押せる）
        var xrType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                              .FirstOrDefault(t => t.Name == "TrackedDeviceGraphicRaycaster");
        if (xrType != null)
        {
            UnityEngine.Object.DestroyImmediate(root.GetComponent<GraphicRaycaster>());
            root.AddComponent(xrType);
        }

        // ---- Panel ----
        var panel = NewUI("Panel", root.transform, typeof(CanvasGroup));
        Stretch(panel);
        var ui = panel.gameObject.AddComponent<GameResultUI>();

        var tint = NewUI("Tint", panel, typeof(Image)); Stretch(tint);
        tint.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        tint.GetComponent<Image>().raycastTarget = false;

        var glow = NewUI("Glow", panel, typeof(Image)); glow.sizeDelta = new Vector2(1000, 700);
        glow.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        glow.GetComponent<Image>().raycastTarget = false;

        var fxRt = NewUI("FairyForestFX", panel); Stretch(fxRt);
        var fx = fxRt.gameObject.AddComponent<FairyForestFX>();

        // ---- Card ----
        var card = NewUI("Card", panel, typeof(CanvasGroup)); card.sizeDelta = new Vector2(760, 420);
        var title = NewText("Title", card, "妖精たちを見つけた！", 58, new Vector2(0, 90), new Vector2(720, 160));
        var sub = NewText("Sub", card, "妖精　0 / 0　　0.0 秒", 30, new Vector2(0, -40), new Vector2(720, 50));

        var prompt = NewUI("PromptGroup", card, typeof(CanvasGroup));
        prompt.anchoredPosition = new Vector2(0, -130); prompt.sizeDelta = new Vector2(420, 70);
        var btnImg = prompt.gameObject.AddComponent<Image>();
        btnImg.color = new Color(1, 1, 1, 0.14f);
        var btn = prompt.gameObject.AddComponent<Button>(); btn.targetGraphic = btnImg;
        var label = NewText("Label", prompt, "R　もういちど さがす", 26, Vector2.zero, new Vector2(420, 70));
        label.raycastTarget = false;

        // ---- 参照をつなぐ ----
        var so = new SerializedObject(ui);
        so.FindProperty("manager").objectReferenceValue = manager;
        so.FindProperty("panel").objectReferenceValue = panel.GetComponent<CanvasGroup>();
        so.FindProperty("tintImage").objectReferenceValue = tint.GetComponent<Image>();
        so.FindProperty("glowImage").objectReferenceValue = glow.GetComponent<Image>();
        so.FindProperty("card").objectReferenceValue = card.GetComponent<CanvasGroup>();
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("subText").objectReferenceValue = sub;
        so.FindProperty("promptGroup").objectReferenceValue = prompt.GetComponent<CanvasGroup>();
        so.FindProperty("restartButton").objectReferenceValue = btn;
        so.FindProperty("fx").objectReferenceValue = fx;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        if (manager == null)
            Debug.LogWarning("[FairyResultScreen] FairyHideSeekManager が見つかりません。GameResultUI の Manager を手動で設定してください。");
        Debug.Log("[FairyResultScreen] 作成完了。Panel の GameResultUI を右クリック → Preview Clear / Preview Time Up で確認できます。");
    }

    static RectTransform NewUI(string name, Transform parent, params Type[] comps)
    {
        var go = new GameObject(name, new[] { typeof(RectTransform) }.Concat(comps).ToArray());
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TMP_Text NewText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 box)
    {
        var rt = NewUI(name, parent, typeof(TextMeshProUGUI));
        rt.anchoredPosition = pos; rt.sizeDelta = box;
        var t = rt.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
        return t;
    }
}
#endif
