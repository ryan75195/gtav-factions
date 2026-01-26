# Shop Menu Design

## Overview

Add a Shop section to the main menu for purchasing military vehicles. Vehicles spawn nearby and are marked on the map with yellow blips.

## Vehicle Catalog

| Category | Vehicle | Model Name | Price | Description |
|----------|---------|------------|-------|-------------|
| Transport | Insurgent | `insurgent` | $25,000 | Armored SUV, seats 9 |
| Light Combat | Technical | `technical` | $15,000 | Pickup with mounted gun |
| Heavy Combat | APC | `apc` | $50,000 | Armored carrier, amphibious |
| Tank | Khanjali | `khanjali` | $100,000 | Main battle tank |
| Air | Buzzard | `buzzard` | $75,000 | Attack helicopter |
| Speed | Bati 801 | `bati` | $10,000 | Fast sport motorcycle |
| Speed | Zentorno | `zentorno` | $40,000 | Supercar |

## Menu Structure

```
Main Menu
├── Zone Management
├── Recruitment
├── Shop (NEW)
│   ├── Cash: $XX,XXX (display only)
│   ├── Insurgent ($25,000) - Armored SUV
│   ├── Technical ($15,000) - Mounted gun pickup
│   ├── APC ($50,000) - Armored carrier
│   ├── Khanjali ($100,000) - Battle tank
│   ├── Buzzard ($75,000) - Attack helicopter
│   ├── Bati 801 ($10,000) - Sport motorcycle
│   ├── Zentorno ($40,000) - Supercar
│   └── Back
└── Settings
```

## Spawn Behavior

- **Ground vehicles**: Spawn on nearest road to player
- **Helicopters**: Spawn on nearest flat area (uses GET_CLOSEST_VEHICLE_NODE)
- **Motorcycles**: Spawn next to player on sidewalk/road

## Map Blips

- Color: Yellow (BlipColor.Yellow)
- Blip disappears when vehicle is destroyed or player moves far away
- Vehicle-appropriate sprite icons

## Purchase Flow

1. Player selects vehicle in Shop menu
2. Check if player has enough money
3. Deduct money from player
4. Find appropriate spawn location near player
5. Spawn vehicle at location
6. Create yellow blip attached to vehicle
7. Show notification: "~g~[Vehicle] delivered! Check your map."

## Implementation Components

1. **ShopMenuController** - New UI controller for shop menu
2. **IVehicleSpawningService** - Interface for vehicle spawning
3. **VehicleSpawningService** - Implementation using GameBridge
4. **ShopItem** - Model for shop items (name, model, price, type)
5. **GameBridge additions** - CreateVehicle, CreateBlipForVehicle methods
6. **MainMenuController update** - Add Shop menu item
7. **GameLoopController update** - Wire up ShopMenuController
