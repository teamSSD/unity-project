using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/CookingTool")]
public class CookingToolData : ScriptableObject
{
    public string id;
    public string cookerName;
    public Sprite defaultImage;

    public void Init(Sprite defaultImage, string id, string cookerName)
    {
        this.defaultImage = defaultImage;
        this.id = id;
        this.cookerName = cookerName;
    }

    public override bool Equals(object obj)
    {
        if (obj is CookingToolData other)
            return this.id == other.id;
        return false;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}