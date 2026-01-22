using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class BonusRefill : MonoBehaviour
{
    [Header("Settings")]
    public int startingLives = 3;
    public float spinSpeed = 1f;
    public float FinalValue;

    [Header("UI")]
    public GameObject heartPrefab;
    public Transform heartsUI;

    [Header("Symbol Prefabs")]
    public GameObject bronzeCoinPrefab, silverCoinPrefab, goldCoinPrefab, diamondCoinPrefab, blankPrefab;
    public GameObject greenCloverPrefab, goldCloverPrefab;

    [Header("Grid Settings")]
    [SerializeField] public int cols = 6;
    [SerializeField] public int rows = 5;
    [SerializeField] private Transform gridParent;

  
    private int remainingLives;
 
    private GameObject[,] symbolGrid;
    private bool[,] isLocked;
    private List<GameObject> cloverObjects = new();

    private float symbolSpacingX;
    private float symbolSpacingY;



    private void Start()
    {
        Debug.Log("Start method called");
        remainingLives = startingLives;
        symbolGrid = new GameObject[cols, rows];
        isLocked = new bool[cols, rows];
        CreateInitialGrid();
        UpdateHeartUI();
        StartCoroutine(PlayBonusRefillSpin());
    }
private IEnumerator PlayBonusRefillSpin()
{
    while (remainingLives > 0)
    {
        bool spinLandedNewSymbol = false;

        yield return StartCoroutine(DropSymbolsWithTween(result => spinLandedNewSymbol = result));

        if (!spinLandedNewSymbol)
        {
            remainingLives--;
            UpdateHeartUI();
            Debug.Log("Dead spin! Remaining lives: " + remainingLives);
        }
        else
        {
            remainingLives = startingLives;  // 🟢 RESET TO 3!
            UpdateHeartUI();
            Debug.Log("New symbols landed! Lives reset to: " + remainingLives);
        }

        if (remainingLives <= 0)
        {
            Debug.Log("No remaining lives. Ending bonus round.");
            StartCoroutine(EndBonusAndReturn());
            yield break;
        }

        yield return new WaitForSeconds(1f);
    }
}



    private void CreateInitialGrid()
    {
    // Calculate symbol spacing based on screen size
    symbolSpacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
    symbolSpacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;

    Vector3 gridOffset = new Vector3(-(cols - 1) * symbolSpacingX / 2f, (rows - 1) * symbolSpacingY / 2f, 0f);

    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            Vector3 spawnPos = new Vector3(x * symbolSpacingX, y * -symbolSpacingY, 0f);
            GameObject blank = Instantiate(blankPrefab, gridParent);
            blank.transform.localPosition = spawnPos + gridOffset;
            symbolGrid[x, y] = blank;
            isLocked[x, y] = false;
        }
    }

    Debug.Log("Grid created with size: " + cols + "x" + rows);
    Debug.Log("Symbol spacing: " + symbolSpacingX + "x" + symbolSpacingY);
    }
    private void UpdateHeartUI()

    {
    foreach (Transform child in heartsUI)
        Destroy(child.gameObject);

    for (int i = 0; i < remainingLives; i++)
    {
        GameObject heart = Instantiate(heartPrefab, heartsUI);
        RectTransform rt = heart.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); // Top-left anchor
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20 + i * 60, -20); // offset down
        rt.localScale = Vector3.one;
    }
}

private IEnumerator DropSymbolsWithTween(System.Action<bool> onSpinComplete)
{
    List<Tween> tweens = new List<Tween>();
    float symbolSpacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
    float symbolSpacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;
    Vector3 gridOffset = new Vector3(-(cols - 1) * symbolSpacingX / 2f, (rows - 1) * symbolSpacingY / 2f, 0f);

    Vector3 GetSymbolWorldPosition(int x, int y)
    {
        return new Vector3(x * symbolSpacingX, -y * symbolSpacingY, 0f) + gridOffset;
    }
    bool anyNewSymbolLanded = false;

    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            if (!isLocked[x, y])
            {
                SymbolType symbolToSpawn = GetRandomBonusSymbolType();
                Vector3 startPos = GetSymbolWorldPosition(x, -1 - y); // above the grid
                Vector3 targetPos = GetSymbolWorldPosition(x, y);

                GameObject newSymbol = Instantiate(GetPrefabFromSymbolType(symbolToSpawn), gridParent);
                newSymbol.transform.localPosition = startPos;

                Tween dropTween = newSymbol.transform.DOLocalMove(targetPos, 0.3f).SetEase(Ease.OutBounce);
                tweens.Add(dropTween);

                Destroy(symbolGrid[x, y]);
                symbolGrid[x, y] = newSymbol;

                // Track coins/clovers landed this spin
                if (symbolToSpawn == SymbolType.BronzeCoin || symbolToSpawn == SymbolType.SilverCoin ||
                    symbolToSpawn == SymbolType.GoldCoin || symbolToSpawn == SymbolType.Diamond ||
                    symbolToSpawn == SymbolType.GreenClover || symbolToSpawn == SymbolType.GoldClover)
                {
                    isLocked[x, y] = true;
                    anyNewSymbolLanded = true;
                }
                else
                {
                    isLocked[x, y] = false;
                }
            }
        }
    }

    yield return DOTween.Sequence().AppendInterval(0.5f);
    yield return DOTween.Sequence().Join(DOTween.Sequence().AppendCallback(() =>
    {
    foreach (Tween tween in tweens)
    {
        tween.Play();
    }
    }).AppendInterval(0.3f));
    onSpinComplete?.Invoke(anyNewSymbolLanded);
    }
   private GameObject GetPrefabFromSymbolType(SymbolType type)  
    {
    switch (type)
    {
        case SymbolType.BronzeCoin:
            return bronzeCoinPrefab;
        case SymbolType.SilverCoin:
            return silverCoinPrefab;
        case SymbolType.GoldCoin:
            return goldCoinPrefab;
        case SymbolType.Diamond:
            return diamondCoinPrefab;
        case SymbolType.GreenClover:
            return greenCloverPrefab;
        case SymbolType.GoldClover:
            return goldCloverPrefab;
        default:
            // Return blank prefab for any other type;
            return blankPrefab;
    }
}
   
    private bool CheckForNewCoins()
    {
    foreach (var obj in symbolGrid)
    {
        if (obj != null)
        {
            Symbol sym = obj.GetComponent<Symbol>();
            if (sym != null && (sym.IsMultiplier || sym.GetCoinValue() > 0f))
                return true;
        }
    }
    return false;
    }
    private SymbolType GetRandomBonusSymbolType()
    {
    float rand = Random.value;
    if (rand < 0.50f) return SymbolType.Blank;
    else if (rand < 0.70f) return SymbolType.BronzeCoin;
    else if (rand < 0.85f) return SymbolType.SilverCoin;
    else if (rand < 0.93f) return SymbolType.GoldCoin;
    else if (rand < 0.97f) return SymbolType.GreenClover;
    else if (rand < 0.99f) return SymbolType.GoldClover;
    else return SymbolType.Diamond; // Rarely spawn a diamond
    }
        private float GetCoinValue()
    {
        if (gameObject.name.Contains("Bronze")) return 1f;
        if (gameObject.name.Contains("Silver")) return 5f;
        if (gameObject.name.Contains("GoldCoin")) return 10f;
        if (gameObject.name.Contains("Diamond")) return 20f;
        return 0f;
    }
    private IEnumerator EndBonusAndReturn()
    {
    float total = 0;

    // Check for and sum the coin values
    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            GameObject symbolObj = symbolGrid[x, y];
            if (symbolObj != null)
            {
                Symbol sym = symbolObj.GetComponent<Symbol>();
                if (sym != null && sym.IsCoin)
                {
                    total += sym.GetCoinValue(); // Add coin value to total
                }
            }
        }
    }

    Debug.Log($"Bonus Ended. Total Winnings: {total}");

    // TODO: Transfer winnings to balance
    yield return new WaitForSeconds(2f);
    SceneManager.LoadScene("LeGroundHogBaseSpins");
    }

    Vector3 GetSymbolWorldPosition(float x, float y)
    {
    float spacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
    float spacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;
    Vector3 offset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);
    return new Vector3(x * spacingX, -y * spacingY, 0f) + offset;
    }

}
