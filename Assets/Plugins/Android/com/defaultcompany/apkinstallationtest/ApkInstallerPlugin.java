package com.defaultcompany.apkinstallationtest;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.util.Log;
import java.io.File;

public class ApkInstallerPlugin {
    
    private static final String TAG = "ApkInstallerPlugin";
    
    public static void installApk(Activity activity, String filePath) {
        try {
            Log.d(TAG, "=== Starting APK Installation ===");
            Log.d(TAG, "File path: " + filePath);
            Log.d(TAG, "Android SDK: " + Build.VERSION.SDK_INT);
            
            File file = new File(filePath);
            
            if (!file.exists()) {
                Log.e(TAG, "File does not exist!");
                throw new Exception("APK file does not exist: " + filePath);
            }
            
            Log.d(TAG, "File exists, size: " + file.length() + " bytes");
            Log.d(TAG, "File readable: " + file.canRead());

            Intent intent;
            Uri apkUri;
            
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                // Android 7.0+ (API 24+) - Use content:// URI
                Log.d(TAG, "Using content URI approach (API 24+)");
                
                String authority = activity.getPackageName() + ".fileprovider";
                String fileName = file.getName();
                
                // Build the content URI manually since we have a custom provider
                String uriString = "content://" + authority + "/cache/" + fileName;
                apkUri = Uri.parse(uriString);
                
                Log.d(TAG, "Authority: " + authority);
                Log.d(TAG, "Content URI: " + uriString);
                
                // Use ACTION_INSTALL_PACKAGE for modern Android
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                    intent = new Intent(Intent.ACTION_INSTALL_PACKAGE);
                    intent.setData(apkUri);
                    intent.putExtra(Intent.EXTRA_NOT_UNKNOWN_SOURCE, true);
                    intent.putExtra(Intent.EXTRA_RETURN_RESULT, true);
                    Log.d(TAG, "Using ACTION_INSTALL_PACKAGE");
                } else {
                    intent = new Intent(Intent.ACTION_VIEW);
                    intent.setDataAndType(apkUri, "application/vnd.android.package-archive");
                    Log.d(TAG, "Using ACTION_VIEW with setDataAndType");
                }
                
                intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                
                // Grant URI permission to package installer apps
                try {
                    activity.grantUriPermission(
                        "com.android.packageinstaller", 
                        apkUri, 
                        Intent.FLAG_GRANT_READ_URI_PERMISSION
                    );
                    Log.d(TAG, "Granted permission to com.android.packageinstaller");
                } catch (Exception e) {
                    Log.w(TAG, "Could not grant to com.android.packageinstaller: " + e.getMessage());
                }
                
                try {
                    activity.grantUriPermission(
                        "com.google.android.packageinstaller", 
                        apkUri, 
                        Intent.FLAG_GRANT_READ_URI_PERMISSION
                    );
                    Log.d(TAG, "Granted permission to com.google.android.packageinstaller");
                } catch (Exception e) {
                    Log.w(TAG, "Could not grant to com.google.android.packageinstaller: " + e.getMessage());
                }
                
            } else {
                // Android 6.0 and below - Use file:// URI
                Log.d(TAG, "Using file URI approach (API < 24)");
                apkUri = Uri.fromFile(file);
                intent = new Intent(Intent.ACTION_VIEW);
                intent.setDataAndType(apkUri, "application/vnd.android.package-archive");
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                Log.d(TAG, "File URI: " + apkUri.toString());
            }
            
            Log.d(TAG, "Intent action: " + intent.getAction());
            Log.d(TAG, "Intent data: " + intent.getData());
            Log.d(TAG, "Intent flags: " + intent.getFlags());
            
            Log.d(TAG, "Starting activity...");
            activity.startActivity(intent);
            Log.d(TAG, "Activity started successfully!");
            
        } catch (Exception e) {
            Log.e(TAG, "Installation failed with exception", e);
            e.printStackTrace();
            throw new RuntimeException("Failed to install APK: " + e.getMessage(), e);
        }
    }
}