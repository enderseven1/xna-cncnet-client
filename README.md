# CnCNet Client - CnCNet客户端

This version is compatible with [Starkku's mod base](https://github.com/Starkku/cncnet-client-mod-base), and adds an age verification system and the ability to display "healthy game advice".  
该版本兼容[Starkku的mod base](https://github.com/Starkku/cncnet-client-mod-base)，并且添加了中国特色功能：防沉迷、脏话屏蔽、健康游戏忠告。

The age verification system and healthy game advice are enabled by default in the People's Republic of China, and there will be no options for healthy game advice in non-People's Republic of China regions.  
防沉迷、健康游戏忠告和脏话屏蔽在中国区内自动开启，非中国区则不会显示也不会有显示健康游戏忠告的选项。

The flag `AgeVerify=` in `ClientDefinitions.ini` controls showing age verification.
`ClientDefinitions.ini`里的`AgeVerify=`语句决定是否启用年龄验证。

The flag `CrabsInRivers=` in `ClientDefinitions.ini` controls showing age verification.
`ClientDefinitions.ini`里的`CrabsInRivers=`语句决定是否启用脏话屏蔽。

合并了一部分pr。

The MonoGame / XNA CnCNet client, a platform for playing classic Command & Conquer games and their mods both online and offline. Supports setting up and launching both singleplayer and multiplayer games with [a CnCNet game spawner](https://github.com/CnCNet/ts-patches). Includes an IRC-based chat client with advanced features like private messaging, a friend list, a configurable game lobby, flexible and moddable UI graphics, and extras like game setting configuration and keeping track of match statistics. And much more!  
MonoGame/XNA CnCNet客户端是游玩经典命令与征服游戏及其MOD的平台，支持使用 [CnCNet游戏生成器](https://github.com/CnCNet/ts-patches)设置和启动单人游戏和多人游戏。包括基于IRC的聊天客户端，具有高级功能，如私信、好友列表、可配置的游戏大厅、灵活可修改的 UI 图形，以及游戏设置配置和跟踪比赛统计数据等附加功能。还有更多！

You can find the [dedicated project development chat](https://discord.gg/M5gGdBYG5m) at C&C Mod Haven Discord server.  
您可以在C&C Mod Haven Discord服务器上找到[专用项目开发聊天](https://discord.gg/M5gGdBYG5m)。

## Targets 目标

The primary targets of the client project are  
该项目主要服务于：
* [Dawn of the Tiberium Age 泰伯利亚时代黎明](https://www.moddb.com/mods/the-dawn-of-the-tiberium-age)
* [Twisted Insurrection 扭曲的暴动](https://www.moddb.com/mods/twisted-insurrection)
* [Mental Omega 心灵终结](https://www.moddb.com/mods/mental-omega)
* [CnCNet Yuri's Revenge 尤里的复仇](https://cncnet.org/yuris-revenge)

However, there is no limitation in the client that would prevent incorporating it into other projects. Any game or mod project that utilizes the CnCNet spawner for Tiberian Sun and Red Alert 2 can be supported. Several other projects also use the client or an unofficial fork of it, including [Tiberian Sun Client](https://www.moddb.com/mods/tiberian-sun-client), [Project Phantom](https://www.moddb.com/mods/project-phantom), [YR Red-Resurrection](https://www.moddb.com/mods/yr-red-resurrection), [The Second Tiberium War](https://www.moddb.com/mods/the-second-tiberium-war) and [CnC: Final War](https://www.moddb.com/mods/cncfinalwar).  
其他项目也可以使用本客户端，也支持任何使用该客户端的游戏或mod。其他几个项目也使用客户端或其第三方分支，包括[泰伯利亚之日](https://www.moddb.com/mods/tiberian-sun-client), [幽灵计划](https://www.moddb.com/mods/project-phantom), [红色复活](https://www.moddb.com/mods/yr-red-resurrection), [第二次泰伯利亚战争](https://www.moddb.com/mods/the-second-tiberium-war) and [CnC: Final War](https://www.moddb.com/mods/cncfinalwar).  

## Development requirements 开发要求

The client has 2 variants: .NET 4.8 and .NET 8.0.  
客户端分为.NET4.8版本和.NET8.0版本。
* Both variants have 3 builds: Windows DirectX11, Windows OpenGL and Windows XNA.
* 两种变体都有3个版本：Windows DirectX11、Windows OpenGL 和 Windows XNA。
* .NET 8.0 in addition has a cross-platform Universal OpenGL build.
* 此外，.NET8.0还具有跨平台的通用 OpenGL 版本。
* The DirectX11 and OpenGL builds rely on MonoGame.
* DirectX11和OpenGL版本依赖MonoGame。
* The XNA build relies on Microsoft's XNA Framework 4.0 Refresh.
* XNA版本依赖Microsoft的XNA Framework 4.0 Refresh。

Building the solution for **any** platform requires Visual Studio 2022 17.8 or newer and/or the .NET SDK 8.0.200. A modern version of Visual Studio Code, MonoDevelop or Visual Studio for Mac could also work, but are not officially supported.
To debug WindowsXNA builds the .NET SDK 8.0 x86 is additionally required.
When using the included build scripts PowerShell 7.2 or newer is required.
编译**任何**平台的版本都要Visual Studio 2022 17.8及以上版本或.NET SDK 8.0.200。Visual Studio Code、MonoDevelop或Visual Studio for Mac的现代版本也可以使用，但不受官方支持。
若要调试WindowsXNA生成，还需要.NET SDK 8.0 x86。
使用包含的生成脚本时，需要PowerShell 7.2及以上版本。
[^install-powershell]

## Compiling and debugging 编译调试

* 懒得翻译了，直接点`Scripts\Build**.bat (develop)` 或 `BuildScripts\Build**.bat (master/modified-client)`。
* Compiling itself is simple: assuming you have the .NET 8.0 SDK installed, you can just open the solution with Visual Studio and compile it right away.
* When built as a debug build, the client executable expects to reside in the same directory with the target project's main game executable. Resources should exist in a "Resources" sub-directory in the same directory. The repository contains sample resources and post-build commands for copying them so that you can immediately run the client in debug mode by just hitting the Debug button in Visual Studio.
* When built in release mode, the client executables expect to reside in the `Resources` sub-directory itself for .NET 4.8, named `clientdx.exe`, `clientogl.exe` and `clientxna.exe`. Each `.exe` file or `.dll` file expects a `.pdb` file for diagnostics purpose. It's advised not to delete these `.pdb` files. Keep all `.pdb` files even for end users.
* The `Scripts` directory has automated build scripts that build the client for all platforms and copy the output files to a folder named `Compiled` in the project root. You can then copy the contents of this `Compiled` directory into the `Resources` sub-directory of any target project.

<details>
  <summary>.NET 8 builds</summary>

* For .NET 8, When built in release mode, the client executables expect to reside in `Resources/BinariesNET8/{Windows, OpenGL, UniversalGL, XNA}` folders, named `client{dx, ogl, ogl, xna}.dll`, respectively. Note that `client{dx, ogl, ogl, xna}.runtimeconfig.json` files are required for the corresponding .NET 8 dlls.
* When built on an OS other than Windows, only the Universal OpenGL build is available.
</details>

<details>
  <summary>Development workarounds</summary>

* If you switch among different solution configurations in Visual Studio (e.g. switch to `TSUniversalGLRelease` from `AresWindowsDXDebug`), especially switching between .NET 4.8 and .NET 8.0 variants, it is recommended to restart Visual Studio after switching configurations to prevent unexpected error messages. If restarting Visual Studio do not work as intended, try deleting all `obj` folders in each project. Due to the same reason, it is highly advised to close Visual Studio when building the client using the scripts in `Scripts` folder.
* Some dependencies are stored in `References` folder instead of the official NuGet source. This folder is also useful if you are working on modifying a dependency and debugging in your local machine without publishing the modification to NuGet. However, if you have replaced the `.(s)nupkg` files of a package, without altering the package version, be sure to remove the corresponding package from `%USERPROFILE%\.nuget\packages` folder (Windows) to purge the old version. 
</details>

## End-user usage

* Windows: Windows 7 SP1 or higher is required. The preferred build is DirectX11 (.NET 4.8), i.e., `clientdx.exe`. If your GPU does not support DX11, consider using the OpenGL or XNA build instead. Advanced users may experiment with the .NET 8 builds at their discretion.
* Other OS: Use the Universal OpenGL build.

## End-user requirements

### Windows .NET 4.8 requirements:

* The [.NET Framework 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer)

(Optional) The XNA build requires:
* [Microsoft XNA Framework Redistributable 4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598).

### Linux requirements:

* The [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=linux) for your specific platform.

### macOS requirements:

* The [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=macos) for your specific platform.

### Windows .NET 8.0 requirements:

<details>
  <summary>Windows .NET 8.0 requirements</summary>

* The [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=windows) for your specific platform.

(Optional) The XNA build requires:
* [Microsoft XNA Framework Redistributable 4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598).
* [.NET 8.0 Desktop Runtime x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.0-windows-x86-installer).

Windows 7 SP1 and Windows 8.x additionally require:
* Microsoft Visual C++ 2015-2019 Redistributable [64-bit](https://aka.ms/vs/16/release/vc_redist.x64.exe) / [32-bit](https://aka.ms/vs/16/release/vc_redist.x86.exe).

Windows 7 SP1 additionally requires:
* KB3063858 [64-bit](https://www.microsoft.com/download/details.aspx?id=47442) / [32-bit](https://www.microsoft.com/download/details.aspx?id=47409).
</details>

## Client launcher 启动器

This repository does not contain the client launcher (for example, `DTA.exe` in Dawn of the Tiberium Age) that selects which platform's client executable is most suitable for each user's system.
See [dta-mg-client-launcher](https://github.com/CnCNet/dta-mg-client-launcher).  
此仓库不包含启动器（例如泰伯利亚时代黎明中的`DTA.exe`），该启动器负责选择合适的客户端。
请参阅 [dta-mg-client-launcher](https://github.com/CnCNet/dta-mg-client-launcher)。

## Branches 分支

Currently there are only two major active branches. `develop` is where development happens, and while things should be fairly stable, occasionally there can also be bugs. If you want stability and reliability, the `master` branch is recommended.  
目前官方只有两个主要的活跃分支，`develop`是开发区，较为稳定，但会偶尔出错。建议使用更稳定的`master`分支。本仓库的`master`分支为`modified-client`。

## Screenshots 截屏

![Screenshot](cncnetchatlobby.png?raw=true "CnCNet IRC Chat Lobby 聊天大厅")
![Screenshot](cncnetgamelobby.png?raw=true "CnCNet Game Lobby 游戏大厅")


[^install-powershell]: [How To Install PowerShell Core 如何安装PowerShell Core](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows)
