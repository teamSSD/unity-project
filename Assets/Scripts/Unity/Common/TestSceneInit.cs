using UnityEngine;

public class TestSceneInit : MonoBehaviour
{
    void Start()
    {
        var progress = ProgressSystem.Instance;
        if (progress != null)
        {
            progress.Initialize();
            Debug.Log("ProgressSystem Initialized by TestSceneInit");
        }
    }
}