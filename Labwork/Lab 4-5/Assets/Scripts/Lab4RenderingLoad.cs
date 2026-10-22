using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CMP5402.Week4
{
    // uGUI (com.unity.ugui) is required. No URP C# dependency or runtime Shader.Find.
    public class Lab4RenderingLoad : MonoBehaviour
    {
        [SerializeField, Range(2, 22)] int gridSize = 12;
        [SerializeField, Range(1, 48)] int transparentLayers = 12;
        readonly List<Material> ownedMaterials = new List<Material>();
        GameObject objectsRoot;
        GameObject overlaysRoot;
        public bool HasGeometry { get; private set; }

        public void Build(Material suppliedMaterial)
        {
            if (objectsRoot != null) return;
            objectsRoot = new GameObject("Lab4GeneratedGeometry");
            objectsRoot.transform.SetParent(transform, false);
            int count = Mathf.Clamp(gridSize, 2, 22);
            if (suppliedMaterial != null)
            {
                for (int y = 0; y < count; y++)
                for (int x = 0; x < count; x++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Lab4Cube";
                    cube.transform.SetParent(objectsRoot.transform, false);
                    cube.transform.localPosition = new Vector3(
                        (x / (float)(count - 1) - 0.5f) * 8f,
                        (y / (float)(count - 1) - 0.5f) * 5f, 0f);
                    cube.transform.localScale = Vector3.one * (4f / count);
                    Destroy(cube.GetComponent<Collider>());
                    var renderer = cube.GetComponent<MeshRenderer>();
                    var material = new Material(suppliedMaterial);
                    ownedMaterials.Add(material);
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                HasGeometry = true;
            }

            overlaysRoot = new GameObject("Lab4TransparentLayers", typeof(RectTransform), typeof(Canvas));
            overlaysRoot.transform.SetParent(transform, false);
            var canvas = overlaysRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -10;
            for (int i = 0; i < Mathf.Clamp(transparentLayers, 1, 48); i++)
            {
                var layer = new GameObject("Lab4Layer", typeof(RectTransform), typeof(Image));
                layer.transform.SetParent(overlaysRoot.transform, false);
                var rect = layer.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.15f, 0.18f);
                rect.anchorMax = new Vector2(0.85f, 0.78f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = layer.GetComponent<Image>();
                image.color = new Color(0.15f, 0.7f, 0.9f, 0.06f);
                image.raycastTarget = false;
                var material = new Material(image.material);
                material.name = "Lab4UniqueUiMaterial";
                ownedMaterials.Add(material);
                image.material = material;
            }
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (objectsRoot != null) objectsRoot.SetActive(visible);
            if (overlaysRoot != null) overlaysRoot.SetActive(visible);
        }

        void OnDestroy()
        {
            foreach (var material in ownedMaterials)
                if (material != null) Destroy(material);
            ownedMaterials.Clear();
        }
    }
}
