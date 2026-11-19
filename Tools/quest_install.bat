@echo off
chcp 65001 >nul
rem PETIT PARIS - Quest 2/3 USB 설치 스크립트 (헤드셋 확인 -> 설치 -> 오디오 권한 -> 실행)
rem 사용법: 더블클릭, 또는  tools\quest_install.bat [apk 경로]
rem 기본 APK: 이 파일과 같은 폴더의 PetitParis_final.apk (릴리스에서 같이 받은 경우),
rem          없으면 저장소의 Builds\PetitParis_final.apk
rem 마지막에 창이 멈추지 않게 하려면  set NOPAUSE=1  후 실행.
setlocal EnableDelayedExpansion

set "PKG=com.DefaultCompany.PetitParis"
set "APK=%~1"
if "%APK%"=="" if exist "%~dp0PetitParis_final.apk" set "APK=%~dp0PetitParis_final.apk"
if "%APK%"=="" set "APK=%~dp0..\Builds\PetitParis_final.apk"

rem --- adb 찾기 (PATH -> Unity 6000.6.9f1 -> Android SDK) ---
set "ADB="
if exist "%~dp0adb.exe" set "ADB=%~dp0adb.exe"
if not defined ADB for /f "delims=" %%p in ('where adb 2^>nul') do if not defined ADB set "ADB=%%p"
if not defined ADB if exist "C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" set "ADB=C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
if not defined ADB if exist "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" set "ADB=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"
if not defined ADB (
  echo [실패] adb.exe를 찾지 못했습니다.
  echo   Android SDK Platform-Tools를 받아 압축을 풀고 adb.exe를 이 파일과 같은 폴더에 두거나 PATH에 추가하세요:
  echo   https://developer.android.com/tools/releases/platform-tools
  goto :fail
)
echo adb: %ADB%

if not exist "%APK%" (
  echo [실패] APK가 없습니다: %APK%
  goto :fail
)
echo APK: %APK%

rem --- 1. 헤드셋 확인 ---
"%ADB%" start-server >nul 2>nul
set "STATE="
set "SERIAL="
for /f "usebackq skip=1 tokens=1,2" %%a in (`"%ADB%" devices`) do (
  if not defined STATE ( set "SERIAL=%%a" & set "STATE=%%b" )
)
if not defined STATE (
  echo [실패] 연결된 헤드셋이 없습니다.
  echo   - USB-C 케이블이 데이터 전송용인지 확인하고 다시 꽂으세요.
  echo   - 헤드셋 설정 ^> 개발자 ^> USB 디버깅이 켜져 있어야 합니다 ^(Meta Horizon 앱에서 개발자 모드 활성화^).
  echo   - 헤드셋을 쓰고 "USB 디버깅 허용" 창이 뜨면 "이 컴퓨터에서 항상 허용" 체크 후 허용.
  goto :fail
)
if /i "%STATE%"=="unauthorized" (
  echo [실패] 헤드셋이 이 PC를 아직 허용하지 않았습니다 ^(unauthorized^).
  echo   헤드셋을 쓰고 "USB 디버깅 허용" 창에서 "이 컴퓨터에서 항상 허용" 체크 후 "허용"을 누르세요.
  echo   창이 안 보이면 케이블을 뺐다 다시 꽂고 이 스크립트를 다시 실행하세요.
  goto :fail
)
if /i not "%STATE%"=="device" (
  echo [실패] 헤드셋 상태: %STATE%  ^(케이블을 다시 꽂아 보세요^)
  goto :fail
)
echo [확인] 헤드셋 연결됨: %SERIAL%

rem --- 2. 설치 ---
echo 설치 중... ^(1분 정도 걸릴 수 있습니다^)
"%ADB%" -s %SERIAL% install -r "%APK%"
if errorlevel 1 (
  echo [실패] 설치 실패. 서명이 다른 이전 버전이 있으면 먼저 지우세요:
  echo   "%ADB%" uninstall %PKG%
  goto :fail
)

rem --- 3. 오디오 권한 (Horizon OS가 PLAY_AUDIO를 ignore로 두면 소리가 안 난다) ---
"%ADB%" -s %SERIAL% shell appops set --uid %PKG% PLAY_AUDIO allow
"%ADB%" -s %SERIAL% shell appops set %PKG% PLAY_AUDIO allow

rem --- 4. 실행 ---
"%ADB%" -s %SERIAL% shell am force-stop %PKG%
"%ADB%" -s %SERIAL% shell monkey -p %PKG% -c android.intent.category.LAUNCHER 1 >nul 2>nul
if errorlevel 1 (
  echo [실패] 실행 명령이 실패했습니다. 헤드셋 앱 목록 ^> 알 수 없는 출처에서 PetitParis를 직접 실행하세요.
  goto :fail
)
echo.
echo [완료] 헤드셋에서 PETIT PARIS가 실행되었습니다.
if not "%NOPAUSE%"=="1" pause
endlocal
exit /b 0

:fail
echo.
if not "%NOPAUSE%"=="1" pause
endlocal
exit /b 1
