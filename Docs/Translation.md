# Translation 翻译

The client has a built-in support for translations. The translation system is made to allow non-programmers to easily translate mods and games based on XNA CnCNet client to the languages of their choice.
客户端支持翻译功能，以便用户翻译。

The translation system supports the following:
翻译系统支持以下功能：
- translating client's built-in text strings;
- 翻译内置文本；
- translating INI-defined text values without modifying the respective INI files themselves;
- 不修改INI来翻译INI的文本；
- adjusting INI-defined size and position values for client controls per translation;
- 根据翻译结果调整控件大小和位置；
- providing custom client asset overrides (including both generic and theme-specific) in translations (for instance, translated buttons with text on them, or fonts for different CJK variatons);
- 覆盖客户端资源，如添加字库；
- auto-detecting the initial language of the client based on the system's language settings (if provided; happens on first start of the client);
- 首次启动时根据系统语言分配已有语言包；
- configurable set of files to copy to the game directory (for ingame translations);
- 可配置文件复制到游戏目录（用于游戏内翻译）；
- an ability to generate a translation template/stub file for easy translation.
- 生成模版以供翻译。

## Translation structure 翻译结构

The translation system reads folders from the `Resources/Translations` directory by default. Each folder found in that directory is considered a translation and can contain the main translation INI (contains some translation metadata and the translated values), generic assets (they take priority over what's found in `Resources` folder under the same relative path), theme-specific translation INIs and theme-specific assets (overrides for `Resources/[theme name]`) placed in the folders with the same names as the main theme folders that they are supposed to override.  
翻译系统默认读取`Resources/Translations`中的文件夹，其中可包含翻译INI文件（元数据和值）、资源（覆盖`Resources`文件夹中的内容）、同名主题特定翻译条目和资源（覆盖`Resources/[theme name]`）。

> **Note 注意**
> Add the `UseMinecraftTranlationFormat=true` to the `[Settings]` section in the `ClientDefinitions.ini` in [Enderseven1's fork](https://github.com/enderseven1/xna-cncnet-client), It will mimic the `lang\language_code.json` format in Minecraft, and read `Resourses\Translations\language_code.ini` instead of `Resourses\Translations\language_code\Translation.ini`.
> 在[Enderseven1的版本](https://github.com/enderseven1/xna-cncnet-client)中填写`ClientDefinitions.ini` -> `[Settings]` -> `UseMinecraftTranlationFormat=true`语句，将模仿Minecraft中的`\lang\语言代码.json`格式，读取`Resourses\Translations\语言代码.ini`而非`Resourses\Translations\语言代码\Translation.ini`。

For example:  
例如：

```md
- Resources
  - Some Theme Folder 主题文件夹
    * someThemeAsset.png 图片资源
    * ...
  - Translations
    - ru 地区代码
      - Some Theme Folder 主题文件夹
	* Translation.ini 主题翻译INI
        * someThemeAsset.png 主题图片资源
        * ...
      * Translation.ini 翻译INI
      * someAsset.png 图片资源
      * ...
    - uk 地区代码
      * ...
    - zh-Hans
      * ...
    - zh-Hant
      * ...
  * someAsset.png 全局图片资源
  * ...
```

### Folder naming and automatic language detection 文件夹命名和自动语言检测

The translation folder name is used to match it to the system locale code (as defined by BCP-47), so it is advised to name the translation folders according to that (for example, see how [the locales Windows uses](https://learn.microsoft.com/ru-ru/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c) are coded). That allows the client to choose the appropriate translation based on the system locale and also automatically fetch the name of the translation.  
翻译文件夹以BCP-47的地区代码命名，如[Windows地区编码](https://learn.microsoft.com/ru-ru/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c)，客户端以此选择语言并获取语言名称。

> **Note 注意**
> > Unless you're aiming for making a translation for a specific country (e.g. `en-US` and `en-GB`), it's advised to use simply a [language code](http://www.loc.gov/standards/iso639-2/php/code_list.php) (for example, `ru`, `de`, `en`, `zh-Hans`, `zh-Hant` etc.)
> 非特定地区翻译（例如`en-US`和`en-GB`）时仅需用[简易代码](http://www.loc.gov/standards/iso639-2/php/code_list.php)，如`ru`、`de`、`en`、`zh-Hans`、`zh-Hant`等）。

The folder name doesn't explicitly need to match the existing locale code. However, in that case you would want to provide an explicit name in the translation INI, and the translation won't be automatically picked in any case.  
文件夹名称可以不是地区代码，但是你需要在INI中提供语言名称，且客户端不会自动分配此语言。

> **Note 注意**
> The hardcoded client strings can be overridden using an `en` translation. Because the built-in `en` strings are always available, so it English client language. Even if the client doesn't have any translations, English will still be picked by default. If for some reason you need to override hardcoded strings in your client distribution, you can create a `Resources/Translations/en/Translation.ini` file and override needed values there.  
> 客户端默认使用`en`翻译，因此可以在`Resources/Translations/en/Translation.ini`覆盖源码中的文本。

### Translation INI format INI格式

```ini
[General]            ; translation metadata 元数据
Name=Some Language   ; string, used instead of a system-provided name if set 语言名称
Author=Someone       ; string 作者

[Values]             ; the key-values for translation 翻译键值
Some:Key=Some Value  ; string, see below for explanation 参见下文
```

#### Translation values key format 翻译键值格式

Examples:  
如：
```ini
INI:Missions:GDIFS:Description=Act 1: GDI Campaign - Desperate Measures
INI:Controls:GameOptionsPanel:chkBlackChatBackground:Text=Dark Chat Background
Client:DTAConfig:FriendsOnly=Only receive game invitations from friends
```

Each key in the `[Values]` section is composed of a few elements, joined using `:`, that have different semantic meaning. The structure can be described like this (with list level denoting the position).  
几个意义不同的元素由`;`连接组成`[Values]`中的每个键，结构如下：
- `Client` - the client's built-in text strings.   
客户端内置文本
  - The 2nd and 3rd parts usually denote the string's "namespace" or category and the string's name, respectively, and are chosen arbitrarily by the developers.  
  第2部分和第3部分通常分别表示字符串的“命名空间”或类别以及字符串的名称，由开发者决定。
- `INI` - the INI-defined values.   
INI定义的值
  - `Controls` - denotes all INI-defined control values.   
  游戏选项名称
    - `[parent control name]` - the name of the parent control of the control that the value is defined for. Specifying `Global` instead of the parent name allows to specify identical translated value for all instances of the control regardless of the parent (parent-specific definition overrides this still though)  
    父控件注册名，值为`Global`的允许为所有没指明父控件注册名的实例指定通用的值
      - `[control name]` - the name of the control that the value is defined for.  
      控件注册名
	      - `[attribute name]` - the name of the attribute that is being translated. Currently supported:   
        INI里的属性名
          - `Text`, `Size`, `Width`, `Height`, `Location`, `X`, `Y`, `DistanceFromRightBorder`, `DistanceFromBottomBorder` for every control;   
          每个控件支持以上属性，以下属性必须在配置INI里已定义；
          - `ToolTip` for controls with tooltip;   
          选项的提示
          - `Suggestion` for suggestion text boxes;  
          空输入框的提示
          - `ItemX` (where X) for setting/game options dropdowns;  
          下拉选项的项目
          - `OptionName` for game option dropdowns;  
          下拉选项名
          - `$X`, `$Y`, `$Width`, `$Height` for INItializable window system.  
          可配置的窗口
  - `Sides` - subcategory for the game's/mod's side names.   
  国家或阵营
  - `Colors` - subcategory for the game's/mod's color names.   
  玩家颜色
  - `Themes` - subcategory for the game's/mod's theme names.   
  游戏主题
  - `GameModes` - subcategory for the game's/mod's game modes.   
  游戏模式
    - `[name]` - uniquely identifies the game mode.   
    模式注册名
      - `[attribute name]` - the name of the attribute that is being translated. Only `UIName` is supported.  
      模式的`UIName`（名称）
  - `Maps` - subcategory for the game's/mod's maps (custom maps are not supported).  
  内置地图
    - `[map path]` - uniquely identifies the map.  
    地图路径
      - `[attribute name]` - the name of the attribute that is being translated. Only `Description` (map name) and `Briefing` are supported. 
      地图的`Description`（名称）`Briefing`（简报）
  - `Missions` - subcategory for the game's/mod's singleplayer missions.  
  单人战役
    - `[mission section name]` - uniquely identifies the map (taken from `Battle*.ini`).  
    `Battle*.ini`里的任务注册名
      - `[attribute name]` - the name of the attribute that is being translated. Only `Description` (mission name) and `LongDescription` (actual description) are supported.  
      战役的`Description`（名称）和`LongDescription`（介绍）
  - `CustomComponents` - subcategory for the game's/mod's custom components.  
  自定义组件
    - `[custom component INI name]` - uniquely identifies the custom component.  
  组件注册名
      - `[attribute name]` - the name of the attribute that is being translated. Only `UIName` is supported.   
      组件的`UIName`（名称）
  - `UpdateMirrors` - subcategory for the game's/mod's update download mirrors.  
  更新服务器
    - `[mirror name]` - uniquely identifies the mirror.  
    服务器名
      - `[attribute name]` - the name of the attribute that is being translated. Only `Name` and `Location` are supported.  
      服务器的`Name`（名称）和`Location`（地区）
  - `Hotkeys` - subcategory for the game's/mod's hotkeys. 
  快捷键
    - `[INI name]` - uniquely identifies the hotkey.  
    快捷键注册名
      - `[attribute name]` - the name of the attribute that is being translated. Only `UIName` and `Description` are supported.  
      快捷键的`UIName`（名称）和`Description`（介绍）
  - `HotkeyCategories` - subcategory for the game's/mod's hotkey categories.  
  快捷键类别
  - `ClientDefinitions` - self explanatory.  
  顾名思义，`ClientDefinitions.ini`里的
    - `WindowTitle` - self explanatory, only works if set in `ClientDefinitions.ini`  
    顾名思义，窗口标题

> **Warning 警告**
> You can only translate an INI value if it was used in the INI in the first place! That means that defining a translated value for a control's attribute (example: translating `X` and `Y` when `Location` is defined) that is not present in the INI **will not have any effect**.  
> 你只能翻译INI里定义的属性，没定义（例如只定义了`Location`却翻译`X`和`Y`）翻译了也**没用**！

## Ingame translation setup 游戏内翻译

The translation system's ingame translation support requires the mod/game author(s) to specify the files which translators can provide in order to translate the game. The files are specified in the the syntax is `GameFileX=path/to/source.file,path/to/destination.file[,checked]` INI key in the `[Translations]` section of `ClientDefinitions.ini` (X is any text you want to add to the key to help sort files), with comma-separated parts of the value meaning the following:  
游戏内翻译需要作者指定文件以便翻译游戏，这些文件在`ClientDefinitions.ini`的`[Translations]`里定义，语法为`GameFileX=path/to/source.file, path/to/destination.file[, checked]`（X为序号，不要求是数字），可用参数为：
1) the path to the source file relative to currently selected translation directory;  
源文件在翻译文件夹中的相对路径；
2) the destination to copy to, relative to the game root folder;  
目标文件到游戏根目录的相对路径；
3) (optional) `checked` for the file to be checked by file integrity checks (should be on if this file can be used to cheat), if not specified - this file is not checked.  
（可选）检查文件完整性，如果为ini或mix等可以影响游戏设定的应当设为true，默认否

> **Warning 警告**
> If you include checked files in your ingame translation files, that means users won't be able to do custom translations if they include those files and you won't be able to use custom components with those files **without triggering the modified files / cheater warning**. This mechanism is made for those games and mods where it's impossible to provide a mechanism to provide translations in a cheat-safe way, so please use it only if you have no other choice, otherwise don't specify this parameter.  
> 开启参数后用户将无法自定义该文件的内容还能不弹出作弊提示，只要修改了不影响游戏就别开。

Example configuration in `ClientDefinitions.ini`:  
举例：
```ini
[Translations]
GameFileTranslationMix=translation.mix,expandmo98.mix
GameFile_GDI01=Missions/g0.map,Maps/Missions/g0.map
GameFile_NOD01=Missions/n0.map,Maps/Missions/n0.map
GameFile_DLL_SD=Resources/language_800x600.dll, Resources/language_800x600.dll
GameFile_DLL_HD=Resources/language_1024x720.dll,Resources/language_1024x720.dll
```

This will make the `translation.mix` file from current translation folder (say, `Resources/Translations/ru`) copied to game root as `expandmo98.mix` on game start.  
这样`Resources/Translations/ru`中的`translation.mix`就会被复制到根目录的`expandmo98.mix`。

> **Warning 警告**
> This feature is needed only for *game* files, not *client* files like INIs, theme assets etc.!
> 此功能用于游戏文件而非客户端文件！

## Suggested translation workflow 建议翻译流程

0. In the mod's settings INI file (for example: `SUN.INI`, `RA2MD.INI`) append `GenerateTranslationStub=true` in `[Options]` section. This will make the client generate a `Translation.ini` file in `Client` folder with all (almost; read caveat below) translatable text values, sorted alphabetically by key. Values with no translations will be commented out; if some translation was already loaded - then the present values and metadata will be carried over to the stub ini.  
在用户配置文件中（如`SUN.INI`、`RA2MD.INI`）的`[Options]`里添加`GenerateTranslationStub=true`属性，让客户端在`Client`文件夹里生成`Translation.ini`，其中包含按字母顺序排列的几乎所有的文本键值，并注释掉没有翻译的值。
   - You can also specify `GenerateOnlyNewValuesInTranslationStub=true` in the same place to only output missing values instead of everything in the translation stub, which may be more convenient depending on your workflow.  
   设置`GenerateOnlyNewValuesInTranslationStub=true`将不会显示翻译过的条目
   - Non-text values (for instance, size and position) are not written to the stub INI, but you can still write them manually if needed.  
   非文本值（例如大小、位置）不会自动写入
1. Create a folder in `Resources/Translations` that uses the desired language code as name (see above) and place `Translation.ini` from `Client` folder there, and start translating the strings and uncommenting the translated ones.  
在`Resources/Translations`中创建翻译文件夹，并将`Client`文件夹中的`Translation.ini`放在那里，然后翻译并取消注释字符串。
   - Hardcoded strings are shared between same client binaries and are independent of mods, so you could reuse all the strings with `Client` prefix that you or someone else made for the language you're translating the client to. Or use `[INISystem]->BasedOn=  ; INI name` in the main `Translation.ini` to include a separate file (for instance, `ClientTranslation.ini`) with all the `Client`-prefixed strings placed in the same section.  
   硬编码字符串在相同的客户端内通用，且每个都独立，因此每个`Client`开头的字符串都能通用，或在Translation.ini中添加`[INISystem]->BasedOn= ;继承INI名`，以包含一个单独的文件（如`ClientTranslation.ini`），所有以`Client`为前缀的字符串都放在同一部分。
   - **Caveat:** hardcoded control size/position values are not read from the translation file at all; as a workaround ask the mod author to specify the size/position values that you will adjust using INI definition for that control, so that it can be adjusted using translation system.  
   **注意：**硬编码的控件大小、位置不会读取，可以定义在INI里以便翻译者修改。
   - To speed up the workflow it's advised to use an editor with multi-selection, like [Visual Studio Code](https://code.visualstudio.com), so that you can select values in batches. Select the `=` on the first untranslated line, press `Ctrl+D` as many times as needed to select the remaining `=` on untranslated lines, press `→`, then `Shift+End`. That will select all untranslated values for the lines you marked, so copy them and go to [DeepL](https://www.deepl.com) (recommended) or any other translator, paste the text, correct the translation, copy it back and paste in the same position. VSCode automatically splits the lines back so you don't need to input them one by one.  
   好的编辑器可以加快翻译速度，推荐[Visual Studio Code](https://code.visualstudio.com)，以便批量选择。选择第一个未翻译的行上的`=`，看情况按`Ctrl+D`选择未翻译的行上的`=`，按`→`，然后按`Shift+End`选择标记的行上所有未翻译的值并复制到[DeepL](https://www.deepl.com)（推荐）或其他翻译器里翻译，修改一下再复制粘贴让VSCode自动安排对应值。
     - DeepL also adds it's "translated with" line too, so you might need to paste the text in some intermediate file/window/tab, remove that line, and copy it again. 
     由于DeepL添加了`translated with（由…翻译）`的标记，可能要找个中转站删掉这行再复制粘贴回去。
2. For every translated asset, including theme-specific ones, you must replicate the exact path relative to the `Resources` folder for the original asset in your translation folder. The assets should also be named the same as the original ones. They will automatically override the non-translated ones.  
翻译的资源（包括特定主题的资源）必须按照`Resources`文件夹里的结构在翻译文件夹里存放以覆盖同名文件。
3. In case you need theme-specific translated values - create `Translation.ini` in the theme subfolder of your translation folder and put the needed key-value overrides in `[Values]` section (metadata won't be read from this file; also it won't be read at all if the main `Translation.ini` doesn't exist).  
特定主题的翻译需要在翻译文件夹中创建主题文件夹并放入`Translation.ini`，但不会读取元数据;如果没有`Translation.ini`不存在就不会读取整个主题文件夹）。
4. (optional) Look up the game/mod-specific ingame translation files that are specified in `ClientDefinitions.ini`->`[Translations]`->`GameFileX` and/or consult the game/mod author(s) for a list of files for ingame translation. Make and arrange your ingame translation into the files with specified names (first part of the value) and place them in your translation folder.  
（可选）查找在`ClientDefinitions.ini`->`[Translations]`->`GameFileX`中指定的游戏内翻译文件，可找游戏作者要游戏内翻译用的文件列表，翻译到第一个参数指定的文件中并放在翻译文件夹里。
   - If the game/mod has integrity-checked translation files - contact the game/mod author to include your translation with the game/mod package so the ingame translation won't make your or your users' installations trigger a modified files warning online.  
   如果翻译文件设定了完整性检查，请找作者收录你的翻译，这样游戏内翻译就触发修改文件警告。

Happy translating!  
翻译愉快！

## Miscellanous 杂项

- Discord presence, game broadcasting, stats etc. use untranslated names so that other players can see the more universal English names, and to not be locked onto a translation in case it changes.  
- Discord状态、游戏广播、统计数据等使用通用的原名防止混淆。
- When translated, original map names still display in a tooltip and can be copied via context menu.  
- 地图的原名仍会提示并且可右键复制。
- Where applicable, both translated and untranslated names are used to search (map and lobby searches).
- 搜索地图可以通过原名和译名搜索。
