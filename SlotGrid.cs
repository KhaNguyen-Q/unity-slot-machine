using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class SlotGrid : MonoBehaviour
{
    public int rows = 5;
    public int cols = 6;
    public float symbolSize = 1.2f;

    public GameObject jPrefab, aPrefab, qPrefab, kPrefab, beerPrefab,
                      wildPrefab, steakPrefab, breadPrefab, cheesePrefab, bonusPrefab;
    public Transform symbolParent;
    public Button spinButton;
    public TMP_Text balanceText;
    public int balance = 1000; // starting balance

    private int spinCost = 20;

    private bool isSpinning = false;
    private Symbol[,] grid;
    private GameObject[,] symbolObjects;

    private float spacingX;
    private float spacingY;
    private Vector3 gridOffset;


    void Start()
    {
    grid = new Symbol[cols, rows];
    symbolObjects = new GameObject[cols, rows];

    InitGrid();
    UpdateBalanceUI();

    spinButton.onClick.AddListener(OnSpinButtonPressed);  // Link the spin button to the method
    }

    void InitGrid()
    {
    float screenWidth = Camera.main.orthographicSize * Camera.main.aspect * 2;
    float screenHeight = Camera.main.orthographicSize * 2;

    float spacingX = screenWidth / cols * 0.9f;
    float spacingY = screenHeight / rows * 0.9f;

    Vector3 gridOffset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);

    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            SymbolType symbolType = GetRandomSymbolType();
            GameObject prefab = GetPrefabForType(symbolType);

            GameObject go = Instantiate(prefab, symbolParent);
            go.transform.localPosition = new Vector3(x * spacingX, -y * spacingY, 0f) + gridOffset;
            go.name = $"Symbol_{x}_{y}";

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();

            Symbol symbol = go.GetComponent<Symbol>();
            if (symbol == null) symbol = go.AddComponent<Symbol>();

            symbol.SetSymbol(symbolType, sr.sprite);

            grid[x, y] = symbol;
            symbolObjects[x, y] = go;
        }
    }

    Debug.Log("Grid initialized and centered.");
    }

    void OnSpinButtonPressed()
    {
        if (isSpinning || balance < spinCost) return;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
            if (symbolObjects[x, y] != null)
            {
                Destroy(symbolObjects[x, y]); // Destroy existing symbols
                symbolObjects[x, y] = null;
            }
            grid[x, y] = null; // Clear grid positions
        }
    }

        balance -= spinCost;
        UpdateBalanceUI();
        spinButton.interactable = false;
        StartCoroutine(PlaySlotCycle());
    }

    void UpdateBalanceUI()
    {
        balanceText.text = $"Balance: {balance}";
    }


    void TriggerBonusFeature()
    {
    Debug.Log("🎉 BONUS TRIGGERED!");
    SceneManager.LoadScene("BonusSelectionUI", LoadSceneMode.Additive);
    }

    IEnumerator PlaySlotCycle()
    {
    isSpinning = true;

    yield return StartCoroutine(InitialDropSymbols());

    bool keepTumbling = true;
    while (keepTumbling)
    {
        yield return StartCoroutine(CheckAndDestroyMatches((matchFound, destroyedList) =>
        {
            keepTumbling = matchFound;
        }));

        if (keepTumbling)
        {
            yield return StartCoroutine(DropSymbolsWithTween());
        }
        
    }

    spinButton.interactable = true;
    isSpinning = false;
    }


IEnumerator CheckAndDestroyMatches(System.Action<bool, List<(int x, int y)>> callback)
{
    Dictionary<SymbolType, List<(int x, int y)>> symbolGroups = new();

    // Collect all non-empty symbol positions grouped by type
    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            Symbol symbol = grid[x, y];
            if (symbol != null && symbol.Type != SymbolType.Empty)
            {
                if (!symbolGroups.ContainsKey(symbol.Type))
                    symbolGroups[symbol.Type] = new List<(int x, int y)>();
                symbolGroups[symbol.Type].Add((x, y));
            }
        }
    }

    bool foundMatch = false;
    List<(int x, int y)> toDestroy = new();

    foreach (var kvp in symbolGroups)
    {
        SymbolType type = kvp.Key;
        List<(int x, int y)> positions = kvp.Value;

        if (positions.Count >= 8)
        {
            foundMatch = true;
            toDestroy.AddRange(positions);

            // Payout (you can expand with animations or effects per symbol)
            int payout = GetPayoutForSymbol(type, positions.Count);
            balance += payout;
            UpdateBalanceUI();

            Debug.Log($"Tumbled {positions.Count} {type} symbols for {payout} payout");

    
        }
        if (type == SymbolType.Bonus && positions.Count >= 4) // Trigger bonus if 4+ Bonus symbols
            {
                TriggerBonusFeature();
            }
    }

    if (foundMatch)
    {
        foreach (var (x, y) in toDestroy)
        {
            if (symbolObjects[x, y] != null)
            {
                grid[x, y].Type = SymbolType.Empty;
                symbolObjects[x, y].transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    Destroy(symbolObjects[x, y]);
                });
            }
        }
        yield return new WaitForSeconds(0.35f);
    }

    callback(foundMatch, toDestroy);    
    }
    IEnumerator InitialDropSymbols()
    {
    float dropDuration = 0.3f;

    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            if (grid[x, y] == null || grid[x, y].Type == SymbolType.Empty)
            {
                SymbolType symbolType = GetRandomSymbolType();
                GameObject prefab = GetPrefabForType(symbolType);

                GameObject go = Instantiate(prefab, symbolParent);
                float spacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
                float spacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;

                Vector3 gridOffset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);
                Vector3 targetPosition = new Vector3(x * spacingX, -y * spacingY, 0f) + gridOffset;
                Vector3 startPosition = targetPosition + new Vector3(0, spacingY * rows, 0);

                go.transform.localPosition = startPosition;

                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();

                Symbol symbol = go.GetComponent<Symbol>();
                if (symbol == null) symbol = go.AddComponent<Symbol>();
                symbol.SetSymbol(symbolType, sr.sprite);

                grid[x, y] = symbol;
                symbolObjects[x, y] = go;

                go.transform.DOLocalMove(targetPosition, dropDuration).SetEase(Ease.OutBounce);
                go.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 8, 1f);

                yield return new WaitForSeconds(0.02f);
            }
        }
    }

    yield return new WaitForSeconds(0.4f);
    }


private List<Tweener> activeTweens = new List<Tweener>();
IEnumerator DropSymbolsWithTween()
{
    float spacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
    float spacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;
    Vector3 offset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);

    Vector3 GetSymbolWorldPosition(int x, int y)
    {
        return new Vector3(x * spacingX, -y * spacingY, 0f) + offset;
    }

    // Clear out previous tweens before starting a new one
    activeTweens.Clear();

    for (int x = 0; x < cols; x++)
    {
        int emptyCount = 0;

        // Go bottom to top in each column
        for (int y = rows - 1; y >= 0; y--)
        {
            if (grid[x, y] == null || grid[x, y].Type == SymbolType.Empty)
            {
                emptyCount++;
            }
            else if (emptyCount > 0)
            {
                // Move symbol down logically
                grid[x, y + emptyCount] = grid[x, y];
                symbolObjects[x, y + emptyCount] = symbolObjects[x, y];

                // Clear original position
                grid[x, y] = null;
                symbolObjects[x, y] = null;

                // Animate to new position and add it to active tweens
                Tweener tween = symbolObjects[x, y + emptyCount].transform.DOLocalMove(
                    GetSymbolWorldPosition(x, y + emptyCount),
                    0.3f
                ).SetEase(Ease.OutBounce);
                activeTweens.Add(tween);
            }
        }

        // Fill new symbols at the top
        for (int i = 0; i < emptyCount; i++)
        {
            int y = i;
            SymbolType symbolType = GetRandomSymbolType();
            GameObject prefab = GetPrefabForType(symbolType);
            GameObject go = Instantiate(prefab, symbolParent);

            Vector3 startPos = GetSymbolWorldPosition(x, -1 - i); // above the grid
            Vector3 targetPos = GetSymbolWorldPosition(x, y);

            go.transform.localPosition = startPos;

            Tweener tween = go.transform.DOLocalMove(targetPos, 0.3f).SetEase(Ease.OutBounce);
            activeTweens.Add(tween);

            Symbol symbol = go.GetComponent<Symbol>();
            if (symbol == null) symbol = go.AddComponent<Symbol>();

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();

            symbol.SetSymbol(symbolType, sr.sprite);

            // Update grid
            grid[x, y] = symbol;
            symbolObjects[x, y] = go;
        }
    }

    // Wait for all tweens to finish
    yield return new WaitUntil(() => activeTweens.All(t => !t.IsActive()));

    // Now that all tweens are finished, check for matches and destroy them
    yield return StartCoroutine(CheckAndDestroyMatches((foundMatch, toDestroy) => 
    {
        if (foundMatch)
        {
            // If matches are found, continue the tumbling process
            StartCoroutine(InitialDropSymbols());
        }
        else
        {
            // No more matches, reset the slot state
           OnSpinButtonPressed();
        }
    }));
}
    

    SymbolType GetRandomSymbolType()
    {
    float roll = Random.value;

    if (roll < 0.20f) return SymbolType.J;          // 20%
    if (roll < 0.40f) return SymbolType.A;          // 15%
    if (roll < 0.50f) return SymbolType.Q;          // 15%
    if (roll < 0.60f) return SymbolType.K;          // 10%
    if (roll < 0.70f) return SymbolType.Beer;       // 10%
    if (roll < 0.80f) return SymbolType.Steak;       // 10%
    if (roll < 0.85f) return SymbolType.Cheese;     // 8%
    if (roll < 0.90f) return SymbolType.Bread;      // 6%
    if (roll < 0.95f) return SymbolType.Wild;       // 4%
    return SymbolType.Bonus;                        // 2%
    }

    GameObject GetPrefabForType(SymbolType type)
    {
        switch (type)
        {
            case SymbolType.J: return jPrefab;
            case SymbolType.A: return aPrefab;
            case SymbolType.Q: return qPrefab;
            case SymbolType.K: return kPrefab;
            case SymbolType.Beer: return beerPrefab;
            case SymbolType.Wild: return wildPrefab;
            case SymbolType.Steak: return steakPrefab;
            case SymbolType.Bread: return breadPrefab;
            case SymbolType.Cheese: return cheesePrefab;
            case SymbolType.Bonus: return bonusPrefab;
            default: return jPrefab;
        }
    }

    int GetPayoutForSymbol(SymbolType type, int count)
    {
    int basePayout;

    switch (type)
    {
        case SymbolType.J: basePayout = 1; break;
        case SymbolType.A: basePayout = 2; break;
        case SymbolType.Q: basePayout = 5; break;
        case SymbolType.K: basePayout = 8; break;
        case SymbolType.Beer: basePayout = 10; break;
        case SymbolType.Cheese: basePayout = 11; break;
        case SymbolType.Steak: basePayout = 12; break;
        case SymbolType.Bread: basePayout = 15; break;
        case SymbolType.Wild: basePayout = 15; break;
        case SymbolType.Bonus: basePayout = 0; break; // bonus payout handled separately
        default: basePayout = 0; break;
    }

    return basePayout * count;
    }
    Vector3 GetSymbolWorldPosition(int x, int y)
    {
    float spacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
    float spacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;
    Vector3 offset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);
    return new Vector3(x * spacingX, -y * spacingY, 0f) + offset;
    }

}
