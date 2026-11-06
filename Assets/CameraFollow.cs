using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Координаты следования")]
    [SerializeField] private Transform target; // Ссылка на игрока (объект, за которым следим)
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // Смещение камеры относительно игрока

    [Header("Настройки плавности")]
    [SerializeField] private float smoothTime = 0.25f; // Время сглаживания (чем меньше, тем быстрее камера догоняет)
    
    private Vector3 currentVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // Вычисляем позицию, в которую камера должна прийти
        Vector3 targetPosition = target.position + offset;

        // Плавно перемещаем камеру из текущей позиции в целевую
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
