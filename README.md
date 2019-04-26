
Branch | Build Status  | MyGet | NuGet
------ | ------- | --- | --- |
Master | [![Build Status](https://dev.azure.com/xBIMTeam/xBIMToolkit/_apis/build/status/XbimWindowsUI?branchName=master)](https://dev.azure.com/xBIMTeam/xBIMToolkit/_build/latest?definitionId=4?branchName=master) | ![master](https://img.shields.io/myget/xbim-master/v/Xbim.WindowsUI.svg) | ![](https://img.shields.io/nuget/v/Xbim.WindowsUI.svg)
Develop |[![Build Status](https://dev.azure.com/xBIMTeam/xBIMToolkit/_apis/build/status/XbimWindowsUI?branchName=develop)](https://dev.azure.com/xBIMTeam/xBIMToolkit/_build/latest?definitionId=4?branchName=develop) | ![](https://img.shields.io/myget/xbim-develop/vpre/Xbim.WindowsUI.svg) | -

# XbimWindowsUI

XbimWindowsUI is part of the [Xbim Toolkit](https://github.com/xBimTeam/XbimEssentials).
It contains libraries and applications that you can use to build applications on Windows desktops. 

This is the home of the XbimXplorer application [XBIM Xplorer](http://docs.xbim.net/downloads/xbimxplorer.html)
which you can use for reference to see how the XBIM technology can be used in a Windows desktop environment.

![XbimXplorer UI](ReadmeResources/XbimXplorerUI.png)

This repo also contains the source for *Xbim.Presentation*, which is re-usable set of WPF and Windows Forms components 
that make up XbimXplorer. You can include this package in your own applications, to make use of the XBIM toolkit.

## Compilation

**Visual Studio 2017 is recommended.**
Prior versions of Visual Studio may work, but we'd recomments 2017 where possible.
The [free VS 2017 Community Edition](https://visualstudio.microsoft.com/downloads/) will be fine. 
All projects target .NET Framework 4.7

The XBIM toolkit uses the NuGet technology for the management of several packages.
We have custom NuGet feeds for the *master* and *develop* branches of the solution, and use
Myget for CI builds. The [nuget.config](nuget.config) file should automatically add these feeds for you


## Acknowledgements
The XbimTeam wishes to thank [JetBrains](https://www.jetbrains.com/) for supporting the XbimToolkit project 
with free open source [Resharper](https://www.jetbrains.com/resharper/) licenses.

![ReSharper Logo](ReadmeResources/icon_ReSharper.png)
