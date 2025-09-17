using UnityEngine;
using UnityEngine.UI;

namespace RetroTech
{
    /// <summary>
    /// Simple vertical gradient effect for Unity UI graphics.
    /// Applies a gradient from bottom to top using the provided colors.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class UiVerticalGradient : BaseMeshEffect
    {
        [SerializeField]
        private Color topColor = Color.white;

        [SerializeField]
        private Color bottomColor = Color.white;

        public void SetColors(Color top, Color bottom)
        {
            topColor = top;
            bottomColor = bottom;
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            Rect rect = graphic.rectTransform.rect;
            float height = rect.height;
            if (Mathf.Approximately(height, 0f))
            {
                height = 1f;
            }

            UIVertex vertex = default;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float normalizedY = (vertex.position.y - rect.yMin) / height;
                Color vertexColor = Color.Lerp(bottomColor, topColor, normalizedY);
                vertexColor *= graphic.color;
                vertex.color = vertexColor;
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
