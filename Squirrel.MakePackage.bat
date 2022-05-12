@set "version=5.1.229"
@echo Before continuing:
@echo a) ensure there's a complete set of the squirrel setup files in XplorerReleases
@echo b) ensure the version number set in this batch is updated (matching current nuget version could be an option)
@rmdir OutPut\Release /s /q
@echo c) Build the solution in release mode now (it's just been deleted).
@echo. 
@pause
@"..\..\nuget.exe" pack "Xbim.Xplorer.squirrel.nuspec" -Version %version%
@echo Releasifying
@"%userprofile%\.nuget\packages\squirrel.windows\1.9.2-cb003\tools\Squirrel.exe" --releasify Xbim.Xplorer.%version%.nupkg --releaseDir=..\XplorerReleases --no-msi --setupIcon=XbimXplorer\xBIM.ico --framework-version=net472
@del Xbim.Xplorer.%version%.nupkg
@pause