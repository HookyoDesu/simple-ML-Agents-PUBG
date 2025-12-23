using UnityEngine;
using System.Collections.Generic;

public class ZoneManager : MonoBehaviour
{
    public float startRadius = 25f;
    public float endRadius = 3f;
    public float shrinkDuration = 120f;

    public Vector2 mapMin = new Vector2(-25f, -25f);
    public Vector2 mapMax = new Vector2(25f, 25f);

    public LineRenderer lineRenderer;
    public int circleSegments = 100;

    public Vector3 zoneCenter;
    private float currentRadius;
    private float shrinkTimer;

    void Awake()
    {
        // Если LineRenderer не назначен в инспекторе — добавим его здесь
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.widthMultiplier = 0.1f;
            lineRenderer.loop = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }

        // Очень важно задать positionCount сразу
        lineRenderer.positionCount = circleSegments + 1;
    }

    void Start()
    {
        zoneCenter = new Vector3(
            Random.Range(mapMin.x, mapMax.x),
            Random.Range(mapMin.y, mapMax.y),
            0f);

        currentRadius = startRadius;
        shrinkTimer = 0f;
    }

    void Update()
    {
        shrinkTimer += Time.deltaTime;
        float t = Mathf.Clamp01(shrinkTimer / shrinkDuration);
        currentRadius = Mathf.Lerp(startRadius, endRadius, t);

        DrawCircle();

        // Проверяем агентов на нахождение внутри зоны
        foreach (var agent in FindObjectsOfType<RLAgent>())
        {
            Vector2 pos2D = new Vector2(agent.transform.position.x, agent.transform.position.y);
            float dist = Vector2.Distance(pos2D, new Vector2(zoneCenter.x, zoneCenter.y));
            agent.SetDamageZoneStatus(dist > currentRadius);  // вне зоны — true (дебафф)
        }
    }


    void DrawCircle()
    {
        if (lineRenderer == null)
            return;

        if (lineRenderer.positionCount != circleSegments + 1)
            lineRenderer.positionCount = circleSegments + 1;

        float angleStep = 360f / circleSegments;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            float x = zoneCenter.x + Mathf.Cos(angle) * currentRadius;
            float y = zoneCenter.y + Mathf.Sin(angle) * currentRadius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 1));
        }
    }
}
