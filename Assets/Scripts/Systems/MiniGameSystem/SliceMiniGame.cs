using System;
using System.Collections.Generic;
using UnityEngine;

public class SliceMiniGame : MiniGameAbstract
{
    [SerializeField] private SliceVisualizer visualizer;

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
    public float sliceAreaWidth = 5f;
    public float tolerance = 0.1f;
    public float hintMoveSpeed = 15f;

    [Header("Visuals")]
    public Sprite maskSprite;

    private SliceScorer _scorer;
    private int _currentSliceIndex = 0;
    private bool _isSlicing = false;
    private bool[] _segmentChecked;

    // --- 계산 프로퍼티 (에러 해결용) ---
    private float CenterX => Ingredient.transform.position.x;
    private float StartY => Ingredient.transform.position.y + (sliceRangeY / 2f);
    private float EndY => Ingredient.transform.position.y - (sliceRangeY / 2f);
    private float ActualWidth => Ingredient.GetComponent<SpriteRenderer>().bounds.size.x;
    private float LeftEdgeX => CenterX - (sliceAreaWidth / 2f);
    private float CurrentTargetX 
    {
        get {
            if (totalSlices <= 1) return CenterX;
            float spacing = sliceAreaWidth / (totalSlices - 1);
            return LeftEdgeX + (spacing * _currentSliceIndex);
        }
    }

    public override void SetIngredients(List<FoodData> ingredients)
    {
        if (ingredients.Count <= 0) return;
        
        _scorer = new SliceScorer(totalSlices, segmentsPerSlice);
        var renderer = Ingredient.GetComponent<SpriteRenderer>();
        renderer.sprite = ingredients[0].image;
        renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        
        visualizer.SetupMask(renderer, maskSprite);
        RefreshState();
        Cursor.visible = false;
    }

    public override void OnUpdate()
    {
        UpdateKnifeTransform();
        if (!isPlaying) return;
        elapsedTime = 0;
        
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
        var piece = Instantiate(slicePrefab, new Vector3(CurrentTargetX, Ingredient.transform.position.y, -0.2f), Quaternion.identity);
        piece.transform.localScale = Ingredient.transform.localScale;
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

    private void OnDestroy() => Cursor.visible = true;
}