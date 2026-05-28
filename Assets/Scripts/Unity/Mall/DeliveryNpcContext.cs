using UnityEngine;

public class DeliveryNpcContext : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject speechBubblePrefab;
    public GameObject receiptPrefab;

    [Header("런타임 상태")]
    [HideInInspector] public GameObject speechBubble;
    [HideInInspector] public SpeechBubble speechBubbleScript;
}
