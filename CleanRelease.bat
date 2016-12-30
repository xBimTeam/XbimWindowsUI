@del Output\Release\*.pdb /s
@del Output\Release\*.xml
@del Output\Release\*.vshost.exe
@del Output\Release\*.vshost.application
"C:\Program Files\7-Zip\7z.exe" a -sfx7z.sfx C:\Users\Claudio\Dropbox\Public\ReleaseV4.exe Output\Release\ 
pause