using System.IO;
using UnityEditor.Android;
using UnityEngine;

public class AndroidPostProcess : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // Detect if this is the root project (check for submodule directories)
        if (!Directory.Exists(Path.Combine(path, "unityLibrary")) || !Directory.Exists(Path.Combine(path, "launcher")))
        {
            Debug.Log("Skipping post-process for non-root path: " + path);
            return;  // Skip for submodules
        }

        string gradlePropertiesPath = Path.Combine(path, "gradle.properties");

        // If the file doesn't exist, create it with default content
        if (!File.Exists(gradlePropertiesPath))
        {
            File.WriteAllText(gradlePropertiesPath, "# Default gradle.properties content\n");
            Debug.Log("Created missing gradle.properties at: " + gradlePropertiesPath);
        }

        // Now read and modify
        string[] lines = File.ReadAllLines(gradlePropertiesPath);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool hasAndroidX = false;
        bool hasJetifier = false;

        foreach (string line in lines)
        {
            if (line.Contains("android.useAndroidX"))
            {
                builder.AppendLine("android.useAndroidX=true");
                hasAndroidX = true;
                continue;
            }
            if (line.Contains("android.enableJetifier"))
            {
                builder.AppendLine("android.enableJetifier=true");
                hasJetifier = true;
                continue;
            }
            builder.AppendLine(line);
        }

        // Append if missing
        if (!hasAndroidX) builder.AppendLine("android.useAndroidX=true");
        if (!hasJetifier) builder.AppendLine("android.enableJetifier=true");

        File.WriteAllText(gradlePropertiesPath, builder.ToString());

        Debug.Log("AndroidX and Jetifier enabled in gradle.properties at: " + gradlePropertiesPath);
    }
}