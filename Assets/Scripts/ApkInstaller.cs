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
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

            // Create a content URI using ContentProvider approach
            AndroidJavaObject uri = GetContentUriForFile(context, apkPath);

            if (uri == null)
            {
                Debug.LogError("Failed to create content URI");
                return;
            }

            Debug.Log("Content URI created successfully");

            // Create install intent
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
            
            intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");

            // Add flags
            int flagGrantRead = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
            int flagNewTask = intentClass.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");
            
            intent.Call<AndroidJavaObject>("addFlags", flagGrantRead);
            intent.Call<AndroidJavaObject>("addFlags", flagNewTask);

            // Start installation
            currentActivity.Call("startActivity", intent);
            Debug.Log("=== Installation intent sent successfully ===");
        }
        catch (Exception e)
        {
            Debug.LogError($"=== Installation failed ===");
            Debug.LogError($"Error: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    private AndroidJavaObject GetContentUriForFile(AndroidJavaObject context, string filePath)
    {
        try
        {
            // Method 1: Try using Unity's built-in ContentProvider
            string packageName = context.Call<string>("getPackageName");
            string authority = packageName + ".fileprovider";

            AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath);
            
            // Try to use content provider with custom implementation
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            
            // Build content URI manually
            string fileName = file.Call<string>("getName");
            string contentUri = $"content://{authority}/cache/{fileName}";
            
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", contentUri);
            
            Debug.Log($"Created content URI: {contentUri}");
            return uri;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create content URI: {e.Message}");
            
            // Fallback: Try direct file URI (may not work on newer Android versions)
            try
            {
                AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath);
                AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
                AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("fromFile", file);
                Debug.LogWarning("Using file:// URI (may not work on Android 7+)");
                return uri;
            }
            catch (Exception fallbackEx)
            {
                Debug.LogError($"Fallback URI creation failed: {fallbackEx.Message}");
                return null;
            }
        }
    }
}