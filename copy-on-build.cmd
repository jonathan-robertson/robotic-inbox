@REM Copy build files to destination based on build environment
@REM Expecting the following to be added to Project Settings -> Build Events:
@REM SET ConfigurationName=$(ConfigurationName)
@REM SET SolutionName=$(SolutionName)
@REM call copy-on-build.cmd

echo Starting Script: copy-on-build.cmd
echo ConfigurationName: %ConfigurationName%
echo SolutionName: %SolutionName%

if "%ConfigurationName%" == "Debug" (
    echo Debug Build
    if "" == "%SDTD_MODS_DEBUG_FOLDER%" (
        echo environment variable 'SDTD_MODS_DEBUG_FOLDER' is not set; please configure this in User Environment Variables and restart Visual Studio.
        exit 0
    )
    robocopy . %SDTD_MODS_DEBUG_FOLDER%\%SolutionName% ModInfo.xml *.md *.dll *.pbd /dcopy:dat
    robocopy .\Config %SDTD_MODS_DEBUG_FOLDER%\%SolutionName%\Config /s /dcopy:dat /PURGE
    exit 0
) else if "%ConfigurationName%" == "Release" (
    echo Release Build
    if "" == "%SDTD_MODS_RELEASE_FOLDER%" (
        echo environment variable 'SDTD_MODS_RELEASE_FOLDER' is not set; please configure this in User Environment Variables and restart Visual Studio.
        exit 0
    )
    robocopy . %SDTD_MODS_RELEASE_FOLDER%\%SolutionName% ModInfo.xml *.md *.dll /dcopy:dat
    robocopy .\Config %SDTD_MODS_RELEASE_FOLDER%\%SolutionName%\Config /s /dcopy:dat /PURGE
    exit 0
)
