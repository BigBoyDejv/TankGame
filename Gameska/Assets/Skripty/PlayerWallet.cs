using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int money = 0;

    public void AddMoney(int amount)
    {
        money += amount;
        Debug.Log("Peniaze: " + money);
    }

    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            Debug.Log("Minul si " + amount + ". Zostatok: " + money);
            return true;
        }
        else
        {
            Debug.Log("Nedostatok peňazí!");
            return false;
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 30, 200, 20), "Peniaze: $" + money);
    }
}