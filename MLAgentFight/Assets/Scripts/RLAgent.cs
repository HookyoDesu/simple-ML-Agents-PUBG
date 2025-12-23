using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public class RLAgent : Agent
{
    [Header("Bruh")]
    public FOVVisualizer fovVisualizer;
    public float baseMoveSpeed = 1f;
    private float maxMoveSpeed;

    public float rotationSpeed = 180f;

    public float health = 100f;
    public float maxHealth = 100f;

    public GameObject currentItem = null;
    private Item currentItemProps;

    public float fovAngle = 120f;
    public float fovRadius = 15f;

    private bool enemyInView = false;

    private float punchCooldown = 1.0f;
    private float lastPunchTime = -999f;

    private float itemCooldown = 1.0f;
    private float lastItemUseTime = -999f;

    [Header("UI Elements")]
    public Canvas worldSpaceCanvas;
    public TMP_Text playerNameText;
    public Slider healthSlider;

    [Header("Audio")]
    public AudioClip meleeDamageSound;
    private AudioSource audioSource;

    [HideInInspector]
    public string playerName = "Agent";

    [Header("Zone Info")]
    public ZoneManager zoneManager; // ссылка на управление зоной

    // Переменные для временного буста скорости
    private Coroutine speedBoostCoroutine;

    // Для дебаффа зоны (например замедление)
    private bool isInDamageZone = false;
    private float damageZoneDebuffFactor = 0.5f;

    void Start()
    {
        maxMoveSpeed = baseMoveSpeed;

        if (currentItem != null)
            currentItemProps = currentItem.GetComponent<Item>();

        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<ZoneManager>();
            if (zoneManager == null)
                Debug.LogWarning("ZoneManager не найден!");
        }

        if (fovVisualizer != null)
        {
            fovVisualizer.fovAngle = this.fovAngle;
            fovVisualizer.fovRadius = this.fovRadius;
        }

        audioSource = GetComponent<AudioSource>();

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
        FaceCamera();
        LockCanvasTransform();

        // Дебафф зоны влияет на скорость
        maxMoveSpeed = isInDamageZone ? baseMoveSpeed * damageZoneDebuffFactor : baseMoveSpeed;

        if (worldSpaceCanvas != null)
        {
            // Фиксируем локальный поворот (например, только небольшой наклон)
            worldSpaceCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);

            // Поворачиваем только в сторону камеры
            FaceCamera();
        }
    }

    public void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = Mathf.Clamp01(health / maxHealth);
        if (playerNameText != null)
            playerNameText.text = playerName;
    }

    void FaceCamera()
    {
        if (worldSpaceCanvas != null && Camera.main != null)
        {
            Vector3 dir = Camera.main.transform.position - worldSpaceCanvas.transform.position;
            dir.z = 0; // Игнорируем глубину (если используется 2D)
            dir.y = 0;
            dir.x = 0;
            worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(health / maxHealth);
        sensor.AddObservation(maxMoveSpeed / 10f);
        sensor.AddObservation(isInDamageZone ? 1f : 0f);

        if (currentItem != null && currentItemProps != null)
        {
            sensor.AddObservation(1f);
            sensor.AddObservation((int)currentItemProps.itemType / 10f);
            sensor.AddObservation(currentItemProps.ammoCount / 100f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, fovRadius);
        List<GameObject> visibleObjects = new List<GameObject>();
        enemyInView = false;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            Vector2 dirToObj = hit.transform.position - transform.position;
            float angle = Vector2.Angle(transform.up, dirToObj);
            if (angle > fovAngle / 2f) continue;

            visibleObjects.Add(hit.gameObject);

            if (hit.CompareTag("Agent"))
                enemyInView = true;
        }

        int maxVisible = 10;

        for (int i = 0; i < maxVisible; i++)
        {
            if (i < visibleObjects.Count)
            {
                var obj = visibleObjects[i];
                float type = 0f;

                if (obj.CompareTag("Wall")) type = 1f;
                else if (obj.CompareTag("Item")) type = 2f;
                else if (obj.CompareTag("Agent")) type = 3f;

                Vector2 relativePos = obj.transform.position - transform.position;
                relativePos = transform.InverseTransformDirection(relativePos);

                sensor.AddObservation(type / 3f);
                sensor.AddObservation(relativePos.x / fovRadius);
                sensor.AddObservation(relativePos.y / fovRadius);

                if (type == 2f)
                {
                    var itemProps = obj.GetComponent<Item>();
                    sensor.AddObservation((int)(itemProps?.itemType ?? 0) / 10f);
                    sensor.AddObservation((itemProps?.ammoCount ?? 0) / 100f);
                }
                else if (type == 3f)
                {
                    var otherAgent = obj.GetComponent<RLAgent>();
                    float otherHealth = otherAgent != null ? otherAgent.health : 0f;
                    sensor.AddObservation(otherHealth / maxHealth);
                    Vector2 agentForward = obj.transform.up;
                    sensor.AddObservation(agentForward.x);
                    sensor.AddObservation(agentForward.y);
                }
                else
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
            }
            else
            {
                // Пустые данные если нет объекта
                for (int j = 0; j < 7; j++) sensor.AddObservation(0f);
            }
        }

        // Добавляем наблюдения о зоне (расстояние и направление к центру)
        if (zoneManager != null)
        {
            Vector3 dirToCenter = zoneManager.zoneCenter - transform.position;
            float distToCenter = dirToCenter.magnitude;
            sensor.AddObservation(distToCenter / zoneManager.startRadius);

            Vector3 localDir = transform.InverseTransformDirection(dirToCenter.normalized);
            sensor.AddObservation(localDir.x);
            sensor.AddObservation(localDir.y);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float rotationInput = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);

        int pickUp = actions.DiscreteActions[0];
        int drop = actions.DiscreteActions[1];
        int use = actions.DiscreteActions[2];
        int punch = actions.DiscreteActions[3];

        transform.Rotate(Vector3.forward, -rotationInput * rotationSpeed * Time.deltaTime);

        Vector2 moveDir = transform.up * moveY + transform.right * moveX;
        moveDir = Vector2.ClampMagnitude(moveDir, 1f);
        transform.position += (Vector3)(moveDir * maxMoveSpeed * Time.deltaTime);

        if (pickUp == 1) TryPickUpItem();
        if (drop == 1) DropCurrentItem();
        if (use == 1) UseCurrentItem();
        if (punch == 1) TryPunch();

        if (isInDamageZone)
        {
            TakeDamage(0.1f);
            AddReward(-0.001f);
        }
        else
        {
            AddReward(0.001f);
        }

        AddReward(-0.001f); // штраф за каждое действие

        if (enemyInView)
            AddReward(0.01f);

        // Награда за движение в сторону центра безопасной зоны
        if (zoneManager != null)
        {
            Vector3 toCenter = zoneManager.zoneCenter - transform.position;
            if (toCenter.magnitude > 0.01f)
            {
                Vector3 moveDir3D = new Vector3(moveDir.x, moveDir.y, 0f);
                float dot = Vector3.Dot(moveDir3D.normalized, toCenter.normalized);
                AddReward(0.01f * dot);
            }
        }
    }

    void TryPickUpItem()
    {
        if (currentItem != null)
        {
            AddReward(-0.02f);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Item"))
            {
                currentItem = hit.gameObject;
                currentItemProps = currentItem.GetComponent<Item>();
                currentItem.SetActive(false);
                AddReward(0.1f);
                break;
            }
        }
    }

    void LockCanvasTransform()
    {
        if (worldSpaceCanvas != null)
        {
            // Фиксируем только позицию и масштаб
            worldSpaceCanvas.transform.localPosition = Vector3.zero;
            worldSpaceCanvas.transform.localScale = Vector3.one;

            // Фиксируем поворот (только поворот по Z, если нужно)
           worldSpaceCanvas.transform.localRotation = Quaternion.identity;
        }
    }

    void DropCurrentItem()
    {
        if (currentItem == null) return;

        currentItem.SetActive(true);
        currentItem.transform.position = transform.position;
        currentItem = null;
        currentItemProps = null;
        AddReward(-0.01f);
    }

    void UseCurrentItem()
    {
        if (Time.time - lastItemUseTime < itemCooldown) return;
        if (currentItem == null || currentItemProps == null) return;

        // Вызываем метод Use у Item, передаем агенту this
        currentItemProps.Use(this);

        if (currentItemProps.isConsumable)
        {
            // Если предмет одноразовый, выбрасываем его после использования
            Destroy(currentItem);
            currentItem = null;
            currentItemProps = null;
        }

        lastItemUseTime = Time.time;
    }

    void TryPunch()
    {
        if (Time.time - lastPunchTime < punchCooldown) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position + transform.up * 1f, 1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Agent") && hit.gameObject != this.gameObject)
            {
                RLAgent otherAgent = hit.GetComponent<RLAgent>();
                if (otherAgent != null)
                {
                    otherAgent.TakeDamage(10f);
                    AddReward(0.2f);
                    audioSource.clip = meleeDamageSound;
                    audioSource.volume = 0.2f;
                    audioSource.Play();
                }
            }
        }

        lastPunchTime = Time.time;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            health = 0;
            AddReward(-1f);
            EndEpisode();
            Destroy(gameObject);
        }
    }

    // Эти методы вызываются из Item.Use()
    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    public void BoostEnergy(float speedMultiplier)
    {
        if (speedBoostCoroutine != null)
            StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(10f, speedMultiplier));
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        maxMoveSpeed = baseMoveSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        maxMoveSpeed = baseMoveSpeed;
    }

    public void Shoot()
    {
        // Логика выстрела (пока заглушка)
        Debug.Log($"{playerName} выстрелил.");
    }

    public void MeleeAttack(int damage)
    {
        // Ближний бой (удар ножом)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position + transform.up * 1f, 1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Agent") && hit.gameObject != this.gameObject)
            {
                RLAgent otherAgent = hit.GetComponent<RLAgent>();
                if (otherAgent != null)
                {
                    otherAgent.TakeDamage(damage);
                    AddReward(0.2f);
                    audioSource.clip = meleeDamageSound;
                    audioSource.volume = 0.2f;
                    audioSource.Play();
                }
            }
        }
    }

    public void ApplyArmor(int armorValue)
    {
        // Можно реализовать логику брони (например, временный буст здоровья или уменьшение урона)
        // Заглушка:
        Debug.Log($"{playerName} получил броню +{armorValue}");
    }
    public void SetDamageZoneStatus(bool isInDamageZone)
    {
        this.isInDamageZone = isInDamageZone;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("DamageZone"))
        {
            isInDamageZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("DamageZone"))
        {
            isInDamageZone = false;
        }
    }
}
