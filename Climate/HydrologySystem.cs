using System;
using Microsoft.Xna.Framework;

namespace TerrainGame
{
    /// <summary>
    /// Handles soil moisture dynamics including precipitation, infiltration, evaporation, and percolation
    /// </summary>
    public class HydrologySystem
    {
        private readonly ClimateParameters parameters;
        private int gridWidth;
        private int gridHeight;

        public HydrologySystem(ClimateParameters parameters, int gridWidth, int gridHeight)
        {
            this.parameters = parameters;
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
        }

        public void UpdateSoilMoisture(EcosystemCell cell, EcosystemCell[,] cellGrid, int x, int y, 
            WeatherState weather, float deltaTimeDays)
        {
            if (cell.BaseTerrainType == TerrainType.Water) return;
            
            // Add groundwater seepage from nearby water bodies
            AddGroundwaterSeepage(cell, cellGrid, x, y, deltaTimeDays);
            
            // Add precipitation
            if (weather.Precipitation > 0)
            {
                AddPrecipitation(cell, cellGrid, x, y, weather, deltaTimeDays);
            }
            else
            {
                cell.DaysSinceLastRain += deltaTimeDays;
                cell.DaysSinceLastStorm += deltaTimeDays;
            }
            
            // Evaporation
            ApplyEvaporation(cell, weather, deltaTimeDays);
            
            // Add fog moisture
            if (weather.IsFoggy)
            {
                cell.SoilMoisture += 0.001f * deltaTimeDays;
            }
            
            // Clamp to field capacity
            float fieldCapacity = (cell.SoilProps.FieldCapacityPct / 100f) * 0.45f;
            cell.SoilMoisture = Math.Clamp(cell.SoilMoisture, 0f, fieldCapacity);
        }

        private void AddPrecipitation(EcosystemCell cell, EcosystemCell[,] cellGrid, int x, int y, 
            WeatherState weather, float deltaTimeDays)
        {
            float basePrecipitation = weather.Precipitation;
            
            // Calculate wind vector
            Vector2 windVec = weather.WindDirection * weather.WindSpeed;
            float windMag = (float)Math.Sqrt(windVec.X * windVec.X + windVec.Y * windVec.Y);
            
            // Orographic effects (plateaus)
            basePrecipitation = ApplyOrographicEffects(cell, cellGrid, x, y, windVec, windMag, basePrecipitation);
            
            // Water body convective precipitation
            basePrecipitation = ApplyWaterConvectiveEffects(cellGrid, x, y, windVec, windMag, basePrecipitation);
            
            // Infiltration
            float infiltration = Math.Min(basePrecipitation, cell.SoilProps.InfiltrationMmHr * 24f);
            float runoff = basePrecipitation - infiltration;
            
            // Add moisture (convert mm to m³/m³)
            float moistureGain = infiltration * 0.001f;
            cell.SoilMoisture += moistureGain;
            
            // Percolation when above field capacity
            float fieldCapacityVolumetric = (cell.SoilProps.FieldCapacityPct / 100f) * 0.45f;
            if (cell.SoilMoisture > fieldCapacityVolumetric)
            {
                float percolation = (cell.SoilMoisture - fieldCapacityVolumetric) * 0.1f;
                cell.SoilMoisture -= percolation * deltaTimeDays;
            }
            
            // Reset rain tracking
            cell.DaysSinceLastRain = 0f;
            
            if (weather.IsStormy)
            {
                cell.DaysSinceLastStorm = 0f;
            }
        }

        private float ApplyOrographicEffects(EcosystemCell cell, EcosystemCell[,] cellGrid, 
            int x, int y, Vector2 windVec, float windMag, float precipitation)
        {
            if (cell.BaseTerrainType != TerrainType.Plateau) return precipitation;
            if (windMag <= 3f) return precipitation;
            
            // Check for upwind plateau (rain shadow)
            Vector2 upwindDir = -windVec / windMag;
            int checkX = x + (int)Math.Round(upwindDir.X);
            int checkY = y + (int)Math.Round(upwindDir.Y);
            
            bool hasUpwindPlateau = false;
            if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
            {
                hasUpwindPlateau = cellGrid[checkX, checkY]?.BaseTerrainType == TerrainType.Plateau;
            }
            
            if (hasUpwindPlateau)
            {
                // Rain shadow: -30%
                return precipitation * (1f - parameters.RainShadowReductionPct / 100f);
            }
            else
            {
                // Windward: +20%
                return precipitation * (1f + parameters.OrographicBoostPct / 100f);
            }
        }

        private float ApplyWaterConvectiveEffects(EcosystemCell[,] cellGrid, int x, int y, 
            Vector2 windVec, float windMag, float precipitation)
        {
            if (windMag <= 1f) return precipitation;
            
            Vector2 waterUpwindDir = -windVec / windMag;
            
            for (int range = 1; range <= parameters.WaterConvectiveRange; range++)
            {
                int waterCheckX = x + (int)Math.Round(waterUpwindDir.X * range);
                int waterCheckY = y + (int)Math.Round(waterUpwindDir.Y * range);
                
                if (waterCheckX >= 0 && waterCheckX < gridWidth && 
                    waterCheckY >= 0 && waterCheckY < gridHeight)
                {
                    if (cellGrid[waterCheckX, waterCheckY]?.BaseTerrainType == TerrainType.Water)
                    {
                        // Downwind of water: convective uplift increases precipitation
                        float distanceFactor = 1f - (range - 1) / (float)parameters.WaterConvectiveRange;
                        return precipitation * (1f + (parameters.WaterPrecipitationBonus / 100f) * distanceFactor);
                    }
                }
            }
            
            return precipitation;
        }

        private void ApplyEvaporation(EcosystemCell cell, WeatherState weather, float deltaTimeDays)
        {
            float evapRate = cell.SoilProps.EvapRateMmDay;
            
            // Temperature and humidity effects
            float tempFactor = 1f + (cell.Temperature - 20f) * 0.05f;
            float humidityFactor = 1f - (cell.Humidity / 100f) * 0.3f;
            evapRate *= tempFactor * humidityFactor;
            
            // Vegetation reduces evaporation
            float vegCover = Math.Max(cell.ForestCover, cell.GrassCover);
            evapRate *= (1f - vegCover * 0.4f);
            
            // Convert to volumetric moisture loss
            float moistureLoss = (evapRate * 0.001f) * deltaTimeDays;
            cell.SoilMoisture -= moistureLoss;
        }

        private void AddGroundwaterSeepage(EcosystemCell cell, EcosystemCell[,] cellGrid, int x, int y, float deltaTimeDays)
        {
            // Check for nearby water bodies and add groundwater seepage
            int seepageRange = 3; // Water bodies affect 3 cells around them
            float maxSeepage = 0.002f * deltaTimeDays; // 0.2% per day maximum
            
            for (int dx = -seepageRange; dx <= seepageRange; dx++)
            {
                for (int dy = -seepageRange; dy <= seepageRange; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;
                    
                    if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
                    {
                        if (cellGrid[checkX, checkY]?.BaseTerrainType == TerrainType.Water)
                        {
                            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (distance <= seepageRange && distance > 0)
                            {
                                // Seepage decreases with distance
                                float seepageAmount = maxSeepage * (1f - distance / seepageRange);
                                cell.SoilMoisture += seepageAmount;
                                
                                // Cap at field capacity
                                float fieldCapVolumetric = cell.SoilProps.FieldCapacityPct / 100f * 0.45f;
                                cell.SoilMoisture = Math.Min(cell.SoilMoisture, fieldCapVolumetric);
                                return; // Found water, no need to check more
                            }
                        }
                    }
                }
            }
        }
    }
}
