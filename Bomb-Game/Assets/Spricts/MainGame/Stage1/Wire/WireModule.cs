using UnityEngine;
using UnityEngine.Events;
using TMPro;


/// <summary>
/// 配線モジュール本体
/// 2つの配線モジュールそれぞれに「〇色を切る」と指示を出し、指示された色の配線を切ったグループを解除済みにする
/// </summary>
public class WireModule : MonoBehaviour
{
    ///<summary>色の定義</summary>
    [System.Serializable]
    public class WireColorDefinition
    {
        public string displayName = "";
        public Color color = Color.white;
    }

    ///<summary>配線グループ(一旦4本*2グループ)</summary>
    [System.Serializable]
    public class WireGroup
    {
        public ModuleWire[] wires;
        [Tooltip("このグループの「〇色を切る」を表示するテキスト(任意)")]
        public TMP_Text instructionText;

        //実行時の状態
        [System.NonSerialized] public int currectColorIndex;
        [System.NonSerialized] public bool isCleared;
    }

    [Header("接続")]
    [SerializeField] private MainGame maingane;

    [Header("配線グループ(一旦2グループ)")]
    [SerializeField] private WireGroup[] groups;

    [Header("色の一覧8末尾に色足せば幾らでも増やせる 同じ色が複数本あっても良い")]
    [SerializeField] private WireColorDefinition[] colors = new WireColorDefinition[]
    {
        new WireColorDefinition { displayName = "赤色", color = new Color(0.90f, 0.15f, 0.15f) },
        new WireColorDefinition { displayName = "青色", color = new Color(0.20f, 0.40f, 1.00f) },
        new WireColorDefinition { displayName = "黄色", color = new Color(1.00f, 0.90f, 0.20f) },
        new WireColorDefinition { displayName = "緑色", color = new Color(0.20f, 0.75f, 0.30f) },
        new WireColorDefinition { displayName = "桃色", color = new Color(1.00f, 0.50f, 0.75f) },
        new WireColorDefinition { displayName = "橙色", color = new Color(1.00f, 0.55f, 0.10f) },
    };

    [Header("指示文")]
    [Tooltip("{0}の部分に色の表示文が入る")]
    [SerializeField] private string instructionFormat = "{0}を切る";

    [Header("配線の色のランダム設定")]
    [Tooltip("オンならリセットの度に各配線の色をランダムに決める、オフなら各配線のInitialColorIndexを使う")]
    [SerializeField] private bool randomizeWireColors = true;

    [Header("演出イベント用")]
    public UnityEvent onCleared;

    private bool isCleared;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
        ResetModule();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (groups == null)
        {
            Debug.LogWarning("[WireModule] groups が null です。");
            return;
        }
        for (int g = 0; g < groups.Length; g++)
        {
            if (groups[g] == null || groups[g].wires == null) continue;
            foreach (var w in groups[g].wires)
            {
                if (w != null) w.Initialize(this, g);
            }
        }
    }

    ///<summary>
    ///MainGame.onStageStartedから呼ぶ用
    ///ステージ開始時にモジュールを初期状態へ
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        EnsureInitialized();
        ResetModule();
    }

    ///<summary>モジュールの状態リセットし、色と正解を引き直す</summary>
    public void ResetModule()
    {
        isCleared = false;

        if (colors == null || colors.Length == 0)
        {
            Debug.LogError("[WireModule] colorsが空です 色の一覧を設定してください");
            return;
        }
        if (groups == null || groups.Length == 0)
        {
            Debug.LogError("[WireModule] groupsが空です");
            return;
        }
        for (int g = 0; g < groups.Length; g++)
        {
            ResetGroups(g);
        }
    }

    private void ResetGroups(int groupIndex)
    {
        WireGroup group = groups[groupIndex];
        if (group == null || group.wires == null || group.wires.Length == 0)
        {
            Debug.LogError($"[WireModule] グループ{groupIndex}に配線がありません 解除できないモジュールになります");
            return;
        }

        group.isCleared = false;

        //各配線の色を決める
        var validWires = new System.Collections.Generic.List<ModuleWire>();
        foreach (var w in group.wires)
        {
            if (w == null) continue;

            int idx = randomizeWireColors
                ? Random.Range(0, colors.Length)
                : Mathf.Clamp(w.InitialColorIndex, 0, colors.Length - 1);

            w.ResetWire(idx, colors[idx].color);
            validWires.Add(w);
        }

        if (validWires.Count == 0)
        {
            Debug.LogWarning($"[WireModule] グループ{groupIndex}の配線が全て未設定です");
            return;
        }

        //正解の色:グループ内のランダムな1本の色にする
        group.currectColorIndex = validWires[Random.Range(0, validWires.Count)].ColorIndex;
        
        UpdateInstructionText(groupIndex);
    }

    //指示文
    ///<summary>グループの指示文を返す 他のUIから使いたい時用</summary>
    public string GetInstruction(int groupIndex)
    {
        if (groups == null || groupIndex < 0 || groupIndex >= groups.Length || groups[groupIndex] == null)
            return string.Empty;

        int colorIndex = groups[groupIndex].currectColorIndex;
        if (colors == null || colorIndex < 0 ||  colorIndex >= colors.Length)
            return string.Empty;

        return instructionFormat.Replace("{0}", colors[colorIndex].displayName);
    }

    private void UpdateInstructionText(int groupIndex)
    {
        var text = groups[groupIndex].instructionText;
        if (text !=  null) text.text = GetInstruction(groupIndex);
    }

    //配線の切断
    ///<summary>ModuleWIreからクリックを受け取る</summary>
    public void OnWireClicked(ModuleWire wire)
    {
        if (isCleared || wire == null || wire.IsCut) return;
        
        //Playing中のみ受け付ける
        if (maingane == null)
        {
            Debug.LogError("[WireModule] MainGameが未設定です");
            return;
        }
        if (maingane.State != GameState.Playing) return;

        int g = wire.GroupIndex;
        if (groups == null || g < 0 || g >= groups.Length || groups[g] == null) return;

        WireGroup group = groups[g];
        if (group.isCleared) return; //解除済みグループの残りの配線は動かせない

        //指示された色と違う配線は、今は何も起きない
        if (wire.ColorIndex != group.currectColorIndex) return;
         
        wire.Cut();
        group.isCleared = true;

        if (CheckAllGroupsCleared())
        {
            HandleModuleCleared();
        }
    }

    private bool CheckAllGroupsCleared()
    {
        foreach (var group in groups)
        {
            if (group == null || !group.isCleared) return false;
        }
        return true;
    }

    private void HandleModuleCleared()
    {
        isCleared = true;
        onCleared?.Invoke();
        maingane.NotifyModuleCleared(ModuleId.Wire);
    }
}
