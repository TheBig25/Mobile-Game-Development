using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        //Screen's current safe area
        Rect safeArea = Screen.safeArea;

        //Change RectTranform if safe area changes
        if (safeArea != lastSafeArea)
        {
            //Update last safe area
            lastSafeArea = safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            //Safe area to anchor coordinates
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            //Apply to the RectTransform
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Check if safeArea changed
        if(Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }
}
