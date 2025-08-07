using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [SerializeField] private Image fadeImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;            
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }        

        if (fadeImage == null)
        {
            Debug.LogError("[FadeManager] Fade Image is not assigned.");
            return;
        }

        // 초기 투명도 설정
        SetAlpha(0f);
    }

    /// <summary>
    /// 페이드 인: 불투명 -> 투명
    /// </summary>
    /// <param name="duration">페이드 인 시간</param>
    /// <param name="onComplete">페이드 인 이후 할 행동</param>
    public void FadeIn(float duration, Action onComplete = null)
    {
        fadeImage.raycastTarget = true;
        fadeImage.transform.SetAsLastSibling();
        StartCoroutine(Fade(1f, 0f, duration, () =>
        {
            fadeImage.raycastTarget = false;
            fadeImage.transform.SetAsFirstSibling();
            onComplete?.Invoke();
        }));
    }

    /// <summary>
    /// 페이드 아웃: 투명 -> 불투명
    /// </summary>
    /// <param name="duration">페이드 아웃 시간</param>
    /// <param name="onComplete">페이드 아웃 이후 할 행동</param>
    public void FadeOut(float duration, Action onComplete = null)
    {
        fadeImage.raycastTarget = true;
        fadeImage.transform.SetAsLastSibling();
        StartCoroutine(Fade(0f, 1f, duration, onComplete));
    }


    /// <summary>
    /// 페이드 실행 코루틴
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="duration"></param>
    /// <param name="onComplete"></param>
    /// <returns></returns>
    private IEnumerator Fade(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            SetAlpha(alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(to);
        onComplete?.Invoke();
    }


    /// <summary>
    /// 페이드 이미지의 알파값을 설정
    /// </summary>
    /// <param name="alpha"></param>
    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color color = fadeImage.color;
        fadeImage.color = new Color(color.r, color.g, color.b, alpha);
    }
}