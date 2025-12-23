using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject agentPrefab;
    public GameObject[] itemPrefabs;

    public int minAgents = 2;
    public int maxAgents = 4;
    public int itemCount = 10;

    public Vector2 spawnAreaMin = new Vector2(-8f, -4f);
    public Vector2 spawnAreaMax = new Vector2(8f, 4f);

    public float minDistanceBetweenAgents = 2.0f;

    private List<GameObject> agents = new List<GameObject>();

    [Header("Agent Names")]
    public List<string> agentNames = new List<string>() {
        "Hecesper", "Voruga", "dcocd", "RUSSKIY_NADZOR", "Karma_Union", "lowpolycat", "hookyo"
    };

    [Header("Agent UI")]
    // Префаб с Canvas (World Space), TMP_Text и Slider, который будет отображать имя и здоровье агента
    public GameObject agentUIPrefab;

    void Start()
    {
        SpawnAgents();
        SpawnFilteredItems();
    }

    void Update()
    {
        CheckAgentsAlive();
    }

    void SpawnAgents()
    {
        int agentCount = Random.Range(minAgents, maxAgents + 1);

        // Создаём копию списка имён, чтобы не повторять
        List<string> availableNames = new List<string>(agentNames);

        for (int i = 0; i < agentCount; i++)
        {
            Vector2 pos;
            int attempts = 0;
            do
            {
                pos = GetRandomPosition();
                attempts++;
                if (attempts > 100)
                {
                    Debug.LogWarning("Не удалось найти свободную позицию для агента.");
                    break;
                }
            } while (!IsPositionFarFromAgents(pos, minDistanceBetweenAgents));

            Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            GameObject agent = Instantiate(agentPrefab, pos, rot);

            // Выбираем уникальное имя
            string chosenName = "Agent_" + (i + 1);
            if (availableNames.Count > 0)
            {
                int index = Random.Range(0, availableNames.Count);
                chosenName = availableNames[index];
                availableNames.RemoveAt(index);
            }

            agent.name = chosenName;

            // Передаем имя и UI в агента
            RLAgent rlAgent = agent.GetComponent<RLAgent>();
            if (rlAgent != null)
            {
                rlAgent.playerName = chosenName;

                if (agentUIPrefab != null)
                {
                    // Создаём UI как дочерний объект агента
                    GameObject uiInstance = Instantiate(agentUIPrefab, agent.transform);
                    rlAgent.worldSpaceCanvas = uiInstance.GetComponent<Canvas>();
                    rlAgent.playerNameText = uiInstance.GetComponentInChildren<TMPro.TMP_Text>();
                    rlAgent.healthSlider = uiInstance.GetComponentInChildren<UnityEngine.UI.Slider>();

                    // Обновим UI сразу
                    rlAgent.UpdateUI();
                }
            }

            agents.Add(agent);
        }
    }

    void SpawnFilteredItems()
    {
        List<GameObject> filteredPrefabs = new List<GameObject>();
        foreach (var prefab in itemPrefabs)
        {
            if (prefab.TryGetComponent<Item>(out Item item))
            {
                if (item.itemType == ItemType.Knife || item.itemType == ItemType.Medkit)
                    filteredPrefabs.Add(prefab);
            }
        }

        if (filteredPrefabs.Count == 0)
        {
            Debug.LogWarning("Нет подходящих предметов для спавна (нужны Knife и Medkit).");
            return;
        }

        for (int i = 0; i < itemCount; i++)
        {
            Vector2 pos = GetRandomPosition();
            GameObject prefab = filteredPrefabs[Random.Range(0, filteredPrefabs.Count)];
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    Vector2 GetRandomPosition()
    {
        return new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );
    }

    bool IsPositionFarFromAgents(Vector2 position, float minDistance)
    {
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            if (Vector2.Distance(agent.transform.position, position) < minDistance)
                return false;
        }
        return true;
    }

    void CheckAgentsAlive()
    {
        agents.RemoveAll(agent => agent == null);

        if (agents.Count == 1)
        {
            // Последний выживший — даём награду
            RLAgent lastAgent = agents[0].GetComponent<RLAgent>();
            if (lastAgent != null)
            {
                lastAgent.AddReward(2.0f); // Большая награда за победу
                lastAgent.EndEpisode();
            }
            Debug.Log("Остался последний агент — перезапуск сцены.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else if (agents.Count == 0)
        {
            Debug.Log("Все агенты мертвы — перезапуск сцены.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

}
