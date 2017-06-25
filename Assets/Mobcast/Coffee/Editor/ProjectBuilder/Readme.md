ProjectBuilder
===

## Overview

Manage building configuration with asset.

* Build target
* Company name
* Product name
* Application identifier
    * (Android) PackageName
    * (iOS) BundleIdentifier
* Development build option(Development & Script debugging)
* Defined Symbols
* Enable/Disable scenes in build
* Application version
* Internal version
    * (Android) BundleVersionCode
    * (iOS) BuildNumber
* Android platform support
    * Keystore infomation
* iOS platform support
    * XCode modifier.
        * Languages
        * Frameworks
        * Services
        * Entitlement file
    * Signing & Provisioning profile
    * Generate exportOptions.plist



## Requirement

* Unity5.3+
* No other SDK



## Usage For CUI

The following command option specifies the builder and executes the build.

`-batchmode -buildTarget <ios|android> -executeMethod Mobcast.Coffee.Build.ProjectBuilder.Build -builder <builder_name> [-devBuildNumber <number>] [-appendSymbols 'INCLUDE_SYMBOL;!EXCLUDE_SYMBOL;...']`

Note: Do not use `-quit` option.

For other infomation, see this link : <https://docs.unity3d.com/Manual/CommandLineArguments.html>




## Usage For Unity Cloud Build

Type `Mobcast.Coffee.ProjectBuilder.PreExport` in `Advanced Settings -> Pre-Export Method Name`.

Builder asset used for building will be selected automatically based on build setting label.

For example, a build setting labeled 'Develop iOS' selects 'Develop_iOS' builder asset in project.




## How To Add Platform For Build

Implement `IPlatformSettings` interface as following for platforms you need.

Add `System.Serializable` attribute to the class to be serializable.

```cs
[System.Serializable]
public class PlatformSettings_WebGL : IPlatformSettings
{
	public BuildTarget platform{get{ return BuildTarget.WebGL;}}
	public Texture icon{get{ return EditorGUIUtility.FindTexture("BuildSettings.WebGL.Small");}}
	public void Reset(){}
	public void ApplySettings(ProjectBuilder builder){}
	public void DrawSetting(SerializedObject serializedObject){}
}
```



## Release Notes

### ver.0.7.0:

* Fix: 'Usage For CUI' was corrected.
* Fix: Util.projectDir has error after compiling.
* Fix: (iOS) When 'Automatically Sign' is enabled, ignore 'Provisioning Profile Id', etc...

### ver.0.6.0:

* Feature: Supports WebGL.
* Feature: Supports other platforms by implementing `IPlatformSettings` interface.


### ver.0.5.0:

* Feature: Supports Unity Cloud Build.
* Feature: Enable/Disable scenes for build.


### ver.0.4.0:

* Obsolete: Obsolete 'Custom build pipeline' to simplify.
* Obsolete: Obsolete 'Generate CUI command' and explain it in Readme instead.
* Obsolete: Several CUI command options.
* Obsolete: Several BuildOptions.
* Feature: (iOS) Supports language options for XCode.


### ver.0.3.0:

* Feature: (iOS) Supports XCode 8 & Automatically Sign.
* Feature: (iOS) Add framework options.
* Feature: (iOS) Add entitlement file.
* Feature: (iOS) Apple servises (iCloud, Push, GameCenter, etc...) can be enable.
* Fix: Inspector GUI.


### ver.0.2.0:

* Supports Unity5.5+.
* Improvement: Setting items are simplified. Several items have been deleted.
* Feature: Add button to 'Build & Run'.
* Feature: (iOS) Generate 'exportOptions.plist'.
* Fix: Generated CUI command is incorrect.


### ver.0.1.0:

* Feature: Manage build configuration with builder asset.
* Feature: (Android) Keystore information.
* Feature: Custom build pipeline.
* Feature: Export `BUILD_VERSION` for CI.