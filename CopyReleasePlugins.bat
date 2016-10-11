@md Output\Release\Plugins\ >nul 2>nul
@md Output\Release\Plugins\XplorerPlugins.Bcf >nul 2>nul
@echo BCF Files
copy XplorerPlugins.Bcf\bin\Release\XplorerPlugins.Bcf.exe Output\Release\Plugins\XplorerPlugins.Bcf
copy XplorerPlugins.Bcf\bin\Release\Ionic.Zip.dll Output\Release\Plugins\XplorerPlugins.Bcf
@md Output\Release\Plugins\XplorerPlugins.Cobie >nul 2>nul
@echo Cobie Files
copy XplorerPlugins.Cobie\bin\Release\XplorerPlugins.Cobie.exe Output\Release\Plugins\XplorerPlugins.Cobie
copy XplorerPlugins.Cobie\bin\Release\Xbim.Cobie.dll Output\Release\Plugins\XplorerPlugins.Cobie
copy XplorerPlugins.Cobie\bin\Release\Xbim.Exchanger.dll Output\Release\Plugins\XplorerPlugins.Cobie
copy XplorerPlugins.Cobie\bin\Release\Xbim.COBieLite.dll Output\Release\Plugins\XplorerPlugins.Cobie
copy XplorerPlugins.Cobie\bin\Release\Xbim.DPoW.dll Output\Release\Plugins\XplorerPlugins.Cobie
copy XplorerPlugins.Cobie\bin\Release\Xbim.COBieLiteUK.dll Output\Release\Plugins\XplorerPlugins.Cobie
copy XplorerPlugins.Cobie\bin\Release\Xbim.Properties.dll Output\Release\Plugins\XplorerPlugins.Cobie
@pause