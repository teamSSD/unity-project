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

    public void Init(string[] args)
    {
        // CSV: NpcId,CharacterName,SpritePath,PosX,PosY,DeliveryState,GroupId
        if (args.Length < 6)
            throw new CsvParsingException("DeliveryNpcData requires at least 6 fields.");

        id = args[0].Trim();
        characterName = args[1].Trim();
        sprite = Resources.Load<Sprite>(args[2].Trim());
        position = new Vector2(float.Parse(args[3].Trim()), float.Parse(args[4].Trim()));
        state = System.Enum.Parse<DeliveryNpcState>(args[5].Trim());
        groupId = args.Length > 6 ? args[6].Trim() : "";
    }
}
