using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DialogueManager 선택지 처리 (partial). 버튼 생성/선택/정리.
/// </summary>
public partial class DialogueManager
{
    private void ShowChoices(List<DialogueChoice> choices)
    {
        ClearChoices();
        foreach (var choice in choices)
            BuildChoiceButton(choice);
    }

    private void BuildChoiceButton(DialogueChoice choice)
    {
        Button btn = Instantiate(choiceButtonPrefab, choicesParent);
        var labelText = btn.GetComponentInChildren<TMP_Text>();
        labelText.text = choice.label;

        // 삼각형 마커 (별도 TMP 요소)
        var markerObj = new GameObject("Marker", typeof(RectTransform));
        markerObj.transform.SetParent(btn.transform, false);
        markerObj.transform.SetAsFirstSibling();
        var marker = markerObj.AddComponent<TextMeshProUGUI>();
        marker.text = "▶";
        marker.fontSize = labelText.fontSize;
        marker.font = labelText.font;
        marker.color = Color.white;
        marker.alignment = TextAlignmentOptions.MidlineLeft;
        var markerLE = markerObj.AddComponent<LayoutElement>();
        markerLE.preferredWidth = 30;

        // 레이블 유동 너비
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        // 가로 레이아웃
        var hlg = btn.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 5;
        hlg.padding = new RectOffset(10, 10, 0, 0);

        var captured = choice;
        btn.onClick.AddListener(() => OnChoiceSelected(captured));
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        ClearChoices();
        waitingForChoice = false;

        if (!string.IsNullOrEmpty(choice.resultTag))
            lastResultTag = choice.resultTag;

        if (choice.responses != null && choice.responses.Count > 0)
        {
            branchResponses = choice.responses;
            branchIndex = 0;
            ShowLine(branchResponses[0]);
        }
        else
        {
            AdvanceDialogue();
        }
    }

    private void ClearChoices()
    {
        foreach (Transform t in choicesParent)
            Destroy(t.gameObject);
    }
}
