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
            data.type = values[1].Trim();
            data.stageName = values[2].Trim();
            data.clearTime = values[3].Trim();
            data.mistakes = values[4].Trim();
            data.evaluation = values[5].Trim();
            data.face = values[6].Trim();
            data.comment = values[7].Trim();

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
    // 結果に合うCSVデータを取得する
    public RankData GetResultData(string stageName, float remainingTime, int mistakeCount)
    {
        Debug.Log("検索開始");
        Debug.Log("ステージ：" + stageName);
        Debug.Log("残り時間：" + remainingTime);
        Debug.Log("ミス回数：" + mistakeCount);

        foreach (RankData data in rankList)
        {
            if (data.stageName.Trim() != stageName.Trim())
                continue;

            bool timeMatch = CheckTime(data.clearTime.Trim(), remainingTime);
            bool mistakeMatch = CheckMistakes(data.mistakes.Trim(), mistakeCount);

            Debug.Log("CSVミス条件：" + data.mistakes);
            Debug.Log("時間判定：" + timeMatch);
            Debug.Log("ミス判定：" + mistakeMatch);

            if (timeMatch && mistakeMatch)
            {
                Debug.Log("取得成功：" + data.evaluation);
                return data;
            }
        }

        Debug.LogError("一致するCSVデータがありません！");
        return null;
    }


    // 時間の条件を調べる
    private bool CheckTime(string condition, float remainingTime)
    {
        if (condition == "関係なし")
        {
            return true;
        }

        if (condition == "30秒以内")
        {
            return remainingTime > 0 && remainingTime <= 30f;
        }

        if (condition == "30秒以上")
        {
            return remainingTime >= 30f;
        }

        return false;
    }

    // ミス回数の条件を調べる
    private bool CheckMistakes(string condition, int mistakeCount)
    {
        if (condition == "関係なし")
        {
            return true;
        }

        if (condition == "0")
        {
            return mistakeCount == 0;
        }
     
        if (condition == "1~4")
        {
            return mistakeCount >= 1 && mistakeCount <= 4;
        }

        if (condition == "5")
        {
            return mistakeCount == 5;
        }

        if (condition == "6")
        {
            return mistakeCount == 6;
        }

        return false;
    }

}
