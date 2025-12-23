using UnityEngine;

public class AgentController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float visionRange = 5f;
    [Range(0, 360)] public float visionAngle = 90f; // ширина конуса в градусах

    private Rigidbody2D rb;
    private GameObject targetEnemy;
    private GameObject heldItem;

    // Визуализация конуса обзора (через Mesh или LineRenderer)
    public MeshFilter fovMeshFilter;
    private Mesh fovMesh;
    public int fovMeshResolution = 30;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Создаём mesh для обзора
        fovMesh = new Mesh();
        fovMesh.name = "FOV Mesh";
        if (fovMeshFilter != null)
            fovMeshFilter.mesh = fovMesh;
    }

    void Update()
    {
        FindTarget();
        Act();
        DrawFOV();
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Agent");
        float closest = Mathf.Infinity;
        targetEnemy = null;

        foreach (var enemy in enemies)
        {
            if (enemy == gameObject) continue;

            Vector2 dirToEnemy = (enemy.transform.position - transform.position);
            float dist = dirToEnemy.magnitude;

            if (dist < visionRange)
            {
                // Проверяем угол попадания в конус обзора
                float angleToEnemy = Vector2.Angle(transform.up, dirToEnemy);
                if (angleToEnemy < visionAngle / 2f)
                {
                    if (dist < closest)
                    {
                        closest = dist;
                        targetEnemy = enemy;
                    }
                }
            }
        }
    }

    void Act()
    {
        if (targetEnemy != null)
        {
            float dist = Vector2.Distance(transform.position, targetEnemy.transform.position);
            if (dist > 1.5f)
            {
                MoveTowards(targetEnemy.transform.position);
            }
            else
            {
                Attack(targetEnemy);
            }
        }
        else
        {
            RandomMove();
        }
    }

    void MoveTowards(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        // Поворачиваемся в сторону движения
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void RandomMove()
    {
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            rb.linearVelocity = randomDir * moveSpeed;

            float angle = Mathf.Atan2(randomDir.y, randomDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void Attack(GameObject enemy)
    {
        Destroy(enemy);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item") && heldItem == null)
        {
            heldItem = other.gameObject;
            Destroy(other.gameObject);
        }
    }

    // Отрисовка конуса обзора
    void DrawFOV()
    {
        if (fovMeshFilter == null)
            return;

        float stepAngleSize = visionAngle / fovMeshResolution;
        int vertexCount = fovMeshResolution + 2; // центр + вершины конуса

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(fovMeshResolution) * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= fovMeshResolution; i++)
        {
            float angle = -visionAngle / 2f + stepAngleSize * i;
            float angleRad = Mathf.Deg2Rad * angle;

            Vector3 vertex = new Vector3(
                Mathf.Sin(angleRad),
                Mathf.Cos(angleRad),
                0
            ) * visionRange;

            vertices[i + 1] = vertex;
        }

        for (int i = 0; i < fovMeshResolution; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        fovMesh.Clear();
        fovMesh.vertices = vertices;
        fovMesh.triangles = triangles;
        fovMesh.RecalculateBounds();
        fovMesh.RecalculateNormals();
    }
}
