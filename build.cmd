dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true
"%WIX%bin\candle.exe" Installer\*.wxs -out Installer\Installer.wix.obj -dTargetDir="AutoPowerManagementForShelfDevices\bin\Release\net5.0\win-x64\publish"
md Installer\bin
"%WIX%bin\light.exe" -b "Installer\bin" -out "Installer\bin\AutoPowerManagementForShelfDevice.msi" Installer\Installer.wix.obj