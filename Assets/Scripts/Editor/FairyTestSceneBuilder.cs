#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メニュー Tools > Fairy Hide&Seek > Create Test Scene で、テストに必要なものを一括作成する。
///  ・XR Origin（Starter Assets のプレハブがあればそれ、なければ WASDテスト用の簡易リグ）
///  ・元からある Main Camera を無効化（カメラの二重表示を防ぐ）
///  ・床（下に何も無いとき）  ・FairyHideSeekManager  ・妖精3体
///  ・DesktopTestController の参照設定（Mode = ForceOn）
/// </summary>
public static class FairyTestSceneBuilder
{
    [MenuItem("Tools/Fairy Hide&Seek/Create Test Scene (Rig + Floor + Fairies)")]
    static void Create()
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        // ---- 1. リグ ----
        string path = AssetDatabase.FindAssets("XR Origin t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == "XR Origin (XR Rig)");

        GameObject rig; Camera cam; string rigKind;
        if (path != null)
        {
            rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            cam = rig.GetComponentInChildren<Camera>();
            rigKind = "Starter Assets の XR Origin (XR Rig)";
        }
        else
        {
            rig = new GameObject("XR Origin (Desktop Test)");
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(offset.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            cam = camGo.GetComponent<Camera>();
            rigKind = "簡易リグ（Starter Assetsのプレハブが見つからなかったため）";
        }
        Undo.RegisterCreatedObjectUndo(rig, "Create Rig");
        rig.transform.position = Vector3.zero;
        cam.gameObject.tag = "MainCamera";

        // ---- 2. 古い Main Camera を無効化 ----
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c != cam && !c.transform.IsChildOf(rig.transform) && c.gameObject.CompareTag("MainCamera"))
            {
                Undo.RecordObject(c.gameObject, "Disable old camera");
                c.gameObject.SetActive(false);
                Debug.Log($"[TestScene] 元のカメラ '{c.name}' を無効化しました。");
            }

        // ---- 3. CharacterController と DesktopTestController ----
        var cc = rig.GetComponent<CharacterController>();
        if (cc == null) cc = rig.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.2f; cc.center = new Vector3(0, 0.9f, 0);

        var dtc = rig.GetComponent<DesktopTestController>();
        if (dtc == null) dtc = rig.AddComponent<DesktopTestController>();
        var so = new SerializedObject(dtc);
        so.FindProperty("mode").enumValueIndex = 1;                       // ForceOn
        so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        so.FindProperty("cameraPoseDriver").objectReferenceValue =
            cam.GetComponents<Behaviour>().FirstOrDefault(b => b != null && b.GetType().Name == "TrackedPoseDriver");
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- 4. 床（足元に何も無いとき）----
        if (!Physics.Raycast(new Vector3(0, 0.5f, 0), Vector3.down, 5f))
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TestFloor"; floor.transform.localScale = new Vector3(4, 1, 4);
            Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
        }

        // ---- 5. ゲーム管理・妖精 ----
        if (Object.FindFirstObjectByType<FairyHideSeekManager>() == null)
        {
            var m = new GameObject("GameManager", typeof(FairyHideSeekManager));
            Undo.RegisterCreatedObjectUndo(m, "Create Manager");
        }
        CreateFairy("Fairy_1", new Vector3(3f, 1.2f, 5f));
        CreateFairy("Fairy_2", new Vector3(-4f, 1.0f, 7f));
        CreateFairy("Fairy_3", new Vector3(1f, 1.5f, 10f));

        Selection.activeGameObject = rig;
        EditorSceneManager.MarkSceneDirty(rig.scene);
        Undo.CollapseUndoOperations(group);
        Debug.Log($"[TestScene] 作成完了：{rigKind} / 妖精3体。再生 → Game画面をクリック → WASDとマウスで動けます。");
    }

    static void CreateFairy(string name, Vector3 pos)
    {
        var root = new GameObject(name);
        root.transform.position = pos;
        root.AddComponent<SphereCollider>().radius = 0.35f;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.name = "Visual";
        vis.transform.SetParent(root.transform, false);
        vis.transform.localScale = Vector3.one * 0.25f;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", new Color(1f, 0.9f, 0.4f));
        mpb.SetColor("_Color", new Color(1f, 0.9f, 0.4f));
        vis.GetComponent<Renderer>().SetPropertyBlock(mpb);

        var lightGo = new GameObject("Glow", typeof(Light));
        lightGo.transform.SetParent(root.transform, false);
        var l = lightGo.GetComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.9f, 0.5f); l.range = 2.5f; l.intensity = 1.5f;

        var ft = root.AddComponent<FairyTarget>();            // XRSimpleInteractable も自動で付く
        var so = new SerializedObject(ft);
        so.FindProperty("fairyVisual").objectReferenceValue = vis;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root, "Create Fairy");
    }
}
#endif
