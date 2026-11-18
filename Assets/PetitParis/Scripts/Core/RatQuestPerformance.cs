using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace PetitParis
{
    /// <summary>
    /// Quest(Android) 실기기에서만 GPU 부담을 줄인다. PC·에디터에는 영향 없음.
    /// - 카메라 URP 후처리(Tonemapping·Color Adjustments·Vignette Volume) 끔:
    ///   모바일 VR에서는 중간 렌더 텍스처 + 전체 화면 패스가 생겨 Quest 2에서 비용이 크다.
    /// - 고정 포비티드 렌더링(시야 가장자리 해상도 낮춤) 켬: OpenXR Foveated Rendering 기능(Android)이 켜져 있어야 동작.
    /// 2026-11-18 Quest 2 실기기 측정: 게임 중 GPU 16~18ms(45~50fps) → 머리를 돌리면 화면이 흔들림.
    /// </summary>
    public static class RatQuestPerformance
    {
        public const float FoveationLevel = 1f; // 0 = 끔, 1 = 최대

        static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyOnStart()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Apply();
            Apply();
        }
#endif

        public static void Apply()
        {
            int cameras = 0;
            foreach (var cam in Camera.allCameras)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null || !data.renderPostProcessing) continue;
                data.renderPostProcessing = false;
                cameras++;
            }

            int foveated = 0;
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
            {
                if (!d.running) continue;
                d.foveatedRenderingLevel = FoveationLevel;
                foveated++;
            }
            Debug.Log($"[PetitParis] Quest 성능 설정: 후처리 끔 {cameras}개 카메라, 포비티드 렌더링 {FoveationLevel:0.##} ({foveated}개 디스플레이)");
        }
    }
}
