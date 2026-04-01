using UnityEngine;

public class TestSceneInit : MonoBehaviour
{
    void Start()
    {
        if (ProgressSystem.Instance != null)
        {
            ProgressSystem.Instance.Initialize();
            Debug.Log("ProgressSystem Initialized by TestSceneInit");
        }
    }
}