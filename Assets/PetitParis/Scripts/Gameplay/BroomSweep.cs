using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 빗자루 쓸기 동작. 손잡이 회전축(pivot, 사람 손 위치)을 좌우로 흔들어 빗자루 머리가 바닥을 쓸게 한다.
    /// - 빗자루 머리(head)에 FootstepHazard가 있으면 쥐가 쓸려 나간다 (실패)
    /// - Frantic(추격 중)이면 더 빠르고 넓게 쓴다
    /// - 쓰는 소리는 3D 루프
    /// pivot은 사람 몸(Body)의 자식이라 몸을 따라 움직이고 돈다.
    /// </summary>
    [DisallowMultipleComponent]
    public class BroomSweep : MonoBehaviour
    {
        [SerializeField] Transform pivot;
        [SerializeField, Range(0f, 80f)] float calmAngle = 25f;
        [SerializeField, Range(0.1f, 3f)] float calmSpeed = 0.9f;
        [SerializeField, Range(0f, 80f)] float franticAngle = 45f;
        [SerializeField, Range(0.1f, 5f)] float franticSpeed = 2.4f;
        [SerializeField] AudioSource sweepAudio;

        float phase;
        float amount;   // 0 = calm, 1 = frantic (부드럽게 전환)
        Quaternion baseRotation;

        public bool Frantic { get; set; }

        void Awake()
        {
            if (pivot == null) pivot = transform;
            baseRotation = pivot.localRotation;
            PetitParisAudio.Configure3D(sweepAudio, 0.3f, 8f);
        }

        void Start()
        {
            if (sweepAudio == null) return;
            sweepAudio.clip = PetitParisAudio.Broom;
            sweepAudio.loop = true;
            sweepAudio.volume = 0.35f;
            sweepAudio.Play();
        }

        void Update()
        {
            amount = Mathf.MoveTowards(amount, Frantic ? 1f : 0f, Time.deltaTime * 2f);
            float speed = Mathf.Lerp(calmSpeed, franticSpeed, amount);
            float angle = Mathf.Lerp(calmAngle, franticAngle, amount);
            phase += Time.deltaTime * speed * Mathf.PI * 2f;
            pivot.localRotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(phase) * angle, 0f);
            if (sweepAudio != null)
            {
                sweepAudio.pitch = Mathf.Lerp(1f, 1.6f, amount);
                sweepAudio.volume = Mathf.Lerp(0.35f, 0.7f, amount);
            }
        }
    }
}
