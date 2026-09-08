using System;
using System.Collections.Generic;
using UnityEngine;

public class SliceMiniGame : MiniGameAbstract
{
    [SerializeField] private SliceVisualizer visualizer;
    [Header("SFX")]
    [SerializeField] private AudioClip interactionSfx;

    [Header("Prefabs")]
    public GameObject cuttingBoard;
    public GameObject activeKnife;
    public GameObject slicePrefab;
    public GameObject mouseHintPrefab;
    public GameObject Ingredient;

    [Header("Settings")]
    public int totalSlices = 6;
    public int segmentsPerSlice = 20;
    public float sliceRangeY = 4f;
    public float sliceMargin = 0.3f;
    public float tolerance = 0.1f;
    public float hintMoveSpeed = 15f;
    public float sliceYOffset = 0f;
    public float pieceXOffset = 0f;
    [Tooltip("슬라이스 선 중심 Y (월드). 0이면 Ingredient 기준 사용")]
    public float sliceCenterY = 0f;
    public bool useFixedSliceCenter = false;

    [Header("Visuals")]
    public Sprite maskSprite;

    private SliceScorer _scorer;
    private int _currentSliceIndex = 0;
    private bool _isSlicing = false;
    private bool[] _segmentChecked;
    private FoodData currentIngredient;
    private float _cachedSliceAreaWidth;

    // --- 계산 프로퍼티 ---
    private float CenterX => Ingredient.transform.position.x;
    private float SliceBaseY => useFixedSliceCenter ? sliceCenterY : Ingredient.transform.position.y + sliceYOffset;
    private float StartY => SliceBaseY + (sliceRangeY / 2f);
    private float EndY => SliceBaseY - (sliceRangeY / 2f);
    private float LeftEdgeX => CenterX - (_cachedSliceAreaWidth / 2f);
    private float CurrentTargetX
    {
        get {
            if (totalSlices <= 1) return CenterX;
            float spacing = _cachedSliceAreaWidth / (totalSlices + 1);
            return LeftEdgeX + (spacing * (_currentSliceIndex + 1));
        }
    }

    public override void ApplyUpgrade(float m, int s) { base.ApplyUpgrade(m, s); tolerance /= m; }

    public override void SetIngredients(List<FoodData> ingredients, string toolId = null)
    {
        if (ingredients.Count <= 0) return;

        currentIngredient = ingredients[0];
        _scorer = new SliceScorer(totalSlices, segmentsPerSlice);
        var renderer = Ingredient.GetComponent<SpriteRenderer>();
        renderer.sprite = toolId != null ? ingredients[0].GetImageForTool(toolId) : ingredients[0].image;
        renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        _cachedSliceAreaWidth = renderer.bounds.size.x - (sliceMargin * 2f);
        
        visualizer.SetupMask(renderer, maskSprite);
        RefreshState();
        Cursor.visible = false;

#if AFTERTASTE_E2E
        // 현재 안내선의 시작/끝 좌표만 관측한다. 실제 절단 판정은 mouse down/move/up과
        // SliceMiniGame의 허용 오차가 그대로 수행한다.
        E2EWorldTargetRegistry.RegisterDynamic("cooking.minigame.slice.start",
            () => new Vector3(CurrentTargetX, StartY, 0f));
        E2EWorldTargetRegistry.RegisterDynamic("cooking.minigame.slice.end",
            () => new Vector3(CurrentTargetX, EndY, 0f));
#endif
    }

    public override void OnUpdate()
    {
        UpdateKnifeTransform();
        if (!isPlaying) return;
        
        HandleInput();
        visualizer.ManageHint(mouseHintPrefab, CurrentTargetX, StartY, _isSlicing, EndY, hintMoveSpeed);
    }

    private void HandleInput()
    {
        Vector3 pos = GetMouseWorldPos();

        if (Input.GetMouseButtonDown(0) && IsAtStart(pos)) {
            _isSlicing = true;
            _segmentChecked = new bool[segmentsPerSlice];
        }
        else if (_isSlicing && Input.GetMouseButton(0)) {
            ProceedSlice(pos);
        }
        else if (_isSlicing && Input.GetMouseButtonUp(0)) {
            SoundManager.Instance?.Play2DSFX(interactionSfx);
            FinishSlice();
        }
    }

    private void ProceedSlice(Vector3 pos) {
        float relY = StartY - pos.y;
        int segIdx = Mathf.FloorToInt(relY / (sliceRangeY / segmentsPerSlice));
        
        for (int i = 0; i <= segIdx; i++) {
            if (i >= 0 && i < segmentsPerSlice && !_segmentChecked[i]) {
                _scorer.RecordSegment(_currentSliceIndex, i, pos.x, CurrentTargetX, tolerance);
                _segmentChecked[i] = true;
            }
        }
    }

    private void FinishSlice() {
        _isSlicing = false;
        visualizer.UpdateMask(CurrentTargetX, LeftEdgeX, Ingredient.transform.position.y, sliceRangeY);
        SpawnPiece();
        
        _currentSliceIndex++;
        visualizer.ClearHint();
        
        if (_currentSliceIndex < totalSlices) RefreshState();
        else EndGame();
    }

    private void SpawnPiece() {
        if (slicePrefab == null) return;
        var piece = Instantiate(slicePrefab, new Vector3(CurrentTargetX + pieceXOffset, SliceBaseY, -0.2f), Quaternion.identity, transform);
        piece.transform.localScale = Ingredient.transform.localScale;
        if (currentIngredient?.pieceSprite != null)
            piece.GetComponent<SpriteRenderer>().sprite = currentIngredient.pieceSprite;
    }

    private void RefreshState() => visualizer.UpdateGuide(CurrentTargetX, StartY, EndY);

    private void UpdateKnifeTransform() {
        if (activeKnife != null) activeKnife.transform.position = GetMouseWorldPos();
    }

    // --- 유틸리티 메서드 (에러 해결용) ---
    private Vector3 GetMouseWorldPos() {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;
        return pos;
    }

    private bool IsAtStart(Vector3 pos) {
        return Mathf.Abs(pos.x - CurrentTargetX) < tolerance * 2 && Mathf.Abs(pos.y - StartY) < 0.5f;
    }

    public override float CalculateScore() => _scorer != null ? _scorer.GetFinalScore() : 0f;

#if AFTERTASTE_E2E
    public override string E2ENextInput => "PointerSlice";
    public override float E2ECurrentValue => _currentSliceIndex;
    public override float E2ETargetValue => totalSlices;
#endif

    private void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.UnregisterDynamic("cooking.minigame.slice.start");
        E2EWorldTargetRegistry.UnregisterDynamic("cooking.minigame.slice.end");
#endif
        Cursor.visible = true;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
