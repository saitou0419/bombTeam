using UnityEngine;
using TMPro;
public class ResultStageName:MonoBehaviour
{
  
   [Header("ステージ名を表示するText")]
   [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text timeText;

    private void Start()
    {
        if (!ResultHolder.HasResult)
        {
            Debug.LogWarning("リザルトがありません。");
            return;
        }
        ResultData data = ResultHolder.Current;
        timeText.text = "残り時間：" + data.remainingTime.ToString("F1") + "秒";
        stageNameText.text = "STAGE " + data.stageId;
    }

}
