@set "version=5.1.0"
@echo Ensure an updated version of the squirrel repo in XplorerReleases
@rmdir OutPut\Release /s /q
@echo Build the solution in release mode now (it's just been deleted).
@echo. 
@pause
@"..\..\nuget.exe" pack "Xbim.Xplorer.squirrel.nuspec" -Version %version%
@echo Releasifying
@"%userprofile%\.nuget\packages\squirrel.windows\1.9.2-cb001\tools\Squirrel.exe" --releasify Xbim.Xplorer.%version%.nupkg --releaseDir=..\XplorerReleases --no-msi
@del Xbim.Xplorer.%version%.nupkg
@pause