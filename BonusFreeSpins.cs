using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class BonusFreeSpins : MonoBehaviour
{
    public int rows = 5;
    public int cols = 6;
    public float symbolSize = 1.2f;

    

    public GameObject jPrefab, aPrefab, qPrefab, kPrefab, beerPrefab,
                      wildPrefab, steakPrefab, breadPrefab, cheesePrefab, bonusPrefab;
    public Transform symbolParent;
    public GameObject victoryPanel;
    public TMP_Text victoryText;
    public TMP_Text freeSpinsText;

    private float spacingX;
    private float spacingY;
    private Vector3 gridOffset;

    public Button spinButton;

    private Symbol[,] grid;
    private GameObject[,] symbolObjects;
    private bool[,] isSticky;

    private int freeSpinsRemaining = 10;
    private int totalWinnings = 0;
  
    //private bool matchResult = false;

    void Start()
    {
        grid = new Symbol[cols, rows];
        symbolObjects = new GameObject[cols, rows];
        isSticky = new bool[cols, rows];

        InitGrid();
        UpdateUI();

        StartCoroutine(PlayFreeSpin());;
    }

    void InitGrid()
    {
    // Get the correct screen size and aspect ratio
    float screenWidth = Camera.main.orthographicSize * Camera.main.aspect * 2;
    float screenHeight = Camera.main.orthographicSize * 2;

    // Calculate the grid spacing
    spacingX = screenWidth / cols;
    spacingY = screenHeight / rows;

    // Adjust grid offset for centering
    gridOffset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);

    // Initialize symbols
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
}

    void CreateSymbolAt(int x, int y, float spacingX, float spacingY, Vector3 gridOffset)
    {
        SymbolType type = GetRandomSymbolType();
        GameObject prefab = GetPrefabForType(type);

        GameObject go = Instantiate(prefab, symbolParent);
        go.transform.localPosition = new Vector3(x * spacingX, -y * spacingY, 0f) + gridOffset;
        go.name = $"Symbol_{x}_{y}";

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        Symbol symbol = go.GetComponent<Symbol>();
        if (symbol == null) symbol = go.AddComponent<Symbol>();

        symbol.SetSymbol(type, sr.sprite);

        grid[x, y] = symbol;
        symbolObjects[x, y] = go;
    }

    void UpdateUI()
    {
        freeSpinsText.text = $"Free Spins: {freeSpinsRemaining}";
        victoryText.text = $"Winnings: {totalWinnings}";
    }


    IEnumerator PlayFreeSpin()
    {
    while (freeSpinsRemaining > 0)
    {
        Debug.Log("▶️ Starting Free Spin");
        freeSpinsRemaining--;
        UpdateUI();

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
                Debug.Log("🔁 Dropping symbols again due to match...");
                yield return StartCoroutine(DropSymbolsWithTween());
            }
        }

        yield return new WaitForSeconds(1f); // delay between spins
    }

    EndBonus(); // ends the bonus
}



    int CalculateWinnings()
    {
        int winnings = 0;

        // Example simple logic: +10 for each Wild on screen
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != null)
                {
                    if (grid[x, y].Type == SymbolType.Wild)
                        winnings += 10;
                    else
                        winnings += 2; // minor win for non-wild
                }
            }
        }

        return winnings;
    }

bool matchFound = false;

IEnumerator CheckAndDestroyMatches(Action<bool, List<Vector2Int>> callback)
{
    List<Vector2Int> toDestroy = new List<Vector2Int>();
    bool matchFound = false;

    // Horizontal match check
    for (int y = 0; y < rows; y++)
    {
        int count = 1;
        for (int x = 1; x < cols; x++)
        {
            SymbolType current = grid[x, y]?.Type ?? SymbolType.Empty;
            SymbolType prev = grid[x - 1, y]?.Type ?? SymbolType.Empty;

            if (current != SymbolType.Empty && current == prev)
            {
                count++;
            }
            else
            {
                if (count >= 3)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int matchedX = x - 1 - i;
                        if (!isSticky[matchedX, y])
                            toDestroy.Add(new Vector2Int(matchedX, y));
                    }
                    matchFound = true;
                    Debug.Log($"💥 Horizontal match of {count} at row {y}, ending at column {x - 1}");
                }
                count = 1;
            }
        }

        // Check for trailing match at end of row
        if (count >= 3)
        {
            for (int i = 0; i < count; i++)
            {
                int matchedX = cols - 1 - i;
                if (!isSticky[matchedX, y])
                    toDestroy.Add(new Vector2Int(matchedX, y));
            }
            matchFound = true;
            Debug.Log($"💥 Horizontal match of {count} at row {y}, ending at column {cols - 1}");
        }
    }

    // Vertical match check
    for (int x = 0; x < cols; x++)
    {
        int count = 1;
        for (int y = 1; y < rows; y++)
        {
            SymbolType current = grid[x, y]?.Type ?? SymbolType.Empty;
            SymbolType prev = grid[x, y - 1]?.Type ?? SymbolType.Empty;

            if (current != SymbolType.Empty && current == prev)
            {
                count++;
            }
            else
            {
                if (count >= 3)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int matchedY = y - 1 - i;
                        if (!isSticky[x, matchedY])
                            toDestroy.Add(new Vector2Int(x, matchedY));
                    }
                    matchFound = true;
                    Debug.Log($"💥 Vertical match of {count} at column {x}, ending at row {y - 1}");
                }
                count = 1;
            }
        }

        // Check for trailing match at end of column
        if (count >= 3)
        {
            for (int i = 0; i < count; i++)
            {
                int matchedY = rows - 1 - i;
                if (!isSticky[x, matchedY])
                    toDestroy.Add(new Vector2Int(x, matchedY));
            }
            matchFound = true;
            Debug.Log($"💥 Vertical match of {count} at column {x}, ending at row {rows - 1}");
        }
    }

    // Destroy matched (non-sticky) symbols
    foreach (Vector2Int pos in toDestroy)
    {
        if (symbolObjects[pos.x, pos.y] != null)
        {
            Destroy(symbolObjects[pos.x, pos.y]);
            grid[pos.x, pos.y] = null;
            symbolObjects[pos.x, pos.y] = null;
            isSticky[pos.x, pos.y] = false;

            Debug.Log($"🔥 Destroyed symbol at ({pos.x}, {pos.y})");
        }
    }

    yield return new WaitForSeconds(0.2f);
    callback(matchFound, toDestroy);
}



    void TriggerBonusFeature()
    {
    Debug.Log("🎉 BONUS TRIGGERED!");
    SceneManager.LoadScene("BonusSelectionUI", LoadSceneMode.Additive);
    }

    IEnumerator InitialDropSymbols()
{
    Debug.Log("⏬ InitialDropSymbols called");
    
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

    activeTweens.Clear();

    for (int x = 0; x < cols; x++)
    {
        int emptyCount = 0;

        for (int y = rows - 1; y >= 0; y--)
        {
            if (grid[x, y] == null || grid[x, y].Type == SymbolType.Empty)
            {
                emptyCount++;
            }
            else if (emptyCount > 0)
            {
                grid[x, y + emptyCount] = grid[x, y];
                symbolObjects[x, y + emptyCount] = symbolObjects[x, y];
                isSticky[x, y + emptyCount] = isSticky[x, y];

                grid[x, y] = null;
                symbolObjects[x, y] = null;
                isSticky[x, y] = false;

                Tweener tween = symbolObjects[x, y + emptyCount].transform.DOLocalMove(
                    GetSymbolWorldPosition(x, y + emptyCount), 0.3f
                ).SetEase(Ease.OutBounce);

                activeTweens.Add(tween);
            }
        }

        for (int i = 0; i < emptyCount; i++)
        {
            int y = i;

            // SKIP SPAWN IF STICKY SYMBOL ALREADY EXISTS
            if (isSticky[x, y] && grid[x, y] != null)
            {
                Debug.Log($"🧲 Sticky Wild at ({x}, {y}) is preserved.");
                continue;
            }

            SymbolType symbolType = GetRandomSymbolType();
            GameObject prefab = GetPrefabForType(symbolType);
            GameObject go = Instantiate(prefab, symbolParent);

            Vector3 startPos = GetSymbolWorldPosition(x, -1 - i);
            Vector3 targetPos = GetSymbolWorldPosition(x, y);
            go.transform.localPosition = startPos;

            Tweener tween = go.transform.DOLocalMove(targetPos, 0.3f).SetEase(Ease.OutBounce);
            activeTweens.Add(tween);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            Symbol symbol = go.GetComponent<Symbol>() ?? go.AddComponent<Symbol>();
            symbol.SetSymbol(symbolType, sr.sprite);

            grid[x, y] = symbol;
            symbolObjects[x, y] = go;

            // MARK STICKY IF WILD
            isSticky[x, y] = (symbolType == SymbolType.Wild);
            if (isSticky[x, y])
                Debug.Log($"📌 Wild at ({x}, {y}) is now sticky.");
        }
    }

    yield return new WaitUntil(() => activeTweens.All(t => t != null && !t.IsActive()));
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
        default: basePayout = 1; break;
    }

    return basePayout * count;
    }
    SymbolType GetRandomSymbolType()
    {
    float roll = UnityEngine.Random.value; 
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

    List<Vector2Int> FindMatches()
{
    List<Vector2Int> matches = new List<Vector2Int>();

    for (int x = 0; x < cols; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            Symbol current = grid[x, y];
            if (current == null || current.Type == SymbolType.Empty)
                continue;

            // Horizontal match (you can expand this)
            if (x <= cols - 3 &&
                grid[x + 1, y]?.Type == current.Type &&
                grid[x + 2, y]?.Type == current.Type)
            {
                matches.Add(new Vector2Int(x, y));
                matches.Add(new Vector2Int(x + 1, y));
                matches.Add(new Vector2Int(x + 2, y));
            }

            // Vertical match (same logic)
            if (y <= rows - 3 &&
                grid[x, y + 1]?.Type == current.Type &&
                grid[x, y + 2]?.Type == current.Type)
            {
                matches.Add(new Vector2Int(x, y));
                matches.Add(new Vector2Int(x, y + 1));
                matches.Add(new Vector2Int(x, y + 2));
            }
        }
    }

    return matches.Distinct().ToList();
}
    void EndBonus()
    {
    Debug.Log($"Bonus Over! Total winnings: {totalWinnings}");

    // Show victory panel
    victoryPanel.SetActive(true);
    victoryText.text = $"You Won {totalWinnings} Coins!";

    // After a delay, return to main game
    StartCoroutine(ReturnToMainGameAfterDelay(3f));
    }

    IEnumerator ReturnToMainGameAfterDelay(float delay)
    {
    yield return new WaitForSeconds(delay);
    SceneManager.LoadScene("MainGameScene"); // Replace with your actual base game scene name
    }
   
        Vector3 GetSymbolWorldPosition(int x, int y)
    {
    float spacingX = Camera.main.orthographicSize * Camera.main.aspect * 2 / cols * 0.9f;
    float spacingY = Camera.main.orthographicSize * 2 / rows * 0.9f;
    Vector3 offset = new Vector3(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f, 0f);
    return new Vector3(x * spacingX, -y * spacingY, 0f) + offset;
    }
}

