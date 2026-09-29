using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore
{
    public sealed class ShieldView : MonoBehaviour
    {
        [SerializeField] private Color fillColor = new(0.05f, 0.55f, 1f, 0.5f);
        [SerializeField] private Color outlineColor = new(1f, 1f, 1f, 0.95f);

        private readonly Dictionary<GridPosition, GameObject> visuals = new();
        private Material fillMaterial;
        private Material outlineMaterial;

        public void Initialize()
        {
            fillMaterial = CreateTransparentMaterial(fillColor, "Shield Fill Material");
            outlineMaterial = CreateTransparentMaterial(outlineColor, "Shield Outline Material");
        }

        public void Apply(ShieldState state)
        {
            List<GridPosition> removed = new();
            foreach (KeyValuePair<GridPosition, GameObject> entry in visuals)
            {
                if (!state.Contains(entry.Key))
                {
                    removed.Add(entry.Key);
                }
            }

            foreach (GridPosition position in removed)
            {
                Destroy(visuals[position]);
                visuals.Remove(position);
            }

            foreach (GridPosition position in state.Positions)
            {
                if (!visuals.ContainsKey(position))
                {
                    visuals.Add(position, CreateShieldVisual(position));
                }
            }
        }

        private GameObject CreateShieldVisual(GridPosition position)
        {
            GameObject root = new($"Shield {position}");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = GameConfig.LogicalToWorld(position, -0.04f);

            CreateQuad(root.transform, "Outline", new Vector3(0.88f, 0.88f, 1f), outlineMaterial, 5);
            CreateQuad(root.transform, "Fill", new Vector3(0.76f, 0.76f, 1f), fillMaterial, 6);
            return root;
        }

        private static void CreateQuad(
            Transform parent,
            string objectName,
            Vector3 scale,
            Material material,
            int sortingOrder)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = scale;

            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = sortingOrder;
        }

        private static Material CreateTransparentMaterial(Color color, string materialName)
        {
            Material material = BoardView.CreateUnlitMaterial(color, materialName);
            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private void OnDestroy()
        {
            if (fillMaterial != null) Destroy(fillMaterial);
            if (outlineMaterial != null) Destroy(outlineMaterial);
        }
    }
}
