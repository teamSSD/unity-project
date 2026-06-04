using UnityEngine;

public class TestSceneInit : MonoBehaviour
{
    void Start()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null)
        {
            progress.Initialize();
        }
    }
}