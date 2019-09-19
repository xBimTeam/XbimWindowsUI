@set "version=4.0.13-V013"
@echo Ensure an updated version of the squirrel repo in Squirrel.Windows
@rmdir OutPut\Release /s /q
@echo Build the solution in release mode now (it's just been deleted).
@echo. 
@pause
@"..\..\nuget.exe" pack "Xbim.Xplorer.squirrel.nuspec" -Version %version%
@echo Releasifying
@"Packages\squirrel.windows.1.5.3-cb003\tools\Squirrel.exe" --releasify Xbim.Xplorer.%version%.nupkg --releaseDir=..\Squirrel.Windows\XplorerReleases --no-msi
@del Xbim.Xplorer.%version%.nupkg
@pause