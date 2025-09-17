using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempMinigameData : ScriptableObject
{
    public int id;
    public string minigameName;

    public override bool Equals(object obj)
    {
        if (obj is IngredientData other)
            return this.id == other.id;
        return false;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}
