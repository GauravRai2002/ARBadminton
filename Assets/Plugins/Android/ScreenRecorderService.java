package com.arbadmintonnet.replay;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Intent;
import android.content.pm.ServiceInfo;
import android.os.Build;
import android.os.IBinder;
import android.util.Log;

/**
 * Foreground service required by Android 10+ for MediaProjection screen
 * recording.
 * Must be started BEFORE calling MediaProjectionManager.getMediaProjection().
 */
public class ScreenRecorderService extends Service {

    private static final String TAG = "ScreenRecorderService";
    private static final String CHANNEL_ID = "screen_recorder_channel";
    private static final int NOTIFICATION_ID = 9002;

    @Override
    public void onCreate() {
        super.onCreate();
        createNotificationChannel();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        Log.d(TAG, "Starting foreground service for screen recording");

        Notification notification = new Notification.Builder(this, CHANNEL_ID)
                .setContentTitle("AR Badminton Net")
                .setContentText("Recording screen for replay...")
                .setSmallIcon(android.R.drawable.ic_media_play)
                .setOngoing(true)
                .build();

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(NOTIFICATION_ID, notification,
                    ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION);
        } else {
            startForeground(NOTIFICATION_ID, notification);
        }

        return START_NOT_STICKY;
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    @Override
    public void onDestroy() {
        Log.d(TAG, "Foreground service stopped");
        super.onDestroy();
    }

    private void createNotificationChannel() {
        NotificationChannel channel = new NotificationChannel(
                CHANNEL_ID,
                "Screen Recording",
                NotificationManager.IMPORTANCE_LOW);
        channel.setDescription("Shows while screen is being recorded for replay");
        NotificationManager manager = getSystemService(NotificationManager.class);
        if (manager != null) {
            manager.createNotificationChannel(channel);
        }
    }
}
