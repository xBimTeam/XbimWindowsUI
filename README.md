
Branch | Status
------ | -------
Master | [ ![Build Status](http://xbimbuilds.cloudapp.net/app/rest/builds/buildType:(id:Xbim_XbimWindowsUi_XbimWindowsUi),branch:(name:master)/statusIcon "Build Status") ](http://xbimbuilds.cloudapp.net/project.html?projectId=Xbim_XbimWindowsUi&tab=projectOverview "Build Status")
Develop | [ ![Build Status](http://xbimbuilds.cloudapp.net/app/rest/builds/buildType:(id:Xbim_XbimWindowsUi_XbimWindowsUi),branch:(name:develop)/statusIcon "Build Status") ](http://xbimbuilds.cloudapp.net/project.html?projectId=Xbim_XbimWindowsUi&tab=projectOverview "Build Status")

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
