package com.arbadmintonnet.replay;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.hardware.display.DisplayManager;
import android.hardware.display.VirtualDisplay;
import android.media.MediaRecorder;
import android.media.projection.MediaProjection;
import android.media.projection.MediaProjectionManager;
import android.os.Environment;
import android.os.Handler;
import android.os.Looper;
import android.util.DisplayMetrics;
import android.util.Log;
import android.view.WindowManager;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Android screen recording bridge for Unity.
 * Uses MediaProjection API to continuously record the screen in segments,
 * keeping a ring buffer of the last N seconds for instant replay.
 */
public class ScreenRecorderBridge {

    private static final String TAG = "ScreenRecorderBridge";
    private static final int REQUEST_CODE_SCREEN_CAPTURE = 9001;
    private static final float SEGMENT_DURATION_SECONDS = 10f;

    private static ScreenRecorderBridge instance;

    private Activity activity;
    private MediaProjectionManager projectionManager;
    private MediaProjection mediaProjection;
    private MediaRecorder mediaRecorder;
    private VirtualDisplay virtualDisplay;
    private Handler mainHandler;

    private boolean isBuffering = false;
    private boolean permissionGranted = false;
    private int resultCode;
    private Intent resultData;

    // Ring buffer of segment file paths
    private final List<String> segmentPaths = Collections.synchronizedList(new ArrayList<>());
    private int segmentIndex = 0;
    private int maxSegments = 6; // Keep ~60s of segments at 10s each

    // Export state: 0=idle, 1=exporting, 2=success, 3=error
    private volatile int exportStatus = 0;
    private volatile String exportedClipPath = "";
    private volatile String errorMessage = "";

    // Screen metrics
    private int screenWidth;
    private int screenHeight;
    private int screenDensity;

    // Segment rotation timer
    private Runnable segmentRotationRunnable;

    public static ScreenRecorderBridge getInstance(Activity activity) {
        if (instance == null) {
            instance = new ScreenRecorderBridge(activity);
        }
        return instance;
    }

    public static ScreenRecorderBridge getInstance() {
        return instance;
    }

    private ScreenRecorderBridge(Activity activity) {
        this.activity = activity;
        this.mainHandler = new Handler(Looper.getMainLooper());
        this.projectionManager = (MediaProjectionManager) activity.getSystemService(Context.MEDIA_PROJECTION_SERVICE);

        // Get screen metrics
        DisplayMetrics metrics = new DisplayMetrics();
        WindowManager wm = (WindowManager) activity.getSystemService(Context.WINDOW_SERVICE);
        wm.getDefaultDisplay().getMetrics(metrics);

        // Use lower resolution for performance (720p max)
        float scale = Math.min(1.0f, 720.0f / Math.min(metrics.widthPixels, metrics.heightPixels));
        screenWidth = (int) (metrics.widthPixels * scale);
        screenHeight = (int) (metrics.heightPixels * scale);
        // Ensure even dimensions (required by MediaRecorder)
        screenWidth = screenWidth % 2 == 0 ? screenWidth : screenWidth - 1;
        screenHeight = screenHeight % 2 == 0 ? screenHeight : screenHeight - 1;
        screenDensity = metrics.densityDpi;

        Log.d(TAG, "Screen recorder initialized: " + screenWidth + "x" + screenHeight);
    }

    /**
     * Request screen capture permission by launching the helper
     * ScreenRecorderActivity.
     */
    public void requestPermission() {
        if (permissionGranted && resultData != null) {
            Log.d(TAG, "Permission already granted");
            return;
        }

        Log.d(TAG, "Launching ScreenRecorderActivity for permission");
        Intent intent = new Intent(activity, ScreenRecorderActivity.class);
        activity.startActivity(intent);
    }

    /**
     * Called by ScreenRecorderActivity to issue the actual permission request.
     */
    public void requestPermissionWithActivity(Activity helperActivity) {
        Log.d(TAG, "Requesting screen capture permission via helper activity");
        Intent captureIntent = projectionManager.createScreenCaptureIntent();
        helperActivity.startActivityForResult(captureIntent, REQUEST_CODE_SCREEN_CAPTURE);
    }

    /**
     * Called from Unity when activity result is received.
     */
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == REQUEST_CODE_SCREEN_CAPTURE) {
            if (resultCode == Activity.RESULT_OK && data != null) {
                this.resultCode = resultCode;
                this.resultData = data;
                this.permissionGranted = true;
                Log.d(TAG, "Screen capture permission granted");

                // If buffering was pending, start it now
                if (!isBuffering) {
                    startBufferingInternal();
                }
            } else {
                this.permissionGranted = false;
                errorMessage = "Screen capture permission denied";
                Log.w(TAG, errorMessage);
            }
        }
    }

    /**
     * Start continuous clip buffering.
     */
    public void startBuffering() {
        if (isBuffering) {
            Log.d(TAG, "Already buffering");
            return;
        }

        if (!permissionGranted || resultData == null) {
            // Need to request permission first
            Log.d(TAG, "Permission not yet granted, requesting...");
            requestPermission();
            return;
        }

        startBufferingInternal();
    }

    private void startBufferingInternal() {
        try {
            // Android 10+ requires a foreground service before using MediaProjection
            startForegroundService();

            mediaProjection = projectionManager.getMediaProjection(resultCode, resultData);
            if (mediaProjection == null) {
                errorMessage = "Failed to create MediaProjection";
                Log.e(TAG, errorMessage);
                stopForegroundService();
                return;
            }

            mediaProjection.registerCallback(new MediaProjection.Callback() {
                @Override
                public void onStop() {
                    Log.d(TAG, "MediaProjection stopped");
                    cleanupRecorder();
                }
            }, mainHandler);

            startNewSegment();
            isBuffering = true;
            Log.d(TAG, "Clip buffering started");

            // Schedule segment rotation
            scheduleSegmentRotation();

        } catch (Exception e) {
            errorMessage = "Failed to start buffering: " + e.getMessage();
            Log.e(TAG, errorMessage, e);
            stopForegroundService();
        }
    }

    private void scheduleSegmentRotation() {
        segmentRotationRunnable = new Runnable() {
            @Override
            public void run() {
                if (isBuffering) {
                    rotateSegment();
                    mainHandler.postDelayed(this, (long) (SEGMENT_DURATION_SECONDS * 1000));
                }
            }
        };
        mainHandler.postDelayed(segmentRotationRunnable, (long) (SEGMENT_DURATION_SECONDS * 1000));
    }

    private void startNewSegment() throws Exception {
        String filePath = getSegmentFilePath(segmentIndex);

        // Delete old file if exists
        File file = new File(filePath);
        if (file.exists())
            file.delete();

        mediaRecorder = new MediaRecorder();
        mediaRecorder.setVideoSource(MediaRecorder.VideoSource.SURFACE);
        mediaRecorder.setOutputFormat(MediaRecorder.OutputFormat.MPEG_4);
        mediaRecorder.setVideoEncoder(MediaRecorder.VideoEncoder.H264);
        mediaRecorder.setVideoSize(screenWidth, screenHeight);
        mediaRecorder.setVideoFrameRate(30);
        mediaRecorder.setVideoEncodingBitRate(6 * 1000 * 1000); // 6 Mbps
        mediaRecorder.setOutputFile(filePath);

        mediaRecorder.prepare();

        virtualDisplay = mediaProjection.createVirtualDisplay(
                "ScreenRecorder",
                screenWidth, screenHeight, screenDensity,
                DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
                mediaRecorder.getSurface(),
                null, null);

        mediaRecorder.start();

        synchronized (segmentPaths) {
            // Remove old segment at this index if it exists
            while (segmentPaths.size() > segmentIndex) {
                segmentPaths.remove(segmentPaths.size() - 1);
            }
            segmentPaths.add(filePath);
        }

        Log.d(TAG, "Recording segment " + segmentIndex + ": " + filePath);
    }

    private void rotateSegment() {
        try {
            // Stop current recorder
            if (mediaRecorder != null) {
                try {
                    mediaRecorder.stop();
                } catch (RuntimeException e) {
                    Log.w(TAG, "Error stopping recorder during rotation: " + e.getMessage());
                }
                mediaRecorder.release();
                mediaRecorder = null;
            }

            if (virtualDisplay != null) {
                virtualDisplay.release();
                virtualDisplay = null;
            }

            // Trim old segments
            synchronized (segmentPaths) {
                while (segmentPaths.size() > maxSegments) {
                    String oldPath = segmentPaths.remove(0);
                    File oldFile = new File(oldPath);
                    if (oldFile.exists())
                        oldFile.delete();
                }
            }

            // Start next segment
            segmentIndex++;
            startNewSegment();

        } catch (Exception e) {
            Log.e(TAG, "Error rotating segment: " + e.getMessage(), e);
            errorMessage = "Segment rotation error: " + e.getMessage();
        }
    }

    /**
     * Stop clip buffering.
     */
    public void stopBuffering() {
        if (!isBuffering)
            return;

        isBuffering = false;

        // Cancel segment rotation
        if (segmentRotationRunnable != null) {
            mainHandler.removeCallbacks(segmentRotationRunnable);
            segmentRotationRunnable = null;
        }

        cleanupRecorder();

        if (mediaProjection != null) {
            mediaProjection.stop();
            mediaProjection = null;
        }

        stopForegroundService();

        Log.d(TAG, "Clip buffering stopped");
    }

    private void cleanupRecorder() {
        if (mediaRecorder != null) {
            try {
                mediaRecorder.stop();
            } catch (RuntimeException e) {
                Log.w(TAG, "Error stopping recorder: " + e.getMessage());
            }
            mediaRecorder.release();
            mediaRecorder = null;
        }

        if (virtualDisplay != null) {
            virtualDisplay.release();
            virtualDisplay = null;
        }
    }

    /**
     * Export the last N seconds of buffered video.
     */
    public void exportClip(final float duration) {
        if (exportStatus == 1) {
            Log.w(TAG, "Already exporting");
            return;
        }

        exportStatus = 1; // exporting

        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    // Pause current recording temporarily to flush
                    // Copy the most recent segment files
                    List<String> filesToMerge;
                    synchronized (segmentPaths) {
                        filesToMerge = new ArrayList<>(segmentPaths);
                    }

                    if (filesToMerge.isEmpty()) {
                        errorMessage = "No recorded segments available";
                        exportStatus = 3;
                        return;
                    }

                    // For simplicity, use the most recent segment file as the export
                    // (In a production app, you'd merge segments using MediaMuxer)
                    String latestSegment = filesToMerge.get(filesToMerge.size() - 1);
                    File srcFile = new File(latestSegment);

                    if (!srcFile.exists() || srcFile.length() == 0) {
                        // Try the previous segment if current is still being written
                        if (filesToMerge.size() > 1) {
                            latestSegment = filesToMerge.get(filesToMerge.size() - 2);
                            srcFile = new File(latestSegment);
                        }
                    }

                    if (!srcFile.exists() || srcFile.length() == 0) {
                        errorMessage = "No valid recorded segments found";
                        exportStatus = 3;
                        return;
                    }

                    // Copy to export location
                    String exportDir = activity.getCacheDir().getAbsolutePath();
                    long timestamp = System.currentTimeMillis() / 1000;
                    String exportPath = exportDir + "/replay_" + timestamp + ".mp4";
                    File exportFile = new File(exportPath);

                    copyFile(srcFile, exportFile);

                    exportedClipPath = exportPath;
                    exportStatus = 2; // success
                    Log.d(TAG, "Export successful: " + exportPath);

                } catch (Exception e) {
                    errorMessage = "Export failed: " + e.getMessage();
                    exportStatus = 3;
                    Log.e(TAG, errorMessage, e);
                }
            }
        }).start();
    }

    private void copyFile(File src, File dst) throws IOException {
        FileInputStream in = new FileInputStream(src);
        FileOutputStream out = new FileOutputStream(dst);
        byte[] buffer = new byte[8192];
        int len;
        while ((len = in.read(buffer)) > 0) {
            out.write(buffer, 0, len);
        }
        in.close();
        out.close();
    }

    // ====== Status Getters (called from Unity) ======

    public boolean getIsBuffering() {
        return isBuffering;
    }

    public int getExportStatus() {
        return exportStatus;
    }

    public String getExportedClipPath() {
        return exportedClipPath;
    }

    public String getErrorMessage() {
        return errorMessage;
    }

    public void resetExportStatus() {
        exportStatus = 0;
        errorMessage = "";
    }

    public boolean isPermissionGranted() {
        return permissionGranted;
    }

    // ====== Helpers ======

    private String getSegmentFilePath(int index) {
        String dir = activity.getCacheDir().getAbsolutePath() + "/replay_segments";
        File dirFile = new File(dir);
        if (!dirFile.exists())
            dirFile.mkdirs();
        return dir + "/segment_" + index + ".mp4";
    }

    /**
     * Cleanup all segment files.
     */
    public void cleanup() {
        stopBuffering();

        synchronized (segmentPaths) {
            for (String path : segmentPaths) {
                File f = new File(path);
                if (f.exists())
                    f.delete();
            }
            segmentPaths.clear();
        }

        segmentIndex = 0;
        Log.d(TAG, "Cleanup complete");
    }

    public static int getRequestCode() {
        return REQUEST_CODE_SCREEN_CAPTURE;
    }

    // ====== Foreground Service ======

    private void startForegroundService() {
        Log.d(TAG, "Starting foreground service for MediaProjection");
        Intent serviceIntent = new Intent(activity, ScreenRecorderService.class);
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
            activity.startForegroundService(serviceIntent);
        } else {
            activity.startService(serviceIntent);
        }
    }

    private void stopForegroundService() {
        Log.d(TAG, "Stopping foreground service");
        Intent serviceIntent = new Intent(activity, ScreenRecorderService.class);
        activity.stopService(serviceIntent);
    }
}
