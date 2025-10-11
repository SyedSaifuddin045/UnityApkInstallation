package com.defaultcompany.apkinstallationtest;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;
import android.util.Log;
import android.webkit.MimeTypeMap;
import java.io.File;
import java.io.FileNotFoundException;

public class ApkFileProvider extends ContentProvider {
    
    private static final String TAG = "ApkFileProvider";
    private static final String[] COLUMNS = {
        OpenableColumns.DISPLAY_NAME,
        OpenableColumns.SIZE
    };

    @Override
    public boolean onCreate() {
        Log.d(TAG, "ApkFileProvider created successfully");
        Log.d(TAG, "Authority: " + getContext().getPackageName() + ".fileprovider");
        return true;
    }

    @Override
    public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        Log.d(TAG, "openFile called");
        Log.d(TAG, "URI: " + uri.toString());
        Log.d(TAG, "Mode: " + mode);
        
        File file = getFileForUri(uri);
        
        if (file == null) {
            Log.e(TAG, "File is null for URI: " + uri);
            throw new FileNotFoundException("Could not resolve file for URI: " + uri);
        }
        
        if (!file.exists()) {
            Log.e(TAG, "File does not exist: " + file.getAbsolutePath());
            throw new FileNotFoundException("File not found: " + file.getAbsolutePath());
        }
        
        Log.d(TAG, "File found: " + file.getAbsolutePath());
        Log.d(TAG, "File size: " + file.length() + " bytes");
        Log.d(TAG, "File readable: " + file.canRead());
        
        int fileMode = ParcelFileDescriptor.MODE_READ_ONLY;
        if ("w".equals(mode)) {
            fileMode = ParcelFileDescriptor.MODE_WRITE_ONLY;
        } else if ("rw".equals(mode)) {
            fileMode = ParcelFileDescriptor.MODE_READ_WRITE;
        }
        
        try {
            ParcelFileDescriptor pfd = ParcelFileDescriptor.open(file, fileMode);
            Log.d(TAG, "ParcelFileDescriptor created successfully");
            return pfd;
        } catch (Exception e) {
            Log.e(TAG, "Failed to open ParcelFileDescriptor", e);
            throw new FileNotFoundException("Could not open file: " + e.getMessage());
        }
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, 
                       String[] selectionArgs, String sortOrder) {
        Log.d(TAG, "query called for URI: " + uri.toString());
        
        File file = getFileForUri(uri);
        
        if (file == null) {
            Log.e(TAG, "File is null for URI in query");
            return null;
        }

        if (projection == null) {
            projection = COLUMNS;
        }

        String[] cols = new String[projection.length];
        Object[] values = new Object[projection.length];
        int i = 0;
        
        for (String col : projection) {
            if (OpenableColumns.DISPLAY_NAME.equals(col)) {
                cols[i] = OpenableColumns.DISPLAY_NAME;
                values[i++] = file.getName();
                Log.d(TAG, "Display name: " + file.getName());
            } else if (OpenableColumns.SIZE.equals(col)) {
                cols[i] = OpenableColumns.SIZE;
                values[i++] = file.length();
                Log.d(TAG, "File size: " + file.length());
            }
        }

        cols = copyOf(cols, i);
        values = copyOf(values, i);

        final MatrixCursor cursor = new MatrixCursor(cols, 1);
        cursor.addRow(values);
        
        Log.d(TAG, "Query completed successfully");
        return cursor;
    }

    @Override
    public String getType(Uri uri) {
        Log.d(TAG, "getType called for URI: " + uri.toString());
        
        File file = getFileForUri(uri);
        
        if (file == null) {
            Log.e(TAG, "File is null in getType");
            return null;
        }

        String fileName = file.getName();
        final int lastDot = fileName.lastIndexOf('.');
        
        if (lastDot >= 0) {
            final String extension = fileName.substring(lastDot + 1);
            final String mime = MimeTypeMap.getSingleton().getMimeTypeFromExtension(extension);
            
            if (mime != null) {
                Log.d(TAG, "MIME type: " + mime);
                return mime;
            }
        }

        Log.d(TAG, "Using default MIME type: application/octet-stream");
        return "application/octet-stream";
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        throw new UnsupportedOperationException("No external inserts");
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException("No external deletes");
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException("No external updates");
    }

    private File getFileForUri(Uri uri) {
        String path = uri.getEncodedPath();
        
        Log.d(TAG, "Resolving path: " + path);
        
        if (path == null) {
            Log.e(TAG, "Path is null");
            return null;
        }

        // Remove leading slash
        if (path.startsWith("/")) {
            path = path.substring(1);
            Log.d(TAG, "Path after removing leading slash: " + path);
        }

        File resultFile = null;

        // Handle cache directory (use external cache)
        if (path.startsWith("cache/")) {
            File cacheDir = getContext().getExternalCacheDir();
            String fileName = path.substring(6); // Remove "cache/"
            resultFile = new File(cacheDir, fileName);
            Log.d(TAG, "External cache directory file: " + resultFile.getAbsolutePath());
        }
        // Handle files directory (optionally update to external if needed)
        else if (path.startsWith("files/")) {
            File filesDir = getContext().getFilesDir();
            String fileName = path.substring(6); // Remove "files/"
            resultFile = new File(filesDir, fileName);
            Log.d(TAG, "Files directory file: " + resultFile.getAbsolutePath());
        }
        // Fallback to external cache
        else {
            File cacheDir = getContext().getExternalCacheDir();
            resultFile = new File(cacheDir, path);
            Log.d(TAG, "Fallback to external cache with full path: " + resultFile.getAbsolutePath());
        }

        if (resultFile != null) {
            Log.d(TAG, "Resolved file exists: " + resultFile.exists());
        }

        return resultFile;
    }

    private static String[] copyOf(String[] original, int newLength) {
        final String[] result = new String[newLength];
        System.arraycopy(original, 0, result, 0, newLength);
        return result;
    }

    private static Object[] copyOf(Object[] original, int newLength) {
        final Object[] result = new Object[newLength];
        System.arraycopy(original, 0, result, 0, newLength);
        return result;
    }
}