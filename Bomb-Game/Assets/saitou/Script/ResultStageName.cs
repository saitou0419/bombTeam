using UnityEngine;
using TMPro;
public class ResultStageName:MonoBehaviour
{
  
   [Header("ステージ名を表示するText")]
   [SerializeField] private TMP_Text stageNameText;

    private void Start()
    {
        if (!ResultHolder.HasResult)
        {
            Debug.LogWarning("リザルトがありません。");
            return;
        }
        ResultData data = ResultHolder.Current;
        stageNameText.text = "STAGE " + data.stageId;
    }

}
