using UnityEngine;

public class CSVTest : MonoBehaviour
{
    [SerializeField] private CSVManage csvManage;

    private void Start()
    {
        Debug.Log("CSVManage確認：" + (csvManage != null));

        if (csvManage == null)
        {
            Debug.LogError("CSVManageが設定されていません！");
            return;
        }

        Debug.Log("CSV全体のデータ数：" + csvManage.rankList.Count);

        foreach (RankData data in csvManage.rankList)
        {
            Debug.Log(
                "ステージ名=[" + data.stageName +
                "] 種類=[" + data.type + "]"
            );
        }

        Debug.Log("1-1の練習データを検索します");

        var result = csvManage.GetStageData("1-1", "練習");

        Debug.Log("取得したデータ数：" + result.Count);
    }
}
