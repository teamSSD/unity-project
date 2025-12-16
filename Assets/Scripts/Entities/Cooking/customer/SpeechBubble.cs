using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contents;

    public void setContents(string contents)
    {
        this.contents.text = contents;
    }
}
