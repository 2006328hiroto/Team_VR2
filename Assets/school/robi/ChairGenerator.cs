using UnityEngine;

public class ChairGenerator : MonoBehaviour
{
    [ContextMenu("Generate Chair")]
    public void Generate()
    {
        // 1. 親オブジェクトの作成
        GameObject chairMaster = new GameObject("Chair_Master");
        chairMaster.transform.position = Vector3.zero;

        // 2. 座面 (Seat) の作成
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.name = "Chair_Seat";
        seat.transform.SetParent(chairMaster.transform);
        seat.transform.localPosition = new Vector3(0, 0.45f, 0);
        seat.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);

        // 3. 背もたれ (Back) の作成
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Chair_Back";
        back.transform.SetParent(chairMaster.transform);
        back.transform.localPosition = new Vector3(0, 0.725f, -0.225f);
        back.transform.localScale = new Vector3(0.5f, 0.5f, 0.05f);

        // 4. 脚 (Legs) の作成（4本）
        float[] legX = { -0.2f, 0.2f, -0.2f, 0.2f };
        float[] legZ = { -0.2f, -0.2f, 0.2f, 0.2f };

        for (int i = 0; i < 4; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Chair_Leg_" + (i + 1);
            leg.transform.SetParent(chairMaster.transform);
            leg.transform.localPosition = new Vector3(legX[i], 0.225f, legZ[i]);
            leg.transform.localScale = new Vector3(0.05f, 0.225f, 0.05f);
        }

        Debug.Log("椅子の生成が完了しました！");
    }
}