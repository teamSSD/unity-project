using UnityEngine;

public class TestSceneInit : MonoBehaviour
{
    void Start()
    {
        if (ProgressSystem.instance != null)
        {
            ProgressSystem.instance.Initialize();
            Debug.Log("ProgressSystem Initialized by TestSceneInit");
        }
    }
}