using UnityEngine;

public class SliceVisualizer : MonoBehaviour
{
    public LineRenderer guideLine;
    public SpriteMask spriteMask;
    private GameObject _activeHint;

    public void SetupMask(SpriteRenderer targetRenderer, Sprite maskSprite) {
        spriteMask.sprite = maskSprite;
        spriteMask.frontSortingLayerID = targetRenderer.sortingLayerID;
        spriteMask.frontSortingOrder = targetRenderer.sortingOrder;
        spriteMask.backSortingLayerID = targetRenderer.sortingLayerID;
        spriteMask.backSortingOrder = targetRenderer.sortingOrder - 1;
    }

    public void UpdateMask(float currentCutX, float leftEdgeX, float centerY, float rangeY) {
        if (spriteMask == null || spriteMask.sprite == null) return;

        float margin = 2.0f; 
        float maskStart = leftEdgeX - margin;

        float worldWidth = currentCutX - maskStart;
        if (worldWidth < 0) worldWidth = 0;

        float worldCenterX = (maskStart + currentCutX) / 2f;
        spriteMask.transform.position = new Vector3(worldCenterX, centerY, 0);

        float spriteFullWidth = spriteMask.sprite.bounds.size.x;
        float spriteFullHeight = spriteMask.sprite.bounds.size.y;

        Vector3 pScale = spriteMask.transform.parent != null ? 
                        spriteMask.transform.parent.lossyScale : Vector3.one;

        float finalScaleX = worldWidth / (pScale.x * spriteFullWidth);
        
        float finalScaleY = (0 + rangeY * 20f) / (pScale.y * spriteFullHeight);

        spriteMask.transform.localScale = new Vector3(finalScaleX, finalScaleY, 1f);
    }

    public void UpdateGuide(float x, float startY, float endY) {
        if (guideLine == null) return;
        guideLine.SetPosition(0, new Vector3(x, startY, -0.1f));
        guideLine.SetPosition(1, new Vector3(x, endY, -0.1f));
    }

    public void ManageHint(GameObject prefab, float x, float y, bool isSlicing, float endY, float speed) {
        if (prefab == null) return;
        if (_activeHint == null) _activeHint = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
        
        float targetY = isSlicing ? endY : y;
        _activeHint.transform.position = Vector3.Lerp(_activeHint.transform.position, new Vector3(x, targetY, 0), Time.deltaTime * speed);
    }

    public void ClearHint() { if (_activeHint) Destroy(_activeHint); }
}