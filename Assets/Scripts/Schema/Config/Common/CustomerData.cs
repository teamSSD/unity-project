using System;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/Customer")]
public class CustomerData : ScriptableObject
{
    public string type;
    [Tooltip("매핑되는 DeliveryNpc id (예: npc_getoro). 비면 NPC 매핑 없음 — 중복/퀘스트 제외 대상 아님.")]
    public string npcId;
    public Sprite characterImage;
    [Tooltip("스프라이트별 크기 보정 — sprite 원본/import 차이로 다른 NPC와 비례가 맞지 않을 때 (기본 1)")]
    public float displayScale = 1f;
    public string orderingMessage;
    public string satisfiedMessage;
    public string unsatisfiedMessage;
    public string escapeMessage;
}