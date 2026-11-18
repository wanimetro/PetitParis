@echo off
chcp 65001 >nul
rem PETIT PARIS - Quest 2에서 게임 소리가 안 날 때 (2026-11-18 실기기에서 확인한 원인)
rem Horizon OS가 이 앱의 PLAY_AUDIO(오디오 재생) 권한을 ignore로 두면 Unity는 소리를 내도 스피커로 안 나간다
rem (adb shell dumpsys audio 에서 이 앱 AAudio 플레이어가 mutedState:appOps).
rem 권한을 allow로 바꾸고 게임을 다시 시작한다 (이미 열린 오디오 스트림은 다시 시작해야 풀린다).
setlocal
set ADB=adb
where adb >nul 2>nul || set "ADB=C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
set PKG=com.DefaultCompany.PetitParis

"%ADB%" shell appops set --uid %PKG% PLAY_AUDIO allow
"%ADB%" shell appops set %PKG% PLAY_AUDIO allow
"%ADB%" shell am force-stop %PKG%
"%ADB%" shell monkey -p %PKG% -c android.intent.category.LAUNCHER 1 >nul
echo.
echo PLAY_AUDIO:
"%ADB%" shell appops get --uid %PKG% PLAY_AUDIO
echo 헤드셋에서 PETIT PARIS가 다시 시작됩니다. 소리를 확인하세요.
endlocal
