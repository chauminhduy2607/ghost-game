using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = new Color(0.35f, 0.35f, 0.35f); // Xám sáng
    public Color bottomColor = new Color(0.2f, 0.2f, 0.2f); // Xám đậm
    
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        var count = vh.currentVertCount;
        if (count == 0)
            return;

        var vertexList = new UIVertex[count];
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertexList[i], i);
        }

        float bottomY = vertexList[0].position.y;
        float topY = vertexList[0].position.y;

        for (int i = 1; i < count; i++)
        {
            float y = vertexList[i].position.y;
            if (y > topY)
                topY = y;
            else if (y < bottomY)
                bottomY = y;
        }

        float height = topY - bottomY;

        for (int i = 0; i < count; i++)
        {
            UIVertex vertex = vertexList[i];
            vertex.color = Color.Lerp(bottomColor, topColor, (vertex.position.y - bottomY) / height);
            vh.SetUIVertex(vertex, i);
        }
    }
}