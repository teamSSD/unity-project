using UnityEngine;

public class DeliveryNpcSay : MonoBehaviour
{
    [SerializeField] private GameObject speechBubblePrefab;

    private GameObject currentBubble;

    public void Say(string message)
    {
        Clear();

        currentBubble = Instantiate(speechBubblePrefab);
        currentBubble.transform.position =
            transform.position + new Vector3(-2.5f, 4f, 0f);

        SpeechBubble bubble =
            currentBubble.GetComponent<SpeechBubble>();

        bubble.setContents(message);
    }

    public void Clear()
    {
        if (currentBubble != null)
            Destroy(currentBubble);
    }
}
