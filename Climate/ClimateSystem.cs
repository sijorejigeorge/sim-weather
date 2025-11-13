using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace TerrainGame
{
    public class ClimateParameters
    {
        // Domain Control
        public int DomainSizeM { get; set; } = 5000;
        public int GridResolutionM { get; set; } = 100;
        public int TimeStepWeatherS { get; set; } = 10; // Weather updates every 10 seconds (was 3600)
        public int TimeStepEcologyS { get; set; } = 5;  // Ecology updates every 5 seconds (was 86400)
        public float SimulationSpeed { get; set; } = 1.0f;

        // Base Climate
        public float TempMeanC { get; set; } = 22f;
        public float TempDiurnalC { get; set; } = 15f;
        public float TempSeasonalC { get; set; } = 8f;
        public float WindMeanMs { get; set; } = 6f;
        public float WindStormMs { get; set; } = 25f;
        public float PrecipDesertMmYr { get; set; } = 80f;
        public float PrecipGrasslandMmYr { get; set; } = 200f;
        public float PrecipForestMmYr { get; set; } = 420f;
        public float StormFrequencyDays { get; set; } = 24f;        // 15 storms per year
        public float StormIntensityMm { get; set; } = 50f;          // P_storm_mean
        public float HumidityDesertPct { get; set; } = 25f;
        public float HumidityGrasslandPct { get; set; } = 50f;
        public float HumidityForestPct { get; set; } = 75f;
        public float FogDaysYr { get; set; } = 60f;

        // Terrain Modifiers
        public float PlateauWindMult { get; set; } = 1.35f;         // m_plateau
        public float CanyonWindMult { get; set; } = 1.5f;           // m_canyon
        public float ValleyWindMult { get; set; } = 0.6f;           // m_valley
        public float ForestWindMult { get; set; } = 0.6f;           // m_forest (wind drag)
        public float CanyonHumidityMin { get; set; } = 5f;          // Canyon RH bonus range
        public float CanyonHumidityMax { get; set; } = 20f;
        public float ValleyHumidityMin { get; set; } = 10f;         // Valley RH bonus range
        public float ValleyHumidityMax { get; set; } = 30f;
        public float ValleyFogFrequency { get; set; } = 0.3f;       // Probability of fog (0-1)
        public float ForestEvapotranspirationRH { get; set; } = 15f; // Forest RH increase
        public float WaterPrecipitationBonus { get; set; } = 10f;   // Downwind precipitation increase %
        public float WaterConvectiveRange { get; set; } = 3;        // Cells affected downwind
        public float WaterCoolingC { get; set; } = -2f;
        public float DesertHeatingC { get; set; } = 5f;
        public float PlateauTempDropC { get; set; } = -5f;
        public float CanyonTempRiseC { get; set; } = 3f;
        public float OrographicBoostPct { get; set; } = 20f;
        public float RainShadowReductionPct { get; set; } = 30f;
        
        // Temperature Feedback Coefficients
        public float TempStormCoolingPerMm { get; set; } = 0.2f;       // T -= 0.2*P
        public float TempFungalMatHeating { get; set; } = 0.5f;        // c_F
        public float TempToxicityHeating { get; set; } = 0.3f;         // c_τ
        public float TempVegetationCooling { get; set; } = 0.4f;       // c_V

        // Soil Properties - Desert
        public float DesertPorosity { get; set; } = 0.35f;
        public float DesertFieldCapacityPct { get; set; } = 8f;
        public float DesertWiltingPointPct { get; set; } = 3f;
        public float DesertDepthM { get; set; } = 0.5f;
        public float DesertInitialMoisturePct { get; set; } = 5f;
        public float DesertEvapRateMmDay { get; set; } = 15f;
        public float DesertInfiltrationMmHr { get; set; } = 2f;
        public float DesertRunoffCoeff { get; set; } = 0.8f;
        public float DesertBaseToxicity { get; set; } = 1.5f;

        // Soil Properties - Grassland
        public float GrasslandPorosity { get; set; } = 0.40f;
        public float GrasslandFieldCapacityPct { get; set; } = 15f;
        public float GrasslandWiltingPointPct { get; set; } = 6f;   // θ_wp = 0.06
        public float GrasslandDepthM { get; set; } = 1.0f;
        public float GrasslandInitialMoisturePct { get; set; } = 15f;
        public float GrasslandEvapRateMmDay { get; set; } = 6f;
        public float GrasslandInfiltrationMmHr { get; set; } = 5f;
        public float GrasslandRunoffCoeff { get; set; } = 0.3f;
        public float GrasslandBaseToxicity { get; set; } = 0.8f;

        // Soil Properties - Forest
        public float ForestPorosity { get; set; } = 0.45f;
        public float ForestFieldCapacityPct { get; set; } = 22f;
        public float ForestWiltingPointPct { get; set; } = 10f;
        public float ForestDepthM { get; set; } = 2.0f;
        public float ForestInitialMoisturePct { get; set; } = 18f;
        public float ForestEvapRateMmDay { get; set; } = 1f;
        public float ForestInfiltrationMmHr { get; set; } = 8f;
        public float ForestRunoffCoeff { get; set; } = 0.1f;
        public float ForestBaseToxicity { get; set; } = 0.8f;

        // Soil Properties - Valley
        public float ValleyPorosity { get; set; } = 0.50f;
        public float ValleyFieldCapacityPct { get; set; } = 28f;    // θ_fc = 0.28
        public float ValleyWiltingPointPct { get; set; } = 12f;     // θ_wp = 0.12
        public float ValleyDepthM { get; set; } = 2.5f;
        public float ValleyInitialMoisturePct { get; set; } = 22f;
        public float ValleyEvapRateMmDay { get; set; } = 0.5f;
        public float ValleyInfiltrationMmHr { get; set; } = 10f;
        public float ValleyRunoffCoeff { get; set; } = 0.05f;
        public float ValleyBaseToxicity { get; set; } = 0.5f;

        // Water Bodies
        public float WaterEvapRateMmDay { get; set; } = 2f;
        public float WaterHumidityBoostPct { get; set; } = 20f;
        public float WaterFogChanceMult { get; set; } = 2.0f;

        // Fungal Dynamics
        public float FungalGrowthRateDay { get; set; } = 0.035f;
        public float FungalMortalityDay { get; set; } = 0.03f;
        public int FungalSporeRadiusCells { get; set; } = 4;
        public float FungalSporeSurvivalProb { get; set; } = 0.6f;
        public float FungalHumidityThresholdPct { get; set; } = 40f;
        public float FungalDroughtDeathRateDay { get; set; } = 0.03f;
        public float FungalColonizationRate { get; set; } = 0.5f;
        public float FungalColonizationK_A { get; set; } = 0.5f;        // Sigmoid half-saturation constant
        public float FungalOptimalTempC { get; set; } = 25f;            // Optimal temperature for fungi
        public float StormSporeMultiplier { get; set; } = 15f;

        // Spore System - Two Types (Ecological Specification)
        public float SporeEmissionForestSeed { get; set; } = 1.0f;      // Y_A_base
        public float SporeEmissionForestNonseed { get; set; } = 5.0f;   // Y_B_base
        public float SporeEmissionMatNonseed { get; set; } = 2.0f;      // Y_B_mat
        public float SporeRangeSeedBase { get; set; } = 200f;           // R_A_base (meters)
        public float SporeRangeNonseedBase { get; set; } = 800f;        // R_B_base (meters)
        public float SporeWetFactor { get; set; } = 0.10f;              // wet_factor
        public float SporeSurvivalDecay { get; set; } = 0.15f;          // λ_spore
        public float SporeDiffusionCoeff { get; set; } = 200f;          // D_turb
        public float SporeToxicityMultiplier { get; set; } = 0.7f;      // mult per τ unit

        // Toxicity System (Specification Compliant)
        public float ToxicityRangeMax { get; set; } = 3.0f;
        public float ToxicityNaturalDecayDay { get; set; } = 0.0006f;   // k_tau_nat
        public float ToxicityFungalBoostDay { get; set; } = 0.04f;      // k_tau_mat
        public float ToxicityForestPurifyDay { get; set; } = 0.006f;    // k_tau_forest
        public float ToxicityDiffusionRate { get; set; } = 0.02f;
        public float WindToxicityTransport { get; set; } = 0.04f;

        // Vegetation Dynamics (Specification Values)
        public float VegGrowthRateDay { get; set; } = 0.025f;          // r_G
        public float VegDroughtDeathRateDay { get; set; } = 0.05f;
        public float VegToxicityDeathRateDay { get; set; } = 0.08f;
        public float VegMoistureThresholdPct { get; set; } = 5f;        // θ_germ
        public float VegToxicityThreshold { get; set; } = 2.0f;         // Max τ for grass
        public float VegEstablishmentProb { get; set; } = 0.3f;
        public float ForestGrowthRateDay { get; set; } = 0.008f;        // r_R
        public float ForestMortalityDay { get; set; } = 0.001f;         // m_R
        public float MatToForestDays { get; set; } = 730f;              // t_mat_to_forest_days
        public float MatToForestThreshold { get; set; } = 0.6f;         // Min fungal biomass (F > 0.6)
        public float SuccessionGrassThresholdToxicity { get; set; } = 1.0f;
        public float SuccessionShrubThresholdToxicity { get; set; } = 0.4f;
        public float SuccessionForestThresholdToxicity { get; set; } = 0.25f;
        public float SuccessionMoistureGrassPct { get; set; } = 5f;
        public float SuccessionMoistureShrubPct { get; set; } = 8f;
        public float SuccessionMoistureForestPct { get; set; } = 15f;
        public int SuccessionTimeGrassDays { get; set; } = 30;
        public int SuccessionTimeShrubDays { get; set; } = 365;
        public int SuccessionTimeForestDays { get; set; } = 1095;

        // Neutralization
        public int NeutralizeRadiusCells { get; set; } = 3;
        public float NeutralizeToxicityDrop { get; set; } = 0.8f;
        public float NeutralizeMoistureBoostPct { get; set; } = 25f;
        public float NeutralizeFungalClear { get; set; } = 1.0f;

        // Simulation Control
        public bool SimulationPaused { get; set; } = false;
        public int LogFrequencyDays { get; set; } = 1;
        public bool OutputStats { get; set; } = true;
        public float VisualizationUpdateRate { get; set; } = 0.1f;

        public static ClimateParameters LoadFromCSV(string filePath)
        {
            var parameters = new ClimateParameters();
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Warning: Climate parameters file not found at {filePath}, using defaults");
                return parameters;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 2)
                        continue;

                    string paramName = parts[0].Trim();
                    string paramValue = parts[1].Trim();

                    SetParameterValue(parameters, paramName, paramValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading climate parameters: {ex.Message}");
            }

            return parameters;
        }

        private static void SetParameterValue(ClimateParameters parameters, string name, string value)
        {
            var property = typeof(ClimateParameters).GetProperty(ToPascalCase(name));
            if (property == null) return;

            try
            {
                object convertedValue = property.PropertyType.Name switch
                {
                    "Single" => float.Parse(value),
                    "Int32" => int.Parse(value),
                    "Boolean" => bool.Parse(value),
                    _ => value
                };
                
                property.SetValue(parameters, convertedValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting parameter {name}: {ex.Message}");
            }
        }

        private static string ToPascalCase(string input)
        {
            var parts = input.Split('_');
            string result = "";
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result += char.ToUpper(part[0]) + part.Substring(1).ToLower();
                }
            }
            return result;
        }

        public SoilProperties GetSoilProperties(TerrainType terrainType)
        {
            return terrainType switch
            {
                TerrainType.Desert => new SoilProperties
                {
                    Porosity = DesertPorosity,
                    FieldCapacityPct = DesertFieldCapacityPct,
                    WiltingPointPct = DesertWiltingPointPct,
                    DepthM = DesertDepthM,
                    InitialMoisturePct = DesertInitialMoisturePct,
                    EvapRateMmDay = DesertEvapRateMmDay,
                    InfiltrationMmHr = DesertInfiltrationMmHr,
                    RunoffCoeff = DesertRunoffCoeff,
                    BaseToxicity = DesertBaseToxicity
                },
                TerrainType.Grassland => new SoilProperties
                {
                    Porosity = GrasslandPorosity,
                    FieldCapacityPct = GrasslandFieldCapacityPct,
                    WiltingPointPct = GrasslandWiltingPointPct,
                    DepthM = GrasslandDepthM,
                    InitialMoisturePct = GrasslandInitialMoisturePct,
                    EvapRateMmDay = GrasslandEvapRateMmDay,
                    InfiltrationMmHr = GrasslandInfiltrationMmHr,
                    RunoffCoeff = GrasslandRunoffCoeff,
                    BaseToxicity = GrasslandBaseToxicity
                },
                TerrainType.Forest => new SoilProperties
                {
                    Porosity = ForestPorosity,
                    FieldCapacityPct = ForestFieldCapacityPct,
                    WiltingPointPct = ForestWiltingPointPct,
                    DepthM = ForestDepthM,
                    InitialMoisturePct = ForestInitialMoisturePct,
                    EvapRateMmDay = ForestEvapRateMmDay,
                    InfiltrationMmHr = ForestInfiltrationMmHr,
                    RunoffCoeff = ForestRunoffCoeff,
                    BaseToxicity = ForestBaseToxicity
                },
                TerrainType.Water => new SoilProperties
                {
                    Porosity = 1.0f,
                    FieldCapacityPct = 100f,
                    WiltingPointPct = 0f,
                    DepthM = 10f, // Deep water
                    InitialMoisturePct = 100f,
                    EvapRateMmDay = WaterEvapRateMmDay,
                    InfiltrationMmHr = 0f,
                    RunoffCoeff = 0f,
                    BaseToxicity = 0f
                },
                TerrainType.Valley => new SoilProperties
                {
                    Porosity = ValleyPorosity,
                    FieldCapacityPct = ValleyFieldCapacityPct,
                    WiltingPointPct = ValleyWiltingPointPct,
                    DepthM = ValleyDepthM,
                    InitialMoisturePct = ValleyInitialMoisturePct,
                    EvapRateMmDay = ValleyEvapRateMmDay,
                    InfiltrationMmHr = ValleyInfiltrationMmHr,
                    RunoffCoeff = ValleyRunoffCoeff,
                    BaseToxicity = ValleyBaseToxicity
                },
                TerrainType.Plateau => new SoilProperties
                {
                    Porosity = DesertPorosity, // Similar to desert - rocky, dry
                    FieldCapacityPct = DesertFieldCapacityPct,
                    WiltingPointPct = DesertWiltingPointPct,
                    DepthM = 0.3f, // Shallow soil on rock
                    InitialMoisturePct = DesertInitialMoisturePct,
                    EvapRateMmDay = DesertEvapRateMmDay * 1.5f, // Higher evaporation due to wind
                    InfiltrationMmHr = DesertInfiltrationMmHr * 0.5f, // Lower infiltration - rocky
                    RunoffCoeff = 0.9f, // High runoff
                    BaseToxicity = DesertBaseToxicity
                },
                TerrainType.Canyon => new SoilProperties
                {
                    Porosity = GrasslandPorosity, // Better than desert due to water collection
                    FieldCapacityPct = GrasslandFieldCapacityPct,
                    WiltingPointPct = GrasslandWiltingPointPct,
                    DepthM = GrasslandDepthM,
                    InitialMoisturePct = GrasslandInitialMoisturePct * 1.2f, // More moisture
                    EvapRateMmDay = GrasslandEvapRateMmDay * 0.8f, // Protected from wind
                    InfiltrationMmHr = GrasslandInfiltrationMmHr,
                    RunoffCoeff = 0.4f, // Moderate runoff
                    BaseToxicity = GrasslandBaseToxicity
                },
                _ => new SoilProperties
                {
                    Porosity = DesertPorosity,
                    FieldCapacityPct = DesertFieldCapacityPct,
                    WiltingPointPct = DesertWiltingPointPct,
                    DepthM = DesertDepthM,
                    InitialMoisturePct = DesertInitialMoisturePct,
                    EvapRateMmDay = DesertEvapRateMmDay,
                    InfiltrationMmHr = DesertInfiltrationMmHr,
                    RunoffCoeff = DesertRunoffCoeff,
                    BaseToxicity = DesertBaseToxicity
                }
            };
        }
    }

    public class SoilProperties
    {
        public float Porosity { get; set; }
        public float FieldCapacityPct { get; set; }
        public float WiltingPointPct { get; set; }
        public float DepthM { get; set; }
        public float InitialMoisturePct { get; set; }
        public float EvapRateMmDay { get; set; }
        public float InfiltrationMmHr { get; set; }
        public float RunoffCoeff { get; set; }
        public float BaseToxicity { get; set; }
    }

    public enum VegetationState
    {
        Barren = 0,
        Grass = 1,
        Shrub = 2,
        FungalMat = 3,  // NEW: Intermediate state between desert and forest
        Forest = 4      // Dense fungal trees formed on fungal mats
    }

    public class WeatherState
    {
        public float Temperature { get; set; } // °C
        public float Humidity { get; set; } // %
        public float WindSpeed { get; set; } // m/s
        public Vector2 WindDirection { get; set; } // normalized vector
        public float Precipitation { get; set; } // mm/day
        public bool IsStormy { get; set; }
        public bool IsFoggy { get; set; }
    }

    public class EcosystemCell
    {
        // Base terrain properties
        public TerrainType BaseTerrainType { get; set; }
        public float Elevation { get; set; }
        public SoilProperties SoilProps { get; set; }
        public bool IsLowToxicityZone { get; set; } // Special 3-7 patches with 0-1 toxicity

        // Dynamic state variables (per specification)
        public float Temperature { get; set; } // T: °C, range -10 to 50
        public float Humidity { get; set; } // RH: %, range 0-100
        public float SoilMoisture { get; set; } // θ: m³/m³, range 0.0-0.45 (volumetric)
        public float Toxicity { get; set; } // τ: soil/water toxicity, range 0.0-3.0
        public float AirToxicity { get; set; } // air toxicity from spores, range 0.0-3.0
        
        // Vegetation cover (separate as per spec)
        public float FungalMatCover { get; set; } // F: fungal mat fraction, range 0.0-1.0
        public float ForestCover { get; set; } // R: fungal-forest fraction, range 0.0-1.0
        public float GrassCover { get; set; } // G: grass fraction, range 0.0-1.0
        
        [Obsolete("Use FungalMatCover instead")]
        public float FungalBiomass { get; set; } // Deprecated - use FungalMatCover
        
        [Obsolete("Use ForestCover and GrassCover instead")]
        public float VegetationIndex { get; set; } // Deprecated - use ForestCover/GrassCover
        
        public VegetationState VegetationState { get; set; }
        
        // Spore dynamics - TWO TYPES as per ecological model
        public float SeedSporeLoad { get; set; }    // S_A: airborne seed-spore density (arb. units, ≥0)
        public float NonSeedSporeLoad { get; set; } // S_B: airborne non-seed spore density (arb. units, ≥0)
        public float SporeProduction { get; set; }  // outgoing spores
        public float DaysCleanSoil { get; set; }    // Days this cell has had 0 toxicity (for forest spore production logic)
        
        [Obsolete("Use SeedSporeLoad and NonSeedSporeLoad instead")]
        public float SporeLoad { get; set; } // Keep for backward compatibility
        
        // Wind and weather
        public Vector2 LocalWind { get; set; } // W⃗: local wind vector m/s (vx, vy), magnitude 0-40
        public float DaysSinceLastStorm { get; set; } // t_storm: days since last storm, ≥0
        
        // Time tracking
        public float DaysSinceLastRain { get; set; }
        public float DaysSinceVegEstablishment { get; set; }
        public float LastNeutralizationTime { get; set; }

        public EcosystemCell()
        {
            BaseTerrainType = TerrainType.Desert;
            VegetationState = VegetationState.Barren;
            LastNeutralizationTime = -999f;
            LocalWind = Vector2.Zero;
            DaysSinceLastStorm = 0f;
        }
    }
}
