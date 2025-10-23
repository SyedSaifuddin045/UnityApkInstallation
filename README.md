# Unity APK Installer

A comprehensive Unity-based Android application demonstrating APK installation functionality through native Android file pickers and custom Java plugins. This project showcases cross-platform integration between Unity's C# environment and Android's native Java layer.

##Demo Video

https://github.com/user-attachments/assets/8d176a75-a660-4593-a1db-bc9944882f7e

## Overview

This project provides a complete implementation for:

- **Native File Selection**: Integrates Android's native file picker interface for seamless APK file selection
- **Runtime Permission Management**: Handles dynamic permission requests for file access and package installation across different Android versions
- **Secure APK Installation**: Implements proper security measures using FileProvider and URI permissions
- **Android Version Compatibility**: Manages installation requirements across multiple Android API levels with appropriate fallbacks

## Prerequisites

### Required Software
- **Unity**: Version 2022.x or higher
- **Android API Level**: Minimum 26, Target 35 (Android 12+)
- **JDK**: Version 11 or higher
- **Android SDK**: Latest build tools

### Required Knowledge
- Unity development fundamentals
- Basic understanding of Android permissions model
- Familiarity with Unity-Android plugin architecture

## Dependencies

This project integrates the following third-party components:



- **[Native File Picker for Unity](https://github.com/yasirkula/UnityNativeFilePicker)** (v1.x)
  - Open source Unity plugin providing native file picking functionality
  - Handles platform-specific file access APIs
  - Supports multiple file types and selection modes

## Architecture

### Component Structure

```
Unity Layer (C#)
    ↓
Native File Picker Plugin
    ↓
Custom Java Plugin (ApkInstaller.java)
    ↓
Android System APIs
```

### Key Components

1. **ApkInstallerPlugin.java**: Custom Android plugin handling installation logic
2. **ApkFileProvider.java**: FileProvider implementation for secure file sharing
3. **Unity Scripts**: C# bridge scripts for plugin communication
4. **AndroidManifest.xml**: Permission and provider declarations (placed directly in `Assets/Plugins/Android/`)
5. **provider_paths.xml**: Optional file path configuration (if needed)

## Setup Instructions

### 1. Project Initialization

Create a new Unity project or open your existing project:
```bash
Unity Version: 2022.3 LTS (recommended)
Template: 3D or 2D based on your needs
```

### 2. Import Dependencies

#### Native File Picker Plugin
- **Option A**: Import from [GitHub Repository](https://github.com/yasirkula/UnityNativeFilePicker)
  - Download the latest `.unitypackage` release
  - Import via `Assets > Import Package > Custom Package`
  
- **Option B**: Use Unity Package Manager
  - Add package via Git URL if available

#### Project Files
Copy the following directory structure from this repository:
```
YourProject/
├── Assets/
│   ├── Plugins/
│   │   └── Android/
│   │       ├── AndroidManifest.xml
│   │       ├── provider_paths.xml
│   │       └── com/
│   │           └── defaultcompany/
│   │               └── apkinstallationtest/
│   │                   ├── ApkFileProvider.java
│   │                   └── ApkInstallerPlugin.java
│   └── Scripts/
│       ├── ApkInstallerBridge.cs
│       └── UIController.cs
```

**Note**: The Java package structure (`com/defaultcompany/apkinstallationtest/`) must match your Unity package name.

### 3. Configure Android Build Settings

Navigate to `Edit > Project Settings > Player > Android Settings`:

**Identification**
- **Package Name**: Set unique identifier (e.g., `com.yourcompany.apkinstaller`)

**Minimum API Level**
- Set to **API Level 26** (Android 8.0) or higher
- Recommended: API Level 29+ for better scoped storage support

**Target API Level**
- Set to **API Level 35** (Android 12+) or latest

**Scripting Backend**
- IL2CPP (recommended for better performance)
- Mono (acceptable for testing)

**Custom Manifest**
- Enable "Custom Main Manifest"
- Enable "Custom Main Gradle Template" (if needed)

### 4. Configure AndroidManifest.xml

Your `AndroidManifest.xml` should be placed directly in `Assets/Plugins/Android/` and include the following configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
          package="com.defaultcompany.apkinstallationtest"
          xmlns:tools="http://schemas.android.com/tools">

    <!-- Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" 
        android:maxSdkVersion="32" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"
        android:maxSdkVersion="28" 
        tools:ignore="ScopedStorage" />
    <uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />
    
    <!-- For Android 13+ (API 33+) -->
    <uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
    <uses-permission android:name="android.permission.READ_MEDIA_VIDEO" />
    <uses-permission android:name="android.permission.READ_MEDIA_AUDIO" />
    
    <application
        android:allowBackup="true"
        android:label="@string/app_name"
        android:theme="@style/UnityThemeSelector"
        android:requestLegacyExternalStorage="true"
        tools:ignore="GoogleAppIndexingWarning">

        <!-- Standard Unity Activity -->
        <activity android:name="com.unity3d.player.UnityPlayerActivity"
                  android:label="@string/app_name"
                  android:theme="@style/UnityThemeSelector"
                  android:screenOrientation="landscape"
                  android:launchMode="singleTask"
                  android:configChanges="keyboard|keyboardHidden|navigation|orientation|screenSize|uiMode"
                  android:hardwareAccelerated="true"
                  android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
            <meta-data android:name="unityplayer.UnityActivity" android:value="true" />
            <meta-data android:name="android.app.lib_name" android:value="unity" />
        </activity>

        <!-- Custom FileProvider for sharing APK files -->
        <provider
            android:name="com.defaultcompany.apkinstallationtest.ApkFileProvider"
            android:authorities="${applicationId}.fileprovider"
            android:exported="false"
            android:grantUriPermissions="true" />

    </application>
</manifest>
```

**Important Notes**:
- Replace `com.defaultcompany.apkinstallationtest` with your actual Unity package name
- The `package` attribute should match your Unity Player Settings package name
- The `ApkFileProvider` class name must match the Java package structure
- The `android:requestLegacyExternalStorage="true"` helps with compatibility on Android 10-11

### 5. Configure FileProvider Paths (Optional)

**Note**: Based on your working configuration, the FileProvider **does not require** the `provider_paths.xml` to be in a `res/xml/` folder structure. Your configuration works without the `<meta-data>` tag for file paths.

If you want to explicitly define file paths (optional for this implementation), create `Assets/Plugins/Android/provider_paths.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<paths xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- External storage root -->
    <external-path 
        name="external_files" 
        path="." />
    
    <!-- App-specific external storage -->
    <external-files-path 
        name="external_app_files" 
        path="." />
</paths>
```

### 6. Scene Configuration

**Create UI Elements**:
1. Create a Canvas (`GameObject > UI > Canvas`)
2. Add a Button for file selection
3. Add Text elements for status messages
4. Add Image component for loading indicators (optional)

**Attach Scripts**:
1. Create an empty GameObject named "APKInstaller"
2. Attach the `ApkInstallerBridge.cs` script
3. Attach the `UIController.cs` to your Canvas
4. Link UI elements in the Inspector

## Usage Guide

### Basic Implementation

```csharp
using UnityEngine;
using NativeFilePicker;

public class ApkInstallerExample : MonoBehaviour
{
    public void SelectAndInstallAPK()
    {
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile(
            (path) => {
                if (path != null)
                {
                    Debug.Log("Selected file: " + path);
                    InstallAPK(path);
                }
            },
            new string[] { "application/vnd.android.package-archive" }
        );
        
        Debug.Log("File picker permission: " + permission);
    }
    
    private void InstallAPK(string apkPath)
    {
        using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject installer = new AndroidJavaObject("com.defaultcompany.apkinstallationtest.ApkInstallerPlugin"))
        {
            installer.Call("installAPK", activity, apkPath);
        }
    }
}
```

### Runtime Flow

1. **User Interaction**: User taps "Select APK" button
2. **Permission Check**: App verifies storage permissions
3. **File Picker**: Native file picker opens filtered to APK files
4. **File Selection**: User selects APK file
5. **Installation Request**: App invokes custom Java plugin
6. **System Prompt**: Android displays installation confirmation
7. **Installation**: User confirms and app installs

### Permission Handling

The application handles permissions differently based on Android version:

- **Android 6-10**: Requests `READ_EXTERNAL_STORAGE`
- **Android 11-12**: Uses scoped storage with `READ_EXTERNAL_STORAGE`
- **Android 13+**: Requests granular media permissions
- **All versions**: Requires `REQUEST_INSTALL_PACKAGES`

## Testing

### Test Environment

**Verified Configurations**:
- Unity 2022.3.10f1 LTS
- Android API Level 35 (Android 12)
- Tested devices:
  - Google Pixel 6 (Android 13)
  - Samsung Galaxy S21 (Android 12)

**Not Yet Tested**:
- Unity 6+ versions
- Android 13+ devices
- Android TV platforms
- Tablets and foldable devices

### Test Scenarios

1. **Basic Installation**: Install a simple APK
2. **Permission Denial**: Test behavior when permissions are denied
3. **Invalid APK**: Attempt to install corrupted APK
4. **Large APK**: Test with APK files > 100MB
5. **Update Installation**: Install newer version of existing app

### Debugging Tips

Enable verbose logging:
```csharp
Debug.unityLogger.logEnabled = true;
```

Monitor Android logcat:
```bash
adb logcat -s Unity ActivityManager PackageManager
```

## Troubleshooting

### Common Issues

**Issue**: FileProvider not found
- **Solution**: Verify package name matches in `AndroidManifest.xml` and `ApkFileProvider.java`

**Issue**: Installation fails silently
- **Solution**: Check `REQUEST_INSTALL_PACKAGES` permission is granted

**Issue**: File picker shows no files
- **Solution**: Verify storage permissions are granted and APK filter is correct

**Issue**: Build errors with Java plugin
- **Solution**: Ensure JDK 11+ is installed and configured in Unity preferences

## Security Considerations

- **Never install APKs from untrusted sources** in production apps
- Implement signature verification for production use
- Use HTTPS for APK downloads
- Validate APK integrity before installation
- Request only necessary permissions
- Follow Android's security best practices

## Performance Optimization

- Use coroutines for async operations
- Implement file size checks before installation
- Cache FileProvider URIs when appropriate
- Dispose Android Java objects properly

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 Syed Saifuddin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files...
```

## Acknowledgments

- **[yasirkula](https://github.com/yasirkula)** - For the excellent Native File Picker plugin
- **Unity Technologies** - For comprehensive Android integration documentation
- **Android Open Source Project** - For FileProvider and package management APIs

## Changelog

### Version 1.0.0 (Current)
- Initial release
- Basic APK installation functionality
- Android 12+ support
- Native file picker integration

---

**Note**: This project is for educational purposes. Always follow Google Play policies and Android security guidelines when distributing applications.
