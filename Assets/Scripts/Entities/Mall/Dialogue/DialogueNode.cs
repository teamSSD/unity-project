using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueRow
{
    public string id;
    public string npc;
    public string contents;
    public string condition;
    public string next;
    public string branchId;
}

[System.Serializable]
public class BranchRow
{
    public string id;
    public int order;
    public string contents;
    public string next;
}
