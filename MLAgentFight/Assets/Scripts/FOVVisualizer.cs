using UnityEngine;

public class FOVVisualizer : MonoBehaviour
{
    [Header("FOV Settings")]
    public float fovAngle = 120f;    // Угол обзора (градусы)
    public float fovRadius = 15f;    // Дальность обзора
    public int segments = 50;        // Количество сегментов (чем больше, тем плавнее)

    [Header("Appearance")]
    public Color fovColor = new Color(0, 0.5f, 1f, 0.3f); // Голубой с прозрачностью
    public Material fovMaterial;     // Можно оставить null, тогда создастся автоматически

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log("LineRenderer создан автоматически!");
        }

        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.blue;

        // Настройка LineRenderer
        //lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false; // Локальные координаты (чтобы вращался с агентом)

        // Настройка внешнего вида
        if (fovMaterial == null)
        {
            fovMaterial = new Material(Shader.Find("Sprites/Default")); // Стандартный 2D шейдер
        }
        lineRenderer.material = fovMaterial;
        lineRenderer.startColor = fovColor;
        lineRenderer.endColor = fovColor;
        lineRenderer.startWidth = fovRadius * 0.1f;
        lineRenderer.endWidth = fovRadius * 0.1f;
    }

    void Update()
    {
        DrawFOVPie();
    }

    void DrawFOVPie()
    {
        // Увеличиваем количество точек, чтобы создать "заполнение"
        int totalPoints = segments + 2;
        Vector3[] points = new Vector3[totalPoints];

        // Центр "пирога" (начальная точка)
        points[0] = Vector3.zero;

        // Рассчитываем точки по окружности
        float startAngle = -fovAngle / 2f;
        float angleStep = fovAngle / segments;

        for (int i = 1; i < totalPoints; i++)
        {
            float angle = startAngle + angleStep * (i - 1);
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.up;
            points[i] = dir * fovRadius;
        }

        // Замыкаем дугу (последняя точка = первой точке на окружности)
        points[totalPoints - 1] = points[1];

        // Применяем точки к LineRenderer
        lineRenderer.positionCount = totalPoints;
        lineRenderer.SetPositions(points);
    }
}