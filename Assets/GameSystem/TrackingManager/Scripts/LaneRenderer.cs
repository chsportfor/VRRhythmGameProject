using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaneRenderer : MonoBehaviour
{
    public Transform hitArea;
    public float lineWidth = 0.05f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.8f);

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        if (lineRenderer.material != null)
        {
            if (lineRenderer.material.HasProperty("_BaseColor"))
            {
                Color matColor = lineRenderer.material.GetColor("_BaseColor");
                matColor.a = lineColor.a;
                lineRenderer.material.SetColor("_BaseColor", matColor);
            }
            else if (lineRenderer.material.HasProperty("_Color"))
            {
                Color matColor = lineRenderer.material.color;
                matColor.a = lineColor.a;
                lineRenderer.material.color = matColor;
            }
        }

        UpdateLine();
    }

    void Update()
    {
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (hitArea != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hitArea.position);
        }
    }
}
