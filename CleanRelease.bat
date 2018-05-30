@del Output\Launch.bat
@echo start Release\XbimXplorer.exe > Output\Launch.bat
@del Output\Release\*.pdb /s
@del Output\Release\*.xml
@del Output\Release\*.vshost.*
@del Output\Release\Plugins\*
@del %USERPROFILE%\Dropbox\Public\ReleaseV4.exe
@cd Output\
@"C:\Program Files\7-Zip\7z.exe" a -sfx7z.sfx %USERPROFILE%\Dropbox\Public\ReleaseV4.exe Release\ 
@"C:\Program Files\7-Zip\7z.exe" a -sfx7z.sfx %USERPROFILE%\Dropbox\Public\ReleaseV4.exe Launch.bat 
@cd ..\
@pause