using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TerrainGame
{
    public enum TerrainType
    {
        Desert,
        Water,
        Grassland,
        Forest,
        Plateau,
        Canyon,
        Valley
    }

    public class TerrainTile
    {
        public TerrainType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsLowToxicityZone { get; set; } // For 3-7 special low-toxicity patches

        public TerrainTile(int x, int y, TerrainType type)
        {
            X = x;
            Y = y;
            Type = type;
            IsLowToxicityZone = false;
        }
    }

    public class TerrainGenerator
    {
        private Random random;
        private int mapWidth;
        private int mapHeight;
        private TerrainTile[,] terrainMap;
        private List<(int x, int y)> forestPositions;

        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;

        public TerrainGenerator(int width, int height, int seed = 0)
        {
            mapWidth = width;
            mapHeight = height;
            random = seed == 0 ? new Random() : new Random(seed);
            terrainMap = new TerrainTile[width, height];
            forestPositions = new List<(int x, int y)>();
        }

        public TerrainTile[,] GenerateMap()
        {
            // Initialize entire map as desert
            InitializeAsDesert();

            // Add one forest spot (5-10% of map)
            GenerateForest();

            // Add grasslands (15-20% of map)
            GenerateGrasslands();

            // Add water spots (5-10% of map)
            GenerateWater();

            // Add plateaus randomly across the map (8-12% of map)
            GeneratePlateaus();

            // Generate 3-7 low-toxicity patches (0-1 toxicity) where grasslands can thrive
            GenerateLowToxicityPatches();

            return terrainMap;
        }

        private void InitializeAsDesert()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    terrainMap[x, y] = new TerrainTile(x, y, TerrainType.Desert);
                }
            }
        }

        private void GenerateForest()
        {
            // Ensure forest is at least 5-10 pixels (minimum 25-100 tiles)
            int minForestSize = 25; // 5x5 pixels minimum
            int maxForestSize = (int)(mapWidth * mapHeight * 0.12f); // Up to 12% of map
            int forestSize = Math.Max(minForestSize, (int)(mapWidth * mapHeight * 0.08f)); // Default 8%
            forestSize = Math.Min(forestSize, maxForestSize);

            // Try multiple times to place forest if needed
            int attempts = 0;
            int maxAttempts = 10;
            bool forestPlaced = false;
            var forestPositions = new List<(int x, int y)>();

            while (!forestPlaced && attempts < maxAttempts)
            {
                attempts++;
                forestPositions.Clear();
                
                int centerX = random.Next(mapWidth / 4, 3 * mapWidth / 4);
                int centerY = random.Next(mapHeight / 4, 3 * mapHeight / 4);

                var forestTiles = new Queue<(int x, int y)>();
                var visited = new HashSet<(int, int)>();

                forestTiles.Enqueue((centerX, centerY));
                visited.Add((centerX, centerY));

                int tilesPlaced = 0;
                while (forestTiles.Count > 0 && tilesPlaced < forestSize)
                {
                    var (x, y) = forestTiles.Dequeue();

                    if (IsValidPosition(x, y))
                    {
                        terrainMap[x, y].Type = TerrainType.Forest;
                        forestPositions.Add((x, y));
                        tilesPlaced++;

                        // Add neighboring tiles with more irregular expansion using noise
                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                // Create irregular shapes using Perlin-like noise and random factors
                                double distance = Math.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
                                double maxDistance = Math.Sqrt(forestSize) * 0.7;
                                
                                // Add noise to create irregular boundaries
                                double noiseValue = SimplexNoise(nx * 0.1, ny * 0.1) * 0.5 + 0.5;
                                double randomFactor = random.NextDouble() * 0.4 + 0.3; // 0.3 to 0.7
                                
                                double baseProbability = Math.Max(0.05, 0.85 - (distance / maxDistance));
                                double finalProbability = baseProbability * noiseValue * randomFactor;

                                if (random.NextDouble() < finalProbability)
                                {
                                    forestTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                }

                // Check if we placed enough forest tiles
                if (tilesPlaced >= minForestSize)
                {
                    forestPlaced = true;
                }
                else
                {
                    // Revert placed tiles and try again
                    foreach (var (fx, fy) in forestPositions)
                    {
                        if (IsValidPosition(fx, fy))
                            terrainMap[fx, fy].Type = TerrainType.Desert;
                    }
                }
            }

            // If we still couldn't place a proper forest, force place a minimum viable one
            if (!forestPlaced)
            {
                ForceMinimumForest();
                // Recalculate forest positions
                forestPositions.Clear();
                for (int x = 0; x < mapWidth; x++)
                {
                    for (int y = 0; y < mapHeight; y++)
                    {
                        if (terrainMap[x, y].Type == TerrainType.Forest)
                        {
                            forestPositions.Add((x, y));
                        }
                    }
                }
            }

            // Store forest positions for later use by grasslands and water
            this.forestPositions = forestPositions;
        }

        private void GenerateGrasslands()
        {
            int grasslandCount = (int)(mapWidth * mapHeight * 0.18f); // ~18% of map
            int clustersCount = random.Next(3, 6); // 3-5 grassland clusters

            for (int cluster = 0; cluster < clustersCount; cluster++)
            {
                int clusterSize = grasslandCount / clustersCount;
                
                // Find a position near the forest
                (int centerX, int centerY) = FindPositionNearForest(15, 35);

                var grassTiles = new Queue<(int x, int y)>();
                var visited = new HashSet<(int, int)>();

                grassTiles.Enqueue((centerX, centerY));
                visited.Add((centerX, centerY));

                int tilesPlaced = 0;
                while (grassTiles.Count > 0 && tilesPlaced < clusterSize)
                {
                    var (x, y) = grassTiles.Dequeue();

                    if (IsValidPosition(x, y) && terrainMap[x, y].Type == TerrainType.Desert)
                    {
                        terrainMap[x, y].Type = TerrainType.Grassland;
                        tilesPlaced++;

                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                // Create irregular shapes with noise
                                double noiseValue = SimplexNoise(nx * 0.08, ny * 0.08) * 0.5 + 0.5;
                                double randomFactor = random.NextDouble() * 0.5 + 0.25;
                                
                                // Distance from cluster center for natural falloff
                                double distance = Math.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
                                double maxDistance = Math.Sqrt(clusterSize) * 0.8;
                                double distanceFactor = Math.Max(0.1, 1.0 - (distance / maxDistance));
                                
                                double finalProbability = 0.6 * noiseValue * randomFactor * distanceFactor;
                                
                                if (random.NextDouble() < finalProbability)
                                {
                                    grassTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateWater()
        {
            int waterCount = (int)(mapWidth * mapHeight * 0.08f); // ~8% of map
            int waterSpotsCount = random.Next(4, 8); // 4-7 water spots
            int minWaterSize = 8; // Minimum 8 pixels for at least one water spot

            // First, create one guaranteed larger water spot
            CreateLargeWaterSpot(minWaterSize);

            // Then create the remaining smaller water spots
            for (int spot = 1; spot < waterSpotsCount; spot++)
            {
                int remainingWaterCount = waterCount - minWaterSize;
                int spotSize = remainingWaterCount / (waterSpotsCount - 1);
                
                // Find a position near the forest but prefer edges of grasslands
                (int centerX, int centerY) = FindPositionNearForest(10, 30);

                var waterTiles = new Queue<(int x, int y)>();
                var visited = new HashSet<(int, int)>();

                waterTiles.Enqueue((centerX, centerY));
                visited.Add((centerX, centerY));

                int tilesPlaced = 0;
                while (waterTiles.Count > 0 && tilesPlaced < spotSize)
                {
                    var (x, y) = waterTiles.Dequeue();

                    if (IsValidPosition(x, y) && (terrainMap[x, y].Type == TerrainType.Desert || terrainMap[x, y].Type == TerrainType.Grassland))
                    {
                        terrainMap[x, y].Type = TerrainType.Water;
                        tilesPlaced++;

                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                // Create very irregular water shapes with multiple noise layers
                                double noise1 = SimplexNoise(nx * 0.12, ny * 0.12);
                                double noise2 = SimplexNoise(nx * 0.05, ny * 0.05) * 0.5;
                                double combinedNoise = (noise1 + noise2) * 0.5 + 0.5;
                                
                                double randomFactor = random.NextDouble() * 0.6 + 0.2;
                                
                                // Distance from water center for natural falloff
                                double distance = Math.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
                                double maxDistance = Math.Sqrt(spotSize) * 1.2;
                                double distanceFactor = Math.Max(0.05, 1.0 - (distance / maxDistance));
                                
                                // Prefer staying in grasslands if available
                                double terrainBonus = 1.0;
                                if (IsValidPosition(nx, ny) && terrainMap[nx, ny].Type == TerrainType.Grassland)
                                    terrainBonus = 1.3;
                                
                                double finalProbability = 0.7 * combinedNoise * randomFactor * distanceFactor * terrainBonus;
                                
                                if (random.NextDouble() < finalProbability)
                                {
                                    waterTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CreateLargeWaterSpot(int minSize)
        {
            // Find a good position for the large water spot
            (int centerX, int centerY) = FindPositionNearForest(8, 25);

            var waterTiles = new Queue<(int x, int y)>();
            var visited = new HashSet<(int, int)>();

            waterTiles.Enqueue((centerX, centerY));
            visited.Add((centerX, centerY));

            int tilesPlaced = 0;
            while (waterTiles.Count > 0 && tilesPlaced < minSize * 2) // Allow up to double the minimum
            {
                var (x, y) = waterTiles.Dequeue();

                if (IsValidPosition(x, y) && (terrainMap[x, y].Type == TerrainType.Desert || terrainMap[x, y].Type == TerrainType.Grassland))
                {
                    terrainMap[x, y].Type = TerrainType.Water;
                    tilesPlaced++;

                    // Continue expanding until we reach minimum size
                    if (tilesPlaced < minSize)
                    {
                        // High probability to continue growing when under minimum size
                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                // High probability for guaranteed growth
                                if (random.NextDouble() < 0.85)
                                {
                                    waterTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                    else
                    {
                        // Once minimum size is reached, use normal irregular growth
                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                double noise = SimplexNoise(nx * 0.1, ny * 0.1) * 0.5 + 0.5;
                                double distance = Math.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
                                double maxDistance = Math.Sqrt(minSize * 2) * 1.5;
                                double distanceFactor = Math.Max(0.1, 1.0 - (distance / maxDistance));
                                
                                if (random.NextDouble() < 0.6 * noise * distanceFactor)
                                {
                                    waterTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GeneratePlateaus()
        {
            int plateauCount = (int)(mapWidth * mapHeight * 0.10f); // ~10% of map
            int plateauClusters = random.Next(3, 6); // 3-5 plateau clusters

            for (int cluster = 0; cluster < plateauClusters; cluster++)
            {
                int clusterSize = plateauCount / plateauClusters;
                int centerX = random.Next(mapWidth / 4, 3 * mapWidth / 4);
                int centerY = random.Next(mapHeight / 4, 3 * mapHeight / 4);

                // Avoid placing too close to forest
                if (IsNearForest(centerX, centerY, 8))
                    continue;

                var plateauTiles = new Queue<(int x, int y)>();
                var visited = new HashSet<(int, int)>();

                plateauTiles.Enqueue((centerX, centerY));
                visited.Add((centerX, centerY));

                int tilesPlaced = 0;
                while (plateauTiles.Count > 0 && tilesPlaced < clusterSize)
                {
                    var (x, y) = plateauTiles.Dequeue();

                    if (IsValidPosition(x, y) && terrainMap[x, y].Type == TerrainType.Desert)
                    {
                        terrainMap[x, y].Type = TerrainType.Plateau;
                        tilesPlaced++;

                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                // Create rounded plateau shapes
                                double distance = Math.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
                                double maxDistance = Math.Sqrt(clusterSize) * 0.8;
                                double probability = Math.Max(0.1, 0.85 - (distance / maxDistance));
                                
                                // Add some noise for natural edges
                                double noise = SimplexNoise(nx * 0.08, ny * 0.08) * 0.3 + 0.7;
                                probability *= noise;

                                if (random.NextDouble() < probability)
                                {
                                    plateauTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                }

                // Add some longer canyons within the plateau
                GenerateCanyonsInPlateau(centerX, centerY, clusterSize / 3); // Increased from clusterSize / 8
            }
        }

        private void GenerateCanyonsInPlateau(int plateauCenterX, int plateauCenterY, int canyonLength)
        {
            // Find plateau tiles around the center
            var plateauTiles = new List<(int x, int y)>();
            int searchRadius = 20; // Increased search radius for longer canyons

            for (int x = plateauCenterX - searchRadius; x <= plateauCenterX + searchRadius; x++)
            {
                for (int y = plateauCenterY - searchRadius; y <= plateauCenterY + searchRadius; y++)
                {
                    if (IsValidPosition(x, y) && terrainMap[x, y].Type == TerrainType.Plateau)
                    {
                        plateauTiles.Add((x, y));
                    }
                }
            }

            if (plateauTiles.Count < 10) return; // Not enough plateau to cut through

            // Create 2-4 longer canyons
            int canyonCount = random.Next(2, 5); // Increased canyon count
            for (int i = 0; i < canyonCount; i++)
            {
                if (plateauTiles.Count == 0) break;

                var startTile = plateauTiles[random.Next(plateauTiles.Count)];
                CreateLongerCanyon(startTile.x, startTile.y, canyonLength * 2); // Double the length
            }
        }

        private void CreateLongerCanyon(int startX, int startY, int maxLength)
        {
            int currentX = startX;
            int currentY = startY;
            int length = 0;
            
            // Start with a random direction
            Vector2 direction = new Vector2(random.Next(-1, 2), random.Next(-1, 2));
            if (direction.Length() == 0)
                direction = new Vector2(1, 0); // Default direction if zero
            
            direction.Normalize();
            
            // Track previous direction for momentum
            Vector2 momentum = direction;

            while (length < maxLength)
            {
                if (IsValidPosition(currentX, currentY) && terrainMap[currentX, currentY].Type == TerrainType.Plateau)
                {
                    // Create wider canyon by affecting neighboring tiles too
                    terrainMap[currentX, currentY].Type = TerrainType.Canyon;
                    
                    // Occasionally make the canyon wider
                    if (random.NextDouble() < 0.3) // 30% chance to widen
                    {
                        var neighbors = GetNeighbors(currentX, currentY);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (IsValidPosition(nx, ny) && terrainMap[nx, ny].Type == TerrainType.Plateau && random.NextDouble() < 0.4)
                            {
                                terrainMap[nx, ny].Type = TerrainType.Canyon;
                            }
                        }
                    }
                    
                    length++;

                    // Calculate next direction with some persistence and randomness
                    Vector2 randomDirection = new Vector2(random.Next(-1, 2), random.Next(-1, 2));
                    if (randomDirection.Length() > 0)
                        randomDirection.Normalize();
                    
                    // Blend previous momentum with new random direction
                    Vector2 newDirection = momentum * 0.7f + randomDirection * 0.3f;
                    if (newDirection.Length() > 0)
                    {
                        newDirection.Normalize();
                        momentum = newDirection;
                        
                        // Move in the blended direction
                        currentX += (int)Math.Round(newDirection.X);
                        currentY += (int)Math.Round(newDirection.Y);
                    }
                    else
                    {
                        // Fallback to random movement
                        currentX += random.Next(-1, 2);
                        currentY += random.Next(-1, 2);
                    }
                    
                    // Add some branching - occasionally create a fork
                    if (length > maxLength / 3 && random.NextDouble() < 0.15) // 15% chance to branch
                    {
                        CreateCanyonBranch(currentX, currentY, maxLength / 4);
                    }
                }
                else
                {
                    break; // Hit edge of plateau or invalid position
                }
            }
        }

        private void CreateCanyonBranch(int startX, int startY, int branchLength)
        {
            int currentX = startX;
            int currentY = startY;
            int length = 0;
            
            // Pick a random direction for the branch
            Vector2 branchDirection = new Vector2(random.Next(-1, 2), random.Next(-1, 2));
            if (branchDirection.Length() == 0)
                branchDirection = new Vector2(1, 0);
            branchDirection.Normalize();

            while (length < branchLength)
            {
                if (IsValidPosition(currentX, currentY) && terrainMap[currentX, currentY].Type == TerrainType.Plateau)
                {
                    terrainMap[currentX, currentY].Type = TerrainType.Canyon;
                    length++;

                    // Move in branch direction with some randomness
                    currentX += (int)Math.Round(branchDirection.X + (random.NextDouble() - 0.5) * 0.6);
                    currentY += (int)Math.Round(branchDirection.Y + (random.NextDouble() - 0.5) * 0.6);
                }
                else
                {
                    break;
                }
            }
        }

        private List<(int x, int y)> GetNeighbors(int x, int y)
        {
            var neighbors = new List<(int x, int y)>();
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    neighbors.Add((x + dx, y + dy));
                }
            }
            
            return neighbors;
        }

        private void GenerateLowToxicityPatches()
        {
            // Generate 3-7 distinct low-toxicity patches (0-1 toxicity)
            // These can spawn ANYWHERE on the map (not restricted to near forests)
            int patchCount = random.Next(3, 8); // 3-7 patches
            
            for (int i = 0; i < patchCount; i++)
            {
                // Small patches: 2-3 pixels (4-9 tiles total)
                int patchSize = random.Next(4, 10);
                
                // Random position anywhere on map, only avoiding water and existing forests
                int attempts = 0;
                int centerX, centerY;
                do
                {
                    centerX = random.Next(5, mapWidth - 5);
                    centerY = random.Next(5, mapHeight - 5);
                    attempts++;
                } while ((terrainMap[centerX, centerY].Type == TerrainType.Water ||
                          terrainMap[centerX, centerY].Type == TerrainType.Forest) && 
                         attempts < 20);
                
                if (attempts >= 20) continue; // Skip if can't find good spot
                
                // Create small cluster
                var patchTiles = new Queue<(int x, int y)>();
                var visited = new HashSet<(int, int)>();
                
                patchTiles.Enqueue((centerX, centerY));
                visited.Add((centerX, centerY));
                
                int tilesPlaced = 0;
                while (patchTiles.Count > 0 && tilesPlaced < patchSize)
                {
                    var (x, y) = patchTiles.Dequeue();
                    
                    if (IsValidPosition(x, y))
                    {
                        // Mark as low-toxicity zone (keep original terrain type unless converting to grassland)
                        terrainMap[x, y].IsLowToxicityZone = true;
                        tilesPlaced++;
                        
                        // Low-toxicity patches DON'T automatically become grassland
                        // They are just desert with low toxicity where grass CAN grow
                        // Only mark a few as grassland (these represent established grass areas)
                        if (i < patchCount * 0.3 && terrainMap[x, y].Type == TerrainType.Desert)
                        {
                            terrainMap[x, y].Type = TerrainType.Grassland;
                        }
                        
                        // Expand to neighbors with high probability for compact patches
                        var neighbors = GetNeighbors(x, y);
                        foreach (var (nx, ny) in neighbors)
                        {
                            if (!visited.Contains((nx, ny)) && IsValidPosition(nx, ny))
                            {
                                // Tight clustering - high probability
                                if (random.NextDouble() < 0.7)
                                {
                                    patchTiles.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
        }

        private bool IsNearForest(int x, int y, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;
                    
                    if (IsValidPosition(checkX, checkY) && 
                        terrainMap[checkX, checkY].Type == TerrainType.Forest)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public TerrainTile GetTile(int x, int y)
        {
            if (IsValidPosition(x, y))
                return terrainMap[x, y];
            return null;
        }

        public Dictionary<TerrainType, int> GetTerrainStats()
        {
            var stats = new Dictionary<TerrainType, int>
            {
                { TerrainType.Desert, 0 },
                { TerrainType.Water, 0 },
                { TerrainType.Grassland, 0 },
                { TerrainType.Forest, 0 },
                { TerrainType.Plateau, 0 },
                { TerrainType.Canyon, 0 }
            };

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    stats[terrainMap[x, y].Type]++;
                }
            }

            return stats;
        }

        private void ForceMinimumForest()
        {
            // Place a guaranteed minimum 6x6 forest in the center of the map
            int centerX = mapWidth / 2;
            int centerY = mapHeight / 2;
            int radius = 3; // Creates roughly a 6x6 area

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (IsValidPosition(x, y))
                    {
                        // Create a roughly circular forest
                        double distance = Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                        if (distance <= radius)
                        {
                            terrainMap[x, y].Type = TerrainType.Forest;
                        }
                    }
                }
            }
        }

        // Simple Simplex-like noise function for creating irregular shapes
        private double SimplexNoise(double x, double y)
        {
            // Simple 2D noise function - this is a simplified version
            int i = (int)Math.Floor(x);
            int j = (int)Math.Floor(y);
            
            double fx = x - i;
            double fy = y - j;
            
            // Get pseudo-random gradients at grid points
            double g00 = PseudoRandom(i, j);
            double g10 = PseudoRandom(i + 1, j);
            double g01 = PseudoRandom(i, j + 1);
            double g11 = PseudoRandom(i + 1, j + 1);
            
            // Interpolate
            double nx0 = Lerp(g00, g10, SmoothStep(fx));
            double nx1 = Lerp(g01, g11, SmoothStep(fx));
            
            return Lerp(nx0, nx1, SmoothStep(fy));
        }

        private double PseudoRandom(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0);
        }

        private double Lerp(double a, double b, double t)
        {
            return a + t * (b - a);
        }

        private double SmoothStep(double t)
        {
            return t * t * (3.0 - 2.0 * t);
        }

        private (int x, int y) FindPositionNearForest(int minDistance, int maxDistance)
        {
            if (forestPositions.Count == 0)
            {
                // Fallback if no forest positions available
                return (random.Next(0, mapWidth), random.Next(0, mapHeight));
            }

            int attempts = 0;
            int maxAttempts = 50;
            
            while (attempts < maxAttempts)
            {
                // Pick a random forest tile as reference
                var forestTile = forestPositions[random.Next(forestPositions.Count)];
                
                // Generate a position at specified distance from this forest tile
                double angle = random.NextDouble() * Math.PI * 2;
                double distance = random.NextDouble() * (maxDistance - minDistance) + minDistance;
                
                int x = (int)(forestTile.x + Math.Cos(angle) * distance);
                int y = (int)(forestTile.y + Math.Sin(angle) * distance);
                
                // Ensure the position is within map bounds
                if (IsValidPosition(x, y))
                {
                    return (x, y);
                }
                
                attempts++;
            }
            
            // Fallback to random position if we can't find a good spot
            return (random.Next(0, mapWidth), random.Next(0, mapHeight));
        }
    }
}