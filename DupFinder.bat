:: this batch will present a report of duplicate code in the solution to help cleaning it out
:: In order to run it download free dupcode tool from Resharpert CLT 
:: at https://www.jetbrains.com/resharper/download/index.html#section=resharper-clt
@echo off
"C:\Program Files\ResharperCLT2017.1\dupfinder.exe" --discard-literals=true --caches-home="%temp%\DFCache" --o="DuplicateCodeReport.xml" --discard-cost=100 "Xbim.WindowsUI.Nuget.sln" --show-text
start DuplicateCodeReport.xml
