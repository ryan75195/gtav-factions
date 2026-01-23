# Project Configuration

## Deployment

GTA V installation path: `E:\SteamLibrary\steamapps\common\Grand Theft Auto V\`

Scripts folder: `E:\SteamLibrary\steamapps\common\Grand Theft Auto V\scripts\`

To deploy, copy the compiled DLL:
```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

## Debugging

Mod logs are stored in the user's Documents folder:

```
C:\Users\ryan7\Documents\FactionWars\Logs\
```

Each game session creates a new timestamped log file (e.g., `FactionWars_2026-01-22_14-30-00.log`).

When debugging in-game issues, read the most recent log file to see what happened during gameplay. The logger supports levels: INFO, DEBUG, WARN, ERROR, COMBAT, ZONE, SPAWN, and AI.
