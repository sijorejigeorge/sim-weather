using System;
using Microsoft.Xna.Framework;

namespace TerrainGame
{
    /// <summary>
    /// Handles spore production from forests and fungal mats, and their dispersal across the landscape
    /// </summary>
    public class SporeDispersalSystem
    {
        private readonly ClimateParameters parameters;
        private readonly Random random;
        private int gridWidth;
        private int gridHeight;

        public SporeDispersalSystem(ClimateParameters parameters, Random random, int gridWidth, int gridHeight)
        {
            this.parameters = parameters;
            this.random = random;
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
        }

        public void UpdateSporeDispersal(EcosystemCell[,] cellGrid, WeatherState weather, float deltaTimeDays)
        {
            // Reset all spore loads before production
            ResetSporeLoads(cellGrid);
            
            int forestCount = 0;
            int fungalMatCount = 0;
            
            // Produce and disperse spores from all sources
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    
                    if (cell?.VegetationState == VegetationState.Forest)
                    {
                        forestCount++;
                        DisperseForestSpores(cellGrid, x, y, cell, weather, deltaTimeDays);
                    }
                    else if (cell?.VegetationState == VegetationState.FungalMat)
                    {
                        fungalMatCount++;
                        DisperseFungalMatSpores(cellGrid, x, y, cell, weather, deltaTimeDays);
                    }
                }
            }
            
            // Debug output every 50th call
            if (forestCount % 50 == 0 || fungalMatCount > 0)
            {
                Console.WriteLine($"SporeSystem: Forest={forestCount}, FungalMats={fungalMatCount}");
                if (forestCount == 0)
                {
                    Console.WriteLine("WARNING: No forest cells found - all forests may have died!");
                }
            }
        }

        private void ResetSporeLoads(EcosystemCell[,] cellGrid)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    if (cell != null)
                    {
                        cell.SeedSporeLoad = 0f;
                        cell.NonSeedSporeLoad = 0f;
                    }
                }
            }
        }

        private void DisperseForestSpores(EcosystemCell[,] cellGrid, int sourceX, int sourceY, 
            EcosystemCell sourceCell, WeatherState weather, float deltaTimeDays)
        {
            // Track clean soil days for forest spore production logic
            if (sourceCell.Toxicity <= 0.001f) // Essentially 0 toxicity
            {
                sourceCell.DaysCleanSoil += deltaTimeDays; // Increment by actual time step
            }
            else
            {
                sourceCell.DaysCleanSoil = 0f; // Reset when toxicity returns
            }
            
            // Check if forest should stop producing spores (after 3 days of clean soil)
            const float cleanSoilGraceDays = 3.0f;
            if (sourceCell.DaysCleanSoil > cleanSoilGraceDays)
            {
                return; // Stop spore production - job is done!
            }
            
            // Specification: A_prod = Y_A * R * (1 + 0.7*(τ/3))
            //                B_prod = Y_B * R * (1 + 0.7*(τ/3))
            float toxicityMultiplier = 1f + 0.7f * (sourceCell.Toxicity / 3.0f);
            
            float seedSporeProduction = parameters.SporeEmissionForestSeed * sourceCell.ForestCover * toxicityMultiplier;
            float nonSeedSporeProduction = parameters.SporeEmissionForestNonseed * sourceCell.ForestCover * toxicityMultiplier;
            
            // Storm effects
            float stormReduction = 1.0f;
            if (weather.IsStormy)
            {
                stormReduction = parameters.SporeWetFactor;
                seedSporeProduction *= parameters.StormSporeMultiplier;
                nonSeedSporeProduction *= parameters.StormSporeMultiplier;
            }
            
            Vector2 windDir = weather.WindDirection;
            float windStrength = weather.WindSpeed / parameters.WindMeanMs;
            
            DisperseSeedSpores(cellGrid, sourceX, sourceY, seedSporeProduction, windDir, windStrength, stormReduction);
            DisperseNonSeedSpores(cellGrid, sourceX, sourceY, nonSeedSporeProduction, windDir, windStrength, stormReduction);
        }

        private void DisperseFungalMatSpores(EcosystemCell[,] cellGrid, int sourceX, int sourceY, 
            EcosystemCell sourceCell, WeatherState weather, float deltaTimeDays)
        {
            // Specification: B_prod_mat = Y_B_mat * F * (1 + 0.2*(θ/θ_fc))
            float theta_fc = (sourceCell.SoilProps.FieldCapacityPct / 100f) * 0.45f;
            float moistureMultiplier = 1f + 0.2f * (sourceCell.SoilMoisture / Math.Max(theta_fc, 0.01f));
            
            float nonSeedSporeProduction = parameters.SporeEmissionMatNonseed * sourceCell.FungalMatCover * moistureMultiplier;
            
            float stormReduction = 1.0f;
            if (weather.IsStormy)
            {
                stormReduction = parameters.SporeWetFactor;
                nonSeedSporeProduction *= parameters.StormSporeMultiplier * 0.6f;
            }
            
            Vector2 windDir = weather.WindDirection;
            float windStrength = weather.WindSpeed / parameters.WindMeanMs;
            
            DisperseNonSeedSpores(cellGrid, sourceX, sourceY, nonSeedSporeProduction, windDir, windStrength, stormReduction);
        }

        private void DisperseSeedSpores(EcosystemCell[,] cellGrid, int sourceX, int sourceY, 
            float sporeProduction, Vector2 windDir, float windStrength, float stormReduction)
        {
            int shortRange = Math.Max(2, (int)(parameters.SporeRangeSeedBase / 100f * stormReduction));
            
            for (int dx = -shortRange; dx <= shortRange; dx++)
            {
                for (int dy = -shortRange; dy <= shortRange; dy++)
                {
                    int targetX = sourceX + dx;
                    int targetY = sourceY + dy;
                    
                    if (targetX < 0 || targetX >= gridWidth || targetY < 0 || targetY >= gridHeight)
                        continue;
                    
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance > shortRange) continue;
                    
                    float deposition = CalculateSporeDeposition(cellGrid, sourceX, sourceY, targetX, targetY, 
                        dx, dy, distance, sporeProduction, windDir, windStrength, shortRange);
                    
                    if (deposition > 0)
                    {
                        cellGrid[targetX, targetY].SeedSporeLoad += deposition;
                    }
                }
            }
        }

        private void DisperseNonSeedSpores(EcosystemCell[,] cellGrid, int sourceX, int sourceY, 
            float sporeProduction, Vector2 windDir, float windStrength, float stormReduction)
        {
            int longRange = Math.Max(4, (int)(parameters.SporeRangeNonseedBase / 100f * stormReduction));
            
            for (int dx = -longRange; dx <= longRange; dx++)
            {
                for (int dy = -longRange; dy <= longRange; dy++)
                {
                    int targetX = sourceX + dx;
                    int targetY = sourceY + dy;
                    
                    if (targetX < 0 || targetX >= gridWidth || targetY < 0 || targetY >= gridHeight)
                        continue;
                    
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance > longRange) continue;
                    
                    float deposition = CalculateSporeDeposition(cellGrid, sourceX, sourceY, targetX, targetY, 
                        dx, dy, distance, sporeProduction, windDir, windStrength, longRange);
                    
                    if (deposition > 0)
                    {
                        cellGrid[targetX, targetY].NonSeedSporeLoad += deposition;
                        cellGrid[targetX, targetY].AirToxicity += deposition * 0.01f; // Non-seed spores pollute air
                    }
                }
            }
        }

        private float CalculateSporeDeposition(EcosystemCell[,] cellGrid, int sourceX, int sourceY, 
            int targetX, int targetY, int dx, int dy, float distance, float sporeProduction, 
            Vector2 windDir, float windStrength, int maxRange)
        {
            var targetCell = cellGrid[targetX, targetY];
            
            // Terrain modifiers
            float terrainModifier = GetTerrainModifier(targetCell, dx, dy, windDir);
            
            // Gaussian-like distribution with wind bias
            float gaussianFactor = (float)Math.Exp(-(distance * distance) / (2 * maxRange * maxRange));
            
            // Wind bias: positive dot product = same direction as wind
            Vector2 dispersalDir = new Vector2(dx, dy);
            float windBias = 0f;
            if (dispersalDir.Length() > 0)
            {
                dispersalDir = Vector2.Normalize(dispersalDir);
                windBias = Vector2.Dot(dispersalDir, windDir);
                // windBias ranges from -1 (against wind) to +1 (with wind)
                // Apply wind bias with proper scaling to avoid negative deposition
                // Use exponential to ensure positive values: e^(windBias * windStrength * scale)
                float windScale = 0.3f; // Moderate wind effect
                gaussianFactor *= (float)Math.Exp(windBias * windStrength * windScale / parameters.WindMeanMs);
            }
            
            // Decay with distance
            float decayFactor = (float)Math.Exp(-parameters.SporeSurvivalDecay * distance);
            
            return sporeProduction * gaussianFactor * decayFactor * terrainModifier;
        }

        private float GetTerrainModifier(EcosystemCell targetCell, int dx, int dy, Vector2 windDir)
        {
            float modifier = 1f;
            
            if (targetCell.BaseTerrainType == TerrainType.Plateau)
            {
                modifier *= 0.25f; // Plateaus strongly resist spore deposition (updrafts, elevation)
            }
            else if (targetCell.BaseTerrainType == TerrainType.Canyon)
            {
                Vector2 dispersalDir = new Vector2(dx, dy);
                if (dispersalDir.Length() > 0)
                {
                    dispersalDir = Vector2.Normalize(dispersalDir);
                    float windAlignment = Vector2.Dot(dispersalDir, windDir);
                    if (windAlignment > 0.7f)
                        modifier *= 1.3f; // Canyons channel spores
                }
            }
            
            return modifier;
        }
    }
}
