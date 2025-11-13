using System;
using Microsoft.Xna.Framework;

namespace TerrainGame
{
    /// <summary>
    /// Handles global and local weather dynamics including temperature, wind, precipitation, and storms
    /// </summary>
    public class WeatherSystem
    {
        private readonly ClimateParameters parameters;
        private readonly Random random;
        private WeatherState currentWeather;
        
        public WeatherState CurrentWeather => currentWeather;

        public WeatherSystem(ClimateParameters parameters, Random random)
        {
            this.parameters = parameters;
            this.random = random;
            
            currentWeather = new WeatherState
            {
                Temperature = parameters.TempMeanC,
                Humidity = parameters.HumidityDesertPct,
                WindSpeed = parameters.WindMeanMs,
                WindDirection = new Vector2(1, 0),
                Precipitation = 0f,
                IsStormy = false,
                IsFoggy = false
            };
        }

        public void UpdateGlobalWeather(float simulationTime)
        {
            float dayOfYear = simulationTime % 365f;
            float hourOfDay = (simulationTime * 24f) % 24f;
            
            // Base temperature with seasonal and diurnal variation
            float seasonalTemp = parameters.TempSeasonalC * (float)Math.Sin(2 * Math.PI * dayOfYear / 365f);
            float diurnalTemp = parameters.TempDiurnalC * (float)Math.Sin(2 * Math.PI * hourOfDay / 24f);
            currentWeather.Temperature = parameters.TempMeanC + seasonalTemp + diurnalTemp;
            
            // Wind with some variability
            float windVariation = (float)(random.NextDouble() - 0.5) * 0.4f;
            currentWeather.WindSpeed = Math.Max(1f, parameters.WindMeanMs * (1f + windVariation));
            
            // Wind direction (mostly prevailing, with some variation)
            float windAngle = (float)(Math.PI + (random.NextDouble() - 0.5) * Math.PI * 0.3); // Mostly west-east
            currentWeather.WindDirection = new Vector2((float)Math.Cos(windAngle), (float)Math.Sin(windAngle));
            
            // Storm events
            float stormChance = 1f / parameters.StormFrequencyDays;
            currentWeather.IsStormy = random.NextSingle() < stormChance;
            
            if (currentWeather.IsStormy)
            {
                currentWeather.WindSpeed = Math.Max(currentWeather.WindSpeed, parameters.WindStormMs);
                currentWeather.Precipitation = parameters.StormIntensityMm;
            }
            else
            {
                currentWeather.Precipitation = 0f;
            }
            
            // Fog conditions
            float fogChance = parameters.FogDaysYr / 365f;
            currentWeather.IsFoggy = random.NextSingle() < fogChance && currentWeather.Humidity > 80f;
        }

        public void UpdateCellWeather(EcosystemCell[,] cellGrid, int gridWidth, int gridHeight)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    if (cell == null) continue;
                    
                    UpdateCellTemperature(cell);
                    UpdateCellHumidity(cell, cellGrid, x, y, gridWidth, gridHeight);
                    UpdateCellWind(cell);
                }
            }
        }

        private void UpdateCellTemperature(EcosystemCell cell)
        {
            // Base temperature with terrain modifiers
            cell.Temperature = currentWeather.Temperature;
            
            switch (cell.BaseTerrainType)
            {
                case TerrainType.Desert:
                    cell.Temperature += parameters.DesertHeatingC;
                    break;
                case TerrainType.Water:
                    cell.Temperature += parameters.WaterCoolingC;
                    break;
                case TerrainType.Plateau:
                    cell.Temperature += parameters.PlateauTempDropC;
                    break;
                case TerrainType.Canyon:
                    cell.Temperature += parameters.CanyonTempRiseC;
                    break;
            }
            
            // Precipitation cooling: T -= 0.2*P
            if (currentWeather.Precipitation > 0)
            {
                cell.Temperature -= parameters.TempStormCoolingPerMm * currentWeather.Precipitation;
            }
            
            // Temperature feedbacks from ecology: T += c_F*F + c_τ*(τ/3) - c_V*(G+R)
            cell.Temperature += parameters.TempFungalMatHeating * cell.FungalMatCover;
            cell.Temperature += parameters.TempToxicityHeating * (cell.Toxicity / 3.0f);
            cell.Temperature -= parameters.TempVegetationCooling * (cell.GrassCover + cell.ForestCover);
        }

        private void UpdateCellHumidity(EcosystemCell cell, EcosystemCell[,] cellGrid, int x, int y, int gridWidth, int gridHeight)
        {
            // Base humidity by terrain type
            cell.Humidity = GetBaseHumidity(cell.BaseTerrainType);
            
            // Canyon: RH +5-20% based on wind channeling and turbulence
            if (cell.BaseTerrainType == TerrainType.Canyon)
            {
                float windSpeed = (float)Math.Sqrt(cell.LocalWind.X * cell.LocalWind.X + cell.LocalWind.Y * cell.LocalWind.Y);
                float canyonBonus = parameters.CanyonHumidityMin + 
                    (parameters.CanyonHumidityMax - parameters.CanyonHumidityMin) * Math.Min(windSpeed / 15f, 1f);
                cell.Humidity += canyonBonus;
            }
            
            // Valley: fog + RH +10-30%
            if (cell.BaseTerrainType == TerrainType.Valley)
            {
                float valleyBonus = parameters.ValleyHumidityMin;
                float windSpeed = (float)Math.Sqrt(cell.LocalWind.X * cell.LocalWind.X + cell.LocalWind.Y * cell.LocalWind.Y);
                
                if (cell.Humidity > 60f && windSpeed < 3f)
                {
                    valleyBonus = parameters.ValleyHumidityMax;
                }
                else
                {
                    float moistureFactor = Math.Min(cell.SoilMoisture / 0.3f, 1f);
                    valleyBonus += (parameters.ValleyHumidityMax - parameters.ValleyHumidityMin) * moistureFactor;
                }
                
                cell.Humidity += valleyBonus;
            }
            
            // Forest: evapotranspiration increases RH
            if (cell.VegetationState == VegetationState.Forest && cell.ForestCover > 0.3f)
            {
                cell.Humidity += parameters.ForestEvapotranspirationRH * cell.ForestCover;
            }
            
            // Water bodies increase nearby humidity
            if (HasNearbyWater(cellGrid, x, y, gridWidth, gridHeight))
            {
                cell.Humidity += parameters.WaterHumidityBoostPct;
            }
            
            cell.Humidity = Math.Clamp(cell.Humidity, 0f, 100f);
        }

        private void UpdateCellWind(EcosystemCell cell)
        {
            Vector2 globalWind = currentWeather.WindDirection * currentWeather.WindSpeed;
            
            // Terrain modifiers
            if (cell.BaseTerrainType == TerrainType.Plateau)
            {
                cell.LocalWind = new Vector2(globalWind.X * parameters.PlateauWindMult, globalWind.Y * parameters.PlateauWindMult);
            }
            else if (cell.BaseTerrainType == TerrainType.Valley)
            {
                cell.LocalWind = new Vector2(globalWind.X * parameters.ValleyWindMult, globalWind.Y * parameters.ValleyWindMult);
            }
            else if (cell.BaseTerrainType == TerrainType.Canyon)
            {
                cell.LocalWind = new Vector2(globalWind.X * parameters.CanyonWindMult, globalWind.Y * parameters.CanyonWindMult);
            }
            else
            {
                cell.LocalWind = globalWind;
            }
            
            // Forest vegetation reduces wind (m_forest)
            if (cell.VegetationState == VegetationState.Forest && cell.ForestCover > 0.3f)
            {
                float forestDrag = parameters.ForestWindMult + (1f - parameters.ForestWindMult) * (1f - cell.ForestCover);
                cell.LocalWind = new Vector2(cell.LocalWind.X * forestDrag, cell.LocalWind.Y * forestDrag);
            }
        }

        private float GetBaseHumidity(TerrainType terrainType)
        {
            return terrainType switch
            {
                TerrainType.Desert => parameters.HumidityDesertPct,
                TerrainType.Grassland => parameters.HumidityGrasslandPct,
                TerrainType.Forest => parameters.HumidityForestPct,
                TerrainType.Water => 95f,
                TerrainType.Plateau => parameters.HumidityDesertPct,
                TerrainType.Canyon => parameters.HumidityGrasslandPct,
                TerrainType.Valley => parameters.HumidityForestPct,
                _ => parameters.HumidityDesertPct
            };
        }

        private bool HasNearbyWater(EcosystemCell[,] cellGrid, int centerX, int centerY, int gridWidth, int gridHeight)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        var cell = cellGrid[x, y];
                        if (cell?.BaseTerrainType == TerrainType.Water)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
