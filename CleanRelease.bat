@del Output\Launch.bat
@echo start Release\XbimXplorer.exe > Output\Launch.bat
@del Output\Release\*.pdb /s
@del Output\Release\*.xml
@del Output\Release\*.vshost.*
@rmdir Output\Release\Plugins /s /q
@echo Press a key to copy to dropbox or ctrl-c to prevent
@pause 
@del %USERPROFILE%\Dropbox\Public\ReleaseV4.exe
@cd Output\
@"C:\Program Files\7-Zip\7z.exe" a -sfx7z.sfx %USERPROFILE%\Dropbox\Public\ReleaseV4.exe Release\ 
@cd ..\
@pause