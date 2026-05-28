using UnityEngine;

public class DeliveryNpcData : ScriptableObject, CsvParsable
{
    public string id;
    public string characterName;
    public Sprite sprite;
    public Vector2 position;
    public DeliveryNpcState state;
    public string groupId;
    public string prerequisiteGroupId;

    /// <summary>
    /// Editor CSV → SO 변환 시 호출. 런타임에서는 인스펙터 자산을 사용하므로 미호출.
    /// </summary>
    public void Init(string[] args)
    {
#if UNITY_EDITOR
        if (args.Length < 6)
            throw new CsvParsingException("DeliveryNpcData requires at least 6 fields.");

        id = args[0].Trim();
        characterName = args[1].Trim();
        sprite = Resources.Load<Sprite>(args[2].Trim());
        position = new Vector2(float.Parse(args[3].Trim()), float.Parse(args[4].Trim()));
        state = System.Enum.Parse<DeliveryNpcState>(args[5].Trim());
        groupId = args.Length > 6 ? args[6].Trim() : "";
#endif
    }
}
