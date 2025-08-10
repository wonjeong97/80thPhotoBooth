using UnityEngine;

public class ButtonAnim : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1f; // 한 주기 속도
    [SerializeField] private float minZ = -30f;        // 최소 Z 회전값
    [SerializeField] private float maxZ = 0f;          // 최대 Z 회전값

    private void Update()
    {
        // PingPong으로 0~1 값을 왕복
        float t = Mathf.PingPong(Time.time * rotationSpeed, 1f);

        // 0~1 → maxZ~minZ 사이 값으로 변환
        float zRotation = Mathf.Lerp(maxZ, minZ, t);

        transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }
}
