# MonoGame Terrain Generator

A MonoGame framework game that generates a terrain map with different biomes following the specified distribution:

- **Desert**: The majority of the map (~66%)
- **Grasslands**: Second most common (~18%), multiple clusters
- **Forest**: Single large forest area (~8%)
- **Water**: Multiple water spots (~8%)

## Features

- Procedural terrain generation with realistic biome clustering
- Interactive camera system with smooth movement
- Real-time minimap showing terrain distribution
- Statistics display showing exact tile counts and percentages
- Map regeneration functionality

## Controls

- **WASD** or **Arrow Keys** - Move camera around the map
- **Left Mouse Drag** - Pan the camera by dragging
- **Mouse Wheel** - Adjust camera movement speed (50-800 units/sec)
- **Right Mouse Click** - Jump camera to clicked position
- **Middle Mouse Click** - Center camera on the map
- **R** - Regenerate the map with new random terrain
- **F1** - Toggle terrain statistics display
- **F2** - Toggle minimap display
- **ESC** - Exit the game

## Terrain Types

1. **Desert** (Sandy Beige) - The base terrain covering most of the map
2. **Water** (Blue) - Multiple water bodies scattered across the map
3. **Grasslands** (Light Green) - Several clusters of fertile land
4. **Forest** (Dark Green) - One large forest area

## Technical Details

- Map size: 200x150 tiles (configurable)
- Tile size: 8x8 pixels (configurable)
- Uses flood-fill algorithm for natural-looking biome boundaries
- Implements camera culling for performance with large maps
- Real-time terrain analysis and statistics

## Building and Running

### Prerequisites
- .NET 6.0 or later
- MonoGame framework

### Running the Project
1. Open a terminal in the project directory
2. Run the following command:
```bash
dotnet run
```

### Building the Project
```bash
dotnet build
```

## Project Structure

- `TerrainGenerator.cs` - Core terrain generation logic
- `TerrainRenderer.cs` - Rendering system for terrain and UI
- `Game1.cs` - Main game loop and input handling
- `Program.cs` - Entry point

## Customization

You can modify the terrain generation parameters in `TerrainGenerator.cs`:
- Map dimensions (`MAP_WIDTH`, `MAP_HEIGHT` in Game1.cs)
- Biome percentages (in the respective generation methods)
- Tile size for different zoom levels
- Colors for each terrain type (in `TerrainRenderer.cs`)

## Performance

The game uses view culling to only render visible tiles and implements efficient flood-fill algorithms for terrain generation. The minimap provides a full overview while maintaining smooth performance.