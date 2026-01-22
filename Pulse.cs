using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
public class Pulse : MonoBehaviour
{
    private Tween pulseTween;

    public void StartPulse()
    {
        if (pulseTween != null && pulseTween.IsActive())
            return; // Already pulsing

        pulseTween = transform.DOScale(Vector3.one * 1.1f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopPulse()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
            transform.localScale = Vector3.one; // Reset scale
        }
    }

    private void OnDisable()
    {
        StopPulse();
    }
}