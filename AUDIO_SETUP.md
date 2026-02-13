# Audio Setup Instructions

Since audio files are not yet in the project, you have two options:

## Option 1: Import Your Own Audio Files

If you have audio files:
1. Place them in `Assets/Audio/` folder
2. Unity will automatically import them
3. Assign in **AudioManager** component on GameManager

**Required files:**
- `net_hit.wav` - Sound when shuttle hits net (0.5-1 second impact sound)
- `placement_confirm.wav` - Sound when net is successfully placed (0.5-1 second success/chime)

## Option 2: Download Free Audio from Unity Asset Store

1. Open Unity Asset Store (Window → Asset Store)
2. Search for "free sound effects"
3. Import a free sound pack
4. Assign appropriate sounds to AudioManager

**Recommended packs (free)**:
- "Free Sound Effects Pack" by Olivier Girardot
- "Free Casual Game SFX Pack" by Dustyroom

## Option 3: Use Temporary Placeholder

For initial testing, you can:
1. Leave audio clips unassigned
2. The game will work but without sound
3. Console will show warnings: "Net hit sound not assigned"
4. Add real audio later

## Audio Specifications

When selecting/creating audio:
- **Format**: WAV or MP3
- **Sample Rate**: 44.1 kHz
- **Length**: 0.5-1 seconds
- **Type**: Short impact/hit sounds work best

## Assigning Audio in Unity

Once you have audio files:
1. Drag/drop audio files into `Assets/Audio/`
2. Select **GameManager** in Hierarchy
3. Find **Audio Manager** component
4. Drag audio clips to:
   - **Net Hit Sound** field
   - **Placement Confirm Sound** field

---

**Note**: The application will function without audio for testing purposes. Audio can be added at any time without code changes.
