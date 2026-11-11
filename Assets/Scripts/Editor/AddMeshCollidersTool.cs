#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 選択したオブジェクト（と、その子すべて）のメッシュに MeshCollider をまとめて付ける。
/// 3Dスキャンした教室に「床・壁の当たり判定」を付けるために使う。
/// 使い方：Hierarchy で iict_building などを選ぶ → Tools > Fairy Hide&Seek > Add Mesh Colliders To Selection
/// </summary>
public static class AddMeshCollidersTool
{
    [MenuItem("Tools/Fairy Hide&Seek/Add Mesh Colliders To Selection")]
    static void Run()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Mesh Collider", "先にHierarchyで、スキャンした教室（建物）のオブジェクトを選んでください。", "OK");
            return;
        }

        int added = 0, existed = 0;
        foreach (var root in Selection.gameObjects)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;

                var col = mf.GetComponent<MeshCollider>();
                if (col == null) { col = Undo.AddComponent<MeshCollider>(mf.gameObject); added++; }
                else existed++;

                col.sharedMesh = mf.sharedMesh;
                col.convex = false;               // 床や壁は凸でなくてよい（Convexをオンにすると形が崩れる）
            }
        }

        Debug.Log($"[MeshColliders] 追加 {added} 個 / すでにあった {existed} 個");
        if (added + existed == 0)
            EditorUtility.DisplayDialog("Mesh Collider",
                "メッシュが見つかりませんでした。選んだオブジェクトの子に、MeshFilter を持つものがあるか確認してください。", "OK");
    }
}
#endif
