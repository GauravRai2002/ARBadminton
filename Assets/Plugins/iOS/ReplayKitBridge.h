#ifndef ReplayKitBridge_Bridging_Header_h
#define ReplayKitBridge_Bridging_Header_h

// C function declarations for Unity DllImport
void ReplayKit_StartClipBuffering(void);
void ReplayKit_StopClipBuffering(void);
void ReplayKit_ExportClip(float duration);
const char* ReplayKit_GetExportedClipPath(void);
int ReplayKit_GetExportStatus(void);
const char* ReplayKit_GetErrorMessage(void);
bool ReplayKit_GetIsBuffering(void);
void ReplayKit_ResetExportStatus(void);

#endif
