using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/Lunnchbox")]
public class LunchboxData : ScriptableObject
{
    public int id;
    public int lunchboxName;
    public int mainQuantity;
    public int sideQuantity;
    public float priceWeight;
}
