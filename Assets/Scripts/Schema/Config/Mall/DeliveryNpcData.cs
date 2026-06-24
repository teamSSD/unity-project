using UnityEngine;

public class DeliveryNpcData : ScriptableObject, CsvParsable
{
    public string id;
    public string characterName;
    public Sprite sprite;
    // position 필드 제거됨 — 위치는 mall 씬의 GameObject Transform이 source of truth
    public DeliveryNpcState state;
    public string groupId;
    public string prerequisiteGroupId;

    /// <summary>
    /// Editor CSV → SO 변환 시 호출. 런타임에서는 인스펙터 자산을 사용하므로 미호출.
    /// CSV의 PosX/PosY 컬럼은 무시 (씬 GameObject로 마이그레이션됨).
    /// </summary>
    public void Init(string[] args)
    {
#if UNITY_EDITOR
        if (args.Length < 6)
            throw new CsvParsingException("DeliveryNpcData requires at least 6 fields.");

        id = args[0].Trim();
        characterName = args[1].Trim();
        sprite = Resources.Load<Sprite>(args[2].Trim());
        // args[3], args[4] = PosX, PosY — 무시 (씬 GameObject가 source)
        state = System.Enum.Parse<DeliveryNpcState>(args[5].Trim());
        groupId = args.Length > 6 ? args[6].Trim() : "";
#endif
    }
}
