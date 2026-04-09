using System;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/Customer")]
public class CustomerData : ScriptableObject
{
    public string type;
    public Sprite characterImage;
    public string orderingMessage;
    public string satisfiedMessage;
    public string unsatisfiedMessage;
    public string escapeMessage;
}