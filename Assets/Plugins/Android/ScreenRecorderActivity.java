package com.arbadmintonnet.replay;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

/**
 * Lightweight transparent Activity that handles MediaProjection permission
 * request.
 * Launches, shows the system permission dialog, forwards the result to
 * ScreenRecorderBridge,
 * then finishes itself immediately.
 */
public class ScreenRecorderActivity extends Activity {

    private static final String TAG = "ScreenRecorderActivity";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Log.d(TAG, "Requesting screen capture permission...");
        ScreenRecorderBridge bridge = ScreenRecorderBridge.getInstance();
        if (bridge != null) {
            bridge.requestPermissionWithActivity(this);
        } else {
            Log.e(TAG, "ScreenRecorderBridge not initialized");
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        Log.d(TAG, "onActivityResult: requestCode=" + requestCode + " resultCode=" + resultCode);
        ScreenRecorderBridge bridge = ScreenRecorderBridge.getInstance();
        if (bridge != null) {
            bridge.onActivityResult(requestCode, resultCode, data);
        }

        // Done — close this transparent activity
        finish();
    }
}
