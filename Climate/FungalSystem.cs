using System;

namespace TerrainGame
{
    /// <summary>
    /// Handles fungal mat colonization and growth dynamics
    /// </summary>
    public class FungalSystem
    {
        private readonly ClimateParameters parameters;

        public FungalSystem(ClimateParameters parameters)
        {
            this.parameters = parameters;
        }

        public void UpdateFungalDynamics(EcosystemCell cell, float deltaTimeDays)
        {
            if (cell.BaseTerrainType == TerrainType.Water) return;
            
            // Colonization by seed spores
            ApplySporeColonization(cell, deltaTimeDays);
            
            // Growth or death of existing mats
            UpdateExistingFungalMats(cell, deltaTimeDays);
            
            cell.FungalMatCover = Math.Clamp(cell.FungalMatCover, 0f, 1f);
            cell.FungalBiomass = cell.FungalMatCover; // Keep deprecated property in sync
        }

        private void ApplySporeColonization(EcosystemCell cell, float deltaTimeDays)
        {
            // ONLY SEED SPORES can form new fungal mats
            // Specification: P_col = base_p * sigmoid(A_deposited / K_A) * f_moisture(θ) * f_temp(T)
            if (cell.SeedSporeLoad <= 0f) return;
            
            // Sigmoid function: sigmoid(x) = x / (K_A + x)
            float sigmoidFactor = cell.SeedSporeLoad / (parameters.FungalColonizationK_A + cell.SeedSporeLoad);
            
            // Moisture function f_moisture(θ)
            float moistureFactor = cell.SoilMoisture > 0.05f ? 1.5f : 1.0f;
            
            // Temperature function f_temp(T): optimal around 25°C
            float tempDelta = Math.Abs(cell.Temperature - parameters.FungalOptimalTempC);
            float tempFactor = Math.Max(0.1f, 1f - tempDelta / 30f);
            
            // P_col = base * sigmoid * f_moisture * f_temp
            float colonizationProb = parameters.FungalColonizationRate * sigmoidFactor * moistureFactor * tempFactor;
            
            // NON-SEED SPORES BOOST COLONIZATION when present with seed spores
            if (cell.NonSeedSporeLoad > 0f)
            {
                float boostFactor = 1f + Math.Min(cell.NonSeedSporeLoad * 2f, 1.5f);
                colonizationProb *= boostFactor;
            }
            
            // F += P_col * (1-F)
            cell.FungalMatCover += colonizationProb * (1f - cell.FungalMatCover) * deltaTimeDays;
            
            // Set vegetation state when fungal mats form
            if (cell.FungalMatCover > 0.001f)
            {
                cell.VegetationState = VegetationState.FungalMat;
            }
        }

        private void UpdateExistingFungalMats(EcosystemCell cell, float deltaTimeDays)
        {
            if (cell.FungalMatCover <= 0f) return;
            
            bool humidityOk = cell.Humidity > parameters.FungalHumidityThresholdPct;
            bool moistureOk = cell.SoilMoisture > 0.05f;
            
            if (humidityOk && moistureOk)
            {
                // Growth
                float growthRate = parameters.FungalGrowthRateDay;
                growthRate *= (cell.Humidity / 100f); // Better in high humidity
                growthRate *= Math.Min(1f, cell.SoilMoisture / 0.1f); // Better with moisture
                
                // Logistic growth
                float growth = growthRate * cell.FungalMatCover * (1f - cell.FungalMatCover) * deltaTimeDays;
                cell.FungalMatCover += growth;
            }
            else
            {
                // Die back in poor conditions
                float deathRate = parameters.FungalMortalityDay;
                if (!humidityOk) deathRate += parameters.FungalDroughtDeathRateDay;
                
                cell.FungalMatCover -= cell.FungalMatCover * deathRate * deltaTimeDays;
            }
        }
    }
}
