using UnityEngine;
using UnityEngine.SceneManagement;

public class BonusSelectionUI : MonoBehaviour
{
    public void LoadRefillLivesBonus()
    {
        Debug.Log("🎉 REFILLBONUS TRIGGERED!");
        SceneManager.LoadScene("BonusRefill");
    }

    public void LoadFreeSpinsBonus()
    {
        Debug.Log("🎉 FREESPINSBONUS TRIGGERED!");
        SceneManager.LoadScene("BonusFreeSpins");
    }
}
