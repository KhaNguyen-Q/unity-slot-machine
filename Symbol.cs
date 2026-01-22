using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
public class Symbol : MonoBehaviour
{   
    public SymbolType Type { get; set; }
    public SpriteRenderer spriteRenderer { get; private set; }
    
    public bool IsCoin { get; private set; } = false;
    public float CoinValue { get; private set; } = 0;

    public bool IsMultiplier { get; private set; } = false;
    public float MultiplierValue { get; private set; } = 1;

    public float FinalValue { get; set; } = 0;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer is missing on the Symbol prefab!");
        }
        
        // Optional: reset values on awake
        IsCoin = false;
        CoinValue = 0;
        IsMultiplier = false;
        MultiplierValue = 1;
    }

    public void SetSymbol(SymbolType type, Sprite sprite)
    {
        Type = type;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            Debug.Log($"Symbol set: {type}, Sprite: {sprite.name}");
        }

        // Reset special attributes by default
        IsCoin = spriteRenderer.sprite.name.Contains("Coin");
        CoinValue = 0;
        IsMultiplier = spriteRenderer.sprite.name.Contains("Multiplier");
        MultiplierValue = 1;

        // Set attributes based on symbol type
        switch (type)
    {
        case SymbolType.Blank:
            CoinValue = 0;
            break;
        case SymbolType.BronzeCoin:
            IsCoin = true;
            CoinValue = Random.Range(0.2f, 4f);
            break;
        case SymbolType.SilverCoin:
            IsCoin = true;
            CoinValue = Random.Range(5f, 20f);
            break;
        case SymbolType.GoldCoin:
            IsCoin = true;
            CoinValue = Random.Range(25f, 100f);
            break;
        case SymbolType.Diamond:
            IsCoin = true;
            CoinValue = Random.Range(150f, 500f);
            break;
        case SymbolType.GreenClover:
            IsMultiplier = true;
            break;
        case SymbolType.GoldClover:
            IsMultiplier = true;
            break;
        }
        FinalValue = GetCoinValue();

    }

    float GetRandomMultiplier(bool isGold = false)
    {
        float[] multipliers = isGold ? new float[] { 2, 3, 4, 5, 10, 20 } : new float[] { 2, 3, 4, 5 };
        return multipliers[Random.Range(0, multipliers.Length)];
        
    }
    public float GetCoinValue()
    {
        if (spriteRenderer.sprite.name.Contains("Bronze")) return 1f;
        if (spriteRenderer.sprite.name.Contains("Silver")) return 5f;
        if (spriteRenderer.sprite.name.Contains("Gold")) return 10f;
        if (spriteRenderer.sprite.name.Contains("Diamond")) return 20f;
        return 0f;
    }
}

