using UnityEngine;

public enum ItemType
{
    None = 0,
    Medkit = 1,
    EnergyDrink = 2,
    Pistol = 3,
    Knife = 4,
    Helmet = 5
}

public class Item : MonoBehaviour
{
    public ItemType itemType = ItemType.None;

    public int ammoCount = 0;
    public int healAmount = 0;
    public float energyBoost = 0f;
    public int meleeDamage = 0;
    public int armorValue = 0;

    // Помечаем одноразовый предмет или нет
    public bool isConsumable = false;

    public void Use(RLAgent agent)
    {
        if (agent == null) return;

        switch (itemType)
        {
            case ItemType.Medkit:
                if (healAmount > 0)
                {
                    agent.Heal(healAmount);
                    Debug.Log($"{agent.name} использовал аптечку на {healAmount} HP");
                    isConsumable = true;
                }
                break;

            case ItemType.EnergyDrink:
                if (energyBoost > 0f)
                {
                    agent.BoostEnergy(energyBoost);
                    Debug.Log($"{agent.name} выпил энергетик (+{energyBoost} скорости)");
                    isConsumable = true;
                }
                break;

            case ItemType.Pistol:
                if (ammoCount > 0)
                {
                    ammoCount--;
                    agent.Shoot();
                    Debug.Log($"{agent.name} выстрелил из пистолета. Осталось патронов: {ammoCount}");
                }
                else
                {
                    Debug.Log($"{agent.name} попытался выстрелить, но патроны закончились.");
                }
                break;

            case ItemType.Knife:
                if (meleeDamage > 0)
                {
                    agent.MeleeAttack(meleeDamage);
                    Debug.Log($"{agent.name} ударил ножом ({meleeDamage} урона)");
                }
                break;

            case ItemType.Helmet:
                agent.ApplyArmor(armorValue);
                Debug.Log($"{agent.name} надел шлем ({armorValue} брони)");
                isConsumable = true;
                break;

            default:
                Debug.LogWarning($"{agent.name} попытался использовать неизвестный предмет.");
                break;
        }
    }
}
