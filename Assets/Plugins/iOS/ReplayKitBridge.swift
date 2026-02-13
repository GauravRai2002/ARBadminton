import Foundation
import ReplayKit

/// Bridge between Unity and iOS ReplayKit for clip buffering.
/// Provides C-callable functions for starting/stopping clip buffering and exporting clips.
@objc public class ReplayKitBridge: NSObject {
    
    static let shared = ReplayKitBridge()
    
    private var isBuffering = false
    private var exportedClipPath: String = ""
    private var exportStatus: Int32 = 0  // 0=idle, 1=exporting, 2=success, 3=error
    private var errorMessage: String = ""
    
    /// Start continuous clip buffering
    @objc public func startClipBuffering() {
        guard #available(iOS 15.4, *) else {
            errorMessage = "Clip buffering requires iOS 15.4+"
            NSLog("[ReplayKit] %@", errorMessage)
            return
        }
        
        guard !isBuffering else {
            NSLog("[ReplayKit] Already buffering")
            return
        }
        
        let recorder = RPScreenRecorder.shared()
        guard recorder.isAvailable else {
            errorMessage = "Screen recording is not available"
            NSLog("[ReplayKit] %@", errorMessage)
            return
        }
        
        recorder.startClipBuffering { [weak self] error in
            if let error = error {
                self?.errorMessage = error.localizedDescription
                NSLog("[ReplayKit] Failed to start clip buffering: %@", error.localizedDescription)
            } else {
                self?.isBuffering = true
                NSLog("[ReplayKit] Clip buffering started successfully")
            }
        }
    }
    
    /// Stop clip buffering
    @objc public func stopClipBuffering() {
        guard #available(iOS 15.4, *) else { return }
        guard isBuffering else { return }
        
        let recorder = RPScreenRecorder.shared()
        recorder.stopClipBuffering { [weak self] error in
            if let error = error {
                NSLog("[ReplayKit] Failed to stop clip buffering: %@", error.localizedDescription)
            } else {
                self?.isBuffering = false
                NSLog("[ReplayKit] Clip buffering stopped")
            }
        }
    }
    
    /// Export the last N seconds as a video clip
    @objc public func exportClip(duration: TimeInterval) {
        guard #available(iOS 15.4, *) else {
            exportStatus = 3
            errorMessage = "Clip buffering requires iOS 15.4+"
            return
        }
        
        guard isBuffering else {
            exportStatus = 3
            errorMessage = "Clip buffering is not active"
            NSLog("[ReplayKit] Cannot export: buffering not active")
            return
        }
        
        exportStatus = 1  // exporting
        
        let tempDir = NSTemporaryDirectory()
        let timestamp = Int(Date().timeIntervalSince1970)
        let filePath = "\(tempDir)replay_\(timestamp).mp4"
        let fileURL = URL(fileURLWithPath: filePath)
        
        // Remove existing file if any
        try? FileManager.default.removeItem(at: fileURL)
        
        let recorder = RPScreenRecorder.shared()
        recorder.exportClip(to: fileURL, duration: duration) { [weak self] error in
            DispatchQueue.main.async {
                if let error = error {
                    self?.exportStatus = 3
                    self?.errorMessage = error.localizedDescription
                    NSLog("[ReplayKit] Export failed: %@", error.localizedDescription)
                } else {
                    self?.exportStatus = 2
                    self?.exportedClipPath = filePath
                    NSLog("[ReplayKit] Export successful: %@", filePath)
                }
            }
        }
    }
    
    @objc public func getExportedClipPath() -> String {
        return exportedClipPath
    }
    
    @objc public func getExportStatus() -> Int32 {
        return exportStatus
    }
    
    @objc public func getErrorMessage() -> String {
        return errorMessage
    }
    
    @objc public func getIsBuffering() -> Bool {
        return isBuffering
    }
    
    @objc public func resetExportStatus() {
        exportStatus = 0
        errorMessage = ""
    }
}

// MARK: - C-callable functions for Unity

@_cdecl("ReplayKit_StartClipBuffering")
public func ReplayKit_StartClipBuffering() {
    ReplayKitBridge.shared.startClipBuffering()
}

@_cdecl("ReplayKit_StopClipBuffering")
public func ReplayKit_StopClipBuffering() {
    ReplayKitBridge.shared.stopClipBuffering()
}

@_cdecl("ReplayKit_ExportClip")
public func ReplayKit_ExportClip(_ duration: Float) {
    ReplayKitBridge.shared.exportClip(duration: TimeInterval(duration))
}

@_cdecl("ReplayKit_GetExportedClipPath")
public func ReplayKit_GetExportedClipPath() -> UnsafePointer<CChar>? {
    let path = ReplayKitBridge.shared.getExportedClipPath()
    // Return a C string that Unity can read
    return (path as NSString).utf8String
}

@_cdecl("ReplayKit_GetExportStatus")
public func ReplayKit_GetExportStatus() -> Int32 {
    return ReplayKitBridge.shared.getExportStatus()
}

@_cdecl("ReplayKit_GetErrorMessage")
public func ReplayKit_GetErrorMessage() -> UnsafePointer<CChar>? {
    let msg = ReplayKitBridge.shared.getErrorMessage()
    return (msg as NSString).utf8String
}

@_cdecl("ReplayKit_GetIsBuffering")
public func ReplayKit_GetIsBuffering() -> Bool {
    return ReplayKitBridge.shared.getIsBuffering()
}

@_cdecl("ReplayKit_ResetExportStatus")
public func ReplayKit_ResetExportStatus() {
    ReplayKitBridge.shared.resetExportStatus()
}
