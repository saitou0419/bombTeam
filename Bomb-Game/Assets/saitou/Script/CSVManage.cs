using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RankData
{
    public string type;
    public int id;
    public string stageName;
    public string clearTime;
    public string mistakes;
    public string evaluation;
}
public class CSVManage: MonoBehaviour
{
    public List<RankData> rankList = new List<RankData>();
    void Start()
    {
        LoadCSV();
    }
    void LoadCSV()
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

            if (values.Length != 6)
            {
                Debug.LogWarning("CSVのデータが正しくありません：" + line);
                continue;
            }

            // 1行分のデータを作る
            RankData data = new RankData();

            data.type = values[0];
           // data.id = int.Parse(values[1]);
            data.stageName = values[2];
            data.clearTime = values[3];
            data.mistakes = values[4];
            data.evaluation = values[5];

            // リストに追加する
            rankList.Add(data);

            // Consoleに表示する
            Debug.Log(data.stageName + "：" + data.evaluation);
        }
    }

}
