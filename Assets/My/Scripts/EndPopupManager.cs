using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndPopupManager : MonoBehaviour
{
    public List<GameObject> popUpElements;

    private void Awake()
    {
        popUpElements = new List<GameObject>();
    }

    /// <summary>
    /// 리스트 안의 모든 이미지의 알파를 0 → 1로 천천히 올린다.
    /// </summary>
    public void FadeInAllImages(float durationPerImage = 1f)
    {
        StartCoroutine(FadeInImagesSequentially(durationPerImage));
    }

    private IEnumerator FadeInImagesSequentially(float durationPerImage)
    {
        foreach (var obj in popUpElements)
        {
            if (obj == null) continue;

            Image img = obj.GetComponent<Image>();
            if (img == null) continue;

            // 시작 시 알파 0
            Color c = img.color;
            c.a = 0f;
            img.color = c;

            float elapsed = 0f;
            while (elapsed < durationPerImage)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / durationPerImage);

                c.a = alpha;
                img.color = c;

                yield return null;
            }

            // 안전하게 알파 1 고정
            c.a = 1f;
            img.color = c;
        }
    }
}
