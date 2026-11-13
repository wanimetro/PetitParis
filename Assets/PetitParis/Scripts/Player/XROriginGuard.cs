using Unity.XR.CoreUtils;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 씬에 XR Origin이 두 개 이상 생기는 것을 막는다.
    /// (샘플 프리팹을 실수로 끌어다 놓으면 카메라·입력·이동 속도가 중복된다)
    /// 다른 모든 스크립트보다 먼저 실행된다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(XROrigin))]
    public class XROriginGuard : MonoBehaviour
    {
        void Awake()
        {
            var mine = GetComponent<XROrigin>();
            var origins = FindObjectsByType<XROrigin>(FindObjectsSortMode.None);
            foreach (var origin in origins)
            {
                if (origin == mine || !origin.gameObject.activeInHierarchy) continue;
                Debug.LogWarning($"[PetitParis] 씬에 XR Origin이 두 개 이상입니다. '{origin.name}'을(를) 비활성화합니다. " +
                                 "PETIT PARIS는 'XR Origin (Rat)' 하나만 사용합니다.", origin);
                origin.gameObject.SetActive(false);
            }

            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int active = 0;
            foreach (var listener in listeners)
                if (listener.isActiveAndEnabled) active++;
            if (active > 1)
                Debug.LogWarning($"[PetitParis] 활성 AudioListener가 {active}개입니다. Main Camera 하나에만 두세요.", this);
        }
    }
}
