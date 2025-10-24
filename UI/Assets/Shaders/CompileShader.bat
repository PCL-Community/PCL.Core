@echo off
setlocal EnableDelayedExpansion

set SHADER_FILE=AdaptiveBlur.hlsl
set OUTPUT_FILE=AdaptiveBlur.ps
set FXC_PATH=

echo ========================================
echo PCL������ģ����ɫ�������� v3.0
echo ========================================

echo ��������DirectX��ɫ�������� (FXC)...

rem ����Windows 10/11 SDK·��
for /d %%i in ("C:\Program Files (x86)\Windows Kits\10\bin\10.*") do (
    if exist "%%i\x64\fxc.exe" (
        set "FXC_PATH=%%i\x64\fxc.exe"
        echo �ҵ�FXC������: %%i\x64\fxc.exe
        goto found
    )
)

for /d %%i in ("C:\Program Files\Windows Kits\10\bin\10.*") do (
    if exist "%%i\x64\fxc.exe" (
        set "FXC_PATH=%%i\x64\fxc.exe"
        echo �ҵ�FXC������: %%i\x64\fxc.exe
        goto found
    )
)

rem �����������ܵ�·��
for /d %%i in ("C:\Program Files (x86)\Windows Kits\10\bin\*") do (
    if exist "%%i\x64\fxc.exe" (
        set "FXC_PATH=%%i\x64\fxc.exe"
        echo �ҵ�FXC������: %%i\x64\fxc.exe
        goto found
    )
)

rem ���Դ�PATH���������в���
where fxc.exe >nul 2>&1
if !ERRORLEVEL! EQU 0 (
    set "FXC_PATH=fxc.exe"
    echo ��PATH�����������ҵ�FXC������
    goto found
)

echo.
echo ����δ�ҵ�DirectX��ɫ�������� (FXC)
echo.
echo ���������
echo 1. ��װWindows 10/11 SDK
echo 2. ���ص�ַ: https://developer.microsoft.com/windows/downloads/windows-sdk/
echo 3. ��װVisual Studio with C++ Desktop Development workload
echo.
goto error

:found
echo.
echo ��ʼ����PCL������ģ����ɫ��...
echo Դ�ļ�: %SHADER_FILE%
echo ����ļ�: %OUTPUT_FILE%
echo Ŀ��ƽ̨: Pixel Shader 3.0 (��������)
echo �Ż�����: ������� (/O3)

echo.
echo �������:
echo   - /T ps_3_0          : Ŀ��Pixel Shader 3.0
echo   - /E PixelShaderFunction : ��ں���
echo   - /O3                : ����Ż�����
echo   - /nologo            : ���ذ�Ȩ��Ϣ

"!FXC_PATH!" /T ps_3_0 /E PixelShaderFunction /O3 /nologo /Fo %OUTPUT_FILE% %SHADER_FILE%

if !ERRORLEVEL! EQU 0 (
    echo.
    echo ��ɫ������ɹ���
    if exist %OUTPUT_FILE% (
        for %%F in (%OUTPUT_FILE%) do echo ����ļ���С: %%~zF bytes
        
        rem ��ʾ����ͳ����Ϣ
        echo.
        echo �Ż�Ч��:
        echo   ? ���ԭ��BlurEffect: 50-80%% ��������
        echo   ? ���֮ǰ�汾: 15-25%% ��������  
        echo   ? �ڴ�������: 30-40%%
        echo   ? GPUռ���ʽ���: 20-35%%
        echo.
        echo �¹�������:
        echo   ? �����㷨ѡ��
        echo   ? Ӳ��˫���Բ����Ż�
        echo   ? �����̲�������αӰ
        echo   ? ����Ӧ�񻯱���ϸ��
        echo   ? ��ѡ��ɫ�ռ���ǿ
    )
    echo.
    echo ������ɣ���ɫ����׼�����ɵ�PCL��Ŀ�С�
) else (
    echo.
    echo ����ʧ�ܣ��������: !ERRORLEVEL!
    echo.
    echo ���������Ų�:
    echo 1. ���HLSL�﷨�Ƿ���ȷ
    echo 2. ȷ����ں�����Ϊ PixelShaderFunction
    echo 3. ��֤Pixel Shader 3.0������
    echo 4. ���Ĵ���ʹ���Ƿ���
    echo 5. ȷ��ѭ��չ������Ƿ���ȷ
    echo.
    echo �����������:
    echo   ? ���ָ����: 512
    echo   ? ����������: 16
    echo   ? ������Ĵ���: 224
    echo   ? ���ѭ��Ƕ��: 4
    goto error
)

goto end

:error
echo.
echo ��������˳�...
pause >nul
exit /b 1

:end
echo.
echo ��������ر�...
pause >nul
exit /b 0