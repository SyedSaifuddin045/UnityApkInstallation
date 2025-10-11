using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FilePickHandler : MonoBehaviour
{
    [SerializeField] 
    private Button pickButton;
    
    [SerializeField] 
    private TMP_InputField pathInputField;

    private string filePath;
    public string FilePath => filePath;

    private void Start()
    {
        if (pickButton != null)
        {
            pickButton.onClick.AddListener(PickApkFile);
        }
    }

    private void OnDestroy()
    {
        if (pickButton != null)
        {
            pickButton.onClick.RemoveListener(PickApkFile);
        }
    }

    private void PickApkFile()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogWarning("File picker only works on Android");
            return;
        }

        // NativeFilePicker handles permissions internally for SAF
        bool permission = NativeFilePicker.CheckPermission(true);  // true for read-only

        if (permission == false)
        {
            NativeFilePicker.RequestPermissionAsync((result) =>
            {
                if (result == NativeFilePicker.Permission.Granted)
                {
                    LaunchFilePicker();
                }
                else
                {
                    Debug.LogWarning("Permission denied. Open settings?");
                    NativeFilePicker.OpenSettings();
                }
            }, true);  // true for read-only
        }
        else if (permission == true)
        {
            LaunchFilePicker();
        }
        else
        {
            Debug.LogError("Permission denied. User must enable in settings.");
            NativeFilePicker.OpenSettings();
        }
    }

    private void LaunchFilePicker()
    {
        NativeFilePicker.PickFile((selectedPath) =>
        {
            if (selectedPath != null)
            {
                filePath = selectedPath;
                Debug.Log($"Selected APK URI: {filePath}");
                
                if (pathInputField != null)
                {
                    pathInputField.text = GetFileNameFromUri(selectedPath);
                }
            }
            else
            {
                Debug.Log("File selection cancelled");
            }
        }, new string[] { "application/vnd.android.package-archive" });

        Debug.Log("File picker launched");
    }

    private string GetFileNameFromUri(string uriString)
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject contentResolver = activity.Call<AndroidJavaObject>("getContentResolver");
            
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", uriString);
            
            string[] projection = new string[] { "_display_name" };
            AndroidJavaObject cursor = contentResolver.Call<AndroidJavaObject>("query", 
                uri, projection, null, null, null);
            
            if (cursor != null && cursor.Call<bool>("moveToFirst"))
            {
                int nameIndex = cursor.Call<int>("getColumnIndex", "_display_name");
                if (nameIndex >= 0)
                {
                    string fileName = cursor.Call<string>("getString", nameIndex);
                    cursor.Call("close");
                    return fileName;
                }
                cursor.Call("close");
            }
            
            return uri.Call<string>("getLastPathSegment") ?? "Selected APK";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not get file name: {e.Message}");
            return "Selected APK";
        }
    }
}