using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultStageName : MonoBehaviour
{
    [Header("ステージ名を表示するText")]
    [SerializeField] private TMP_Text stageNameText;

    [Header("残り時間を表示するText")]
    [SerializeField] private TMP_Text timeText;

    [Header("CSV管理")]
    [SerializeField] private CSVManage csvManage01;

    [Header("ランク画像を表示するImage")]
    [SerializeField] private Image rank;

    [Header("ランク画像 6種類")]
    [SerializeField] private Sprite[] rankSprites;

    [Header("先生の表情を表示するImage")]
    [SerializeField] private Image teacherImage;

    [Header("先生の表情画像 4種類")]
    [SerializeField] private Sprite[] teacherSprites;

    [Header("コメント画像を表示するImage")]
    [SerializeField] private Image commentImage;

    [Header("コメント画像 5種類")]
    [SerializeField] private Sprite[] commentSprites;

    private void Start()
    {
        // 結果が保存されているか確認
        if (!ResultHolder.HasResult)
        {
            Debug.LogWarning("リザルトがありません。");
            return;
        }

        // CSVManageが設定されているか確認
        if (csvManage01 == null)
        {
            Debug.LogError("CSVManage01が設定されていません。");
            return;
        }

        // 結果データを取得
        ResultData result = ResultHolder.Current;

        string stageId = result.stageId;
        float remainingTime = result.remainingTime;
        int mistakeCount = result.mistakeCount;

        // ステージ名を表示
        stageNameText.text = stageId;

        // 残り時間を表示
        timeText.text = Mathf.CeilToInt(remainingTime).ToString();

        Debug.Log("ステージ：" + stageId);
        Debug.Log("残り時間：" + remainingTime);
        Debug.Log("ミス回数：" + mistakeCount);

        // クリアしていない場合
        if (!result.isCleared)
        {
            Debug.Log("ゲームオーバー：失格");

            ChangeRankImage("失格");
            ChangeTeacherImage("Sad");
            ChangeCommentImage("Comment01");

            return;
        }

        // クリアした場合はCSVからデータを取得
        RankData data = csvManage01.GetResultData(
            stageId,
            remainingTime,
            mistakeCount
        );

        if (data == null)
        {
            Debug.LogError("CSVから結果を取得できませんでした。");
            return;
        }

        Debug.Log("評価：" + data.evaluation);
        Debug.Log("先生：" + data.face);
        Debug.Log("コメント：" + data.comment);

        // 画像を変更
        ChangeRankImage(data.evaluation);
        ChangeTeacherImage(data.face);
        ChangeCommentImage(data.comment);
    }
    // ランク画像を変更
    private void ChangeRankImage(string evaluation)
    {
        int index = -1;

        switch (evaluation)
        {
            case "ダイナマイト級":
                index = 0;
                break;

            case "時限爆弾級":
                index = 1;
                break;

            case "爆竹級":
                index = 2;
                break;

            case "失格":
                index = 3;
                break;

            case "合格":
                index = 4;
                break;

            case "不合格":
                index = 5;
                break;
        }

        if (index >= 0 && index < rankSprites.Length)
        {
            rank.sprite = rankSprites[index];
        }
        else
        {
            Debug.LogWarning("対応するランク画像がありません：" + evaluation);
        }
    }

    // 先生の表情画像を変更
    private void ChangeTeacherImage(string face)
    {
        int index = -1;

        switch (face)
        {
            case "Surprise":
                index = 0;
                break;

            case "Smile":
                index = 1;
                break;

            case "Normal":
                index = 2;
                break;

            case "Sad":
                index = 3;
                break;
        }

        if (index >= 0 && index < teacherSprites.Length)
        {
            teacherImage.sprite = teacherSprites[index];
        }
        else
        {
            Debug.LogWarning("対応する先生の画像がありません：" + face);
        }
    }

    // コメント画像を変更
    private void ChangeCommentImage(string comment)
    {
        int index = -1;

        switch (comment)
        {
            case "Comment01":
                index = 0;
                break;

            case "Comment02":
                index = 1;
                break;

            case "Comment03":
                index = 2;
                break;

            case "Comment04":
                index = 3;
                break;

            case "Comment06":
                index = 4;
                break;
        }

        if (index >= 0 && index < commentSprites.Length)
        {
            commentImage.sprite = commentSprites[index];
        }
        else
        {
            Debug.LogWarning("対応するコメント画像がありません：" + comment);
        }
    }
}

