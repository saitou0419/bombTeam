using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RankData
{
   
    public int id;
    public string type;
    public string stageName;
    public string clearTime;
    public string mistakes;
    public string evaluation;
    public string face;
    public string comment;
}
public class CSVManage : MonoBehaviour
{
    public List<RankData> rankList = new List<RankData>();
    private void Awake()
    {
        LoadCSV();
    }

    private void LoadCSV()
    {
        // CSVファイルを読み込む
        TextAsset csvFile = Resources.Load<TextAsset>("CSVbom01");

        // ファイルが見つからない場合
        if (csvFile == null)
        {
            Debug.LogError("CSVファイルが見つかりません！");
            return;
        }

        // CSVを1行ずつに分ける
        string[] lines = csvFile.text.Split('\n');

        // 1行目は見出しなので、2行目から読み込む
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // 空行なら読み飛ばす
            if (string.IsNullOrEmpty(line)) continue;

            // カンマで項目を分ける
            string[] values = line.Split(',');

            if (values.Length != 8)
            {
                Debug.LogWarning("CSVのデータが正しくありません：" + line);
                continue;
            }

            // 1行分のデータを作る
            RankData data = new RankData();
            if (!int.TryParse(values[0].Trim().Trim('\uFEFF'), out int id))
            {
                Debug.LogWarning("IDが数字ではありません：" + values[0]);
                continue;
            }
            data.id = id;
            data.type = values[1];
            data.stageName = values[2];
            data.clearTime = values[3];
            data.mistakes = values[4];
            data.evaluation = values[5];
            data.face = values[6];
            data.comment = values[7];

            // リストに追加する
            rankList.Add(data);

            // Consoleに表示する
            Debug.Log(data.stageName + "：" + data.evaluation + "：" + data.face + "：" + data.comment);
        }
    }
    public List<RankData> GetStageData(string stageName, string type)
    {
       List<RankData> result = new List<RankData>();
       

        foreach (RankData data in rankList)
        {
            if (data.stageName == stageName && data.type == type)
            {
                result.Add(data);
            }
        }

        return result;
    }
}
