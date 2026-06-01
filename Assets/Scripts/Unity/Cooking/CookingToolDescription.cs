using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CookingToolDescription : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI contentText;

    public void setName(string name)
    {
        nameText.text = "요리 도구 : " + name;
    }

    public void setIngredients(List<string> contents)
    {
        contentText.text = "내용물 : " + String.Join(", ", contents);
    }

    public void setResult(string content)
    {
        contentText.text = "내용물 : " + content + "(요리됨)";
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}