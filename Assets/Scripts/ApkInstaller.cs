using System;
using UnityEngine;
using UnityEngine.UI;

public class ApkInstaller : MonoBehaviour
{
    [SerializeField]
    private Button installButton;
    [SerializeField]
    private FilePickHandler filePickHandler;
    private string pendingApkPath;

    private void Start()
    {
        if (installButton != null)
        {
            installButton.onClick.AddListener(OnInstallButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (installButton != null)
        {
            installButton.onClick.RemoveListener(OnInstallButtonClicked);
        }
    }

    private void OnInstallButtonClicked()
    {
        string apkPath = filePickHandler?.FilePath;
        if (string.IsNullOrEmpty(apkPath))
        {
            Debug.LogError("No APK file selected");
            return;
        }

        pendingApkPath = apkPath;
        Debug.Log($"=== Installation Flow Started ===");
        Debug.Log($"APK Path/URI: {apkPath}");
        Debug.Log($"Android API Level: {GetAndroidAPILevel()}");

        // Check install permission
        if (!CanRequestPackageInstalls())
        {
            Debug.LogWarning("Install unknown apps permission not granted. Opening settings...");
            OpenUnknownAppSettings();
            return;
        }

        InstallApk(pendingApkPath);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // When user returns from settings, retry installation
        if (hasFocus && !string.IsNullOrEmpty(pendingApkPath))
        {
            Debug.Log("App regained focus, checking permissions...");
            if (CanRequestPackageInstalls())
            {
                Debug.Log("Install permission granted, attempting installation...");
                InstallApk(pendingApkPath);
                pendingApkPath = null;
            }
        }
    }

    private bool CanRequestPackageInstalls()
    {
        if (Application.platform != RuntimePlatform.Android) return true;

        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");

            if (GetAndroidAPILevel() >= 26)
            {
                bool canInstall = packageManager.Call<bool>("canRequestPackageInstalls");
                Debug.Log($"Can request package installs: {canInstall}");
                return canInstall;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking install permission: {e.Message}");
            return false;
        }
    }

    private int GetAndroidAPILevel()
    {
        try
        {
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
        catch
        {
            return 0;
        }
    }

    private void OpenUnknownAppSettings()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            string packageName = currentActivity.Call<string>("getPackageName");

            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + packageName);

            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction", "android.settings.MANAGE_UNKNOWN_APP_SOURCES");
            intent.Call<AndroidJavaObject>("setData", uri);

            currentActivity.Call("startActivity", intent);
            Debug.Log("Opened unknown app sources settings");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to open settings: {e.Message}");
        }
    }

    private void InstallApk(string apkPath)
    {
        if (string.IsNullOrEmpty(apkPath))
        {
            Debug.LogError("Invalid APK path");
            return;
        }

        Debug.Log($"=== Starting APK Installation ===");
        Debug.Log($"APK Path: {apkPath}");

        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Verify file exists first
            AndroidJavaObject file = new AndroidJavaObject("java.io.File", apkPath);
            bool fileExists = file.Call<bool>("exists");
            long fileSize = file.Call<long>("length");
            bool canRead = file.Call<bool>("canRead");
            
            Debug.Log($"File exists: {fileExists}");
            Debug.Log($"File size: {fileSize} bytes");
            Debug.Log($"File readable: {canRead}");
            
            if (!fileExists || fileSize == 0)
            {
                Debug.LogError("APK file does not exist or is empty");
                return;
            }

            // Verify the APK is valid by checking the package info
            try
            {
                AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageArchiveInfo", 
                    apkPath, 0);
                
                if (packageInfo != null)
                {
                    string packageName = packageInfo.Get<string>("packageName");
                    string versionName = packageInfo.Get<string>("versionName");
                    int versionCode = packageInfo.Get<int>("versionCode");
                    
                    Debug.Log($"APK Package: {packageName}");
                    Debug.Log($"APK Version: {versionName} ({versionCode})");
                    Debug.Log("APK is valid!");
                }
                else
                {
                    Debug.LogError("APK file is corrupted or invalid - PackageManager cannot parse it");
                    Debug.LogError("The file may have been corrupted during copy operation");
                    return;
                }
            }
            catch (Exception verifyEx)
            {
                Debug.LogError($"APK verification failed: {verifyEx.Message}");
                Debug.LogError("The APK file appears to be corrupted");
                return;
            }

            // Check if Java plugin class exists
            try
            {
                AndroidJavaClass pluginClass = new AndroidJavaClass("com.defaultcompany.apkinstallationtest.ApkInstallerPlugin");
                Debug.Log("Java plugin class found!");
                
                // Call the installation method
                pluginClass.CallStatic("installApk", currentActivity, apkPath);
                
                Debug.Log("=== Installation method called successfully ===");
            }
            catch (Exception pluginEx)
            {
                Debug.LogError($"Java plugin error: {pluginEx.Message}");
                Debug.LogError("Make sure ApkInstallerPlugin.java is in: Assets/Plugins/Android/com/defaultcompany/apkinstallationtest/");
                throw;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"=== Installation failed ===");
            Debug.LogError($"Error: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }
}