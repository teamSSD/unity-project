using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BentoPositionModel : MonoBehaviour
{
    private bool set = false;

    public bool IsSet() => set;

    public void Setting(bool state) => set = state;
}
