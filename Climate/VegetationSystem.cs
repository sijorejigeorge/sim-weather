using System;

namespace TerrainGame
{
    /// <summary>
    /// Handles vegetation growth (forests and grasslands) and succession dynamics
    /// </summary>
    public class VegetationSystem
    {
        private readonly ClimateParameters parameters;

        public VegetationSystem(ClimateParameters parameters)
        {
            this.parameters = parameters;
        }

        public void UpdateVegetation(EcosystemCell cell, float deltaTimeDays)
        {
            if (cell.BaseTerrainType == TerrainType.Water) return;
            
            // Check stress conditions
            bool droughtStress = CheckDroughtStress(cell);
            bool toxicStress = CheckToxicStress(cell);
            
            // Apply stress-induced mortality
            if (droughtStress || toxicStress)
            {
                ApplyMortality(cell, droughtStress, toxicStress, deltaTimeDays);
            }
            
            // Growth if conditions allow
            float successionMoistureVolumetric = parameters.SuccessionMoistureGrassPct / 100f * 0.45f;
            bool canGrow = !droughtStress && !toxicStress && cell.SoilMoisture > successionMoistureVolumetric;
            
            if (canGrow)
            {
                ApplyGrowth(cell, deltaTimeDays);
                cell.DaysSinceVegEstablishment += deltaTimeDays;
            }
            else
            {
                cell.DaysSinceVegEstablishment = 0f;
            }
            
            // Succession logic
            UpdateVegetationSuccession(cell);
            
            // Update VegetationState based on current cover levels
            UpdateVegetationState(cell);
            
            // Clamp and sync values
            cell.ForestCover = Math.Clamp(cell.ForestCover, 0f, 1f);
            cell.GrassCover = Math.Clamp(cell.GrassCover, 0f, 1f);
            cell.VegetationIndex = Math.Max(cell.ForestCover, cell.GrassCover);
        }

        private void UpdateVegetationState(EcosystemCell cell)
        {
            // Update VegetationState based on dominant cover type
            if (cell.FungalMatCover > 0.001f && cell.FungalMatCover > cell.ForestCover && cell.FungalMatCover > cell.GrassCover)
            {
                cell.VegetationState = VegetationState.FungalMat;
            }
            else if (cell.ForestCover > 0.1f && cell.ForestCover > cell.GrassCover)
            {
                cell.VegetationState = VegetationState.Forest;
            }
            else if (cell.GrassCover > 0.1f)
            {
                cell.VegetationState = VegetationState.Grass;
            }
            else
            {
                cell.VegetationState = VegetationState.Barren;
            }
        }

        private bool CheckDroughtStress(EcosystemCell cell)
        {
            float wiltingPointVolumetric = cell.SoilProps.WiltingPointPct / 100f * 0.45f;
            
            // Forests have deeper roots and access to groundwater - much more drought resistant
            if (cell.VegetationState == VegetationState.Forest)
            {
                return cell.SoilMoisture < (wiltingPointVolumetric * 0.1f); // 90% more drought tolerant
            }
            
            return cell.SoilMoisture < wiltingPointVolumetric;
        }

        private bool CheckToxicStress(EcosystemCell cell)
        {
            if (cell.VegetationState == VegetationState.Forest)
            {
                return cell.Toxicity > 3.0f; // Forests can survive very high toxicity
            }
            else if (cell.VegetationState == VegetationState.FungalMat)
            {
                return false; // Fungal mats thrive in toxicity
            }
            else
            {
                return cell.Toxicity > parameters.VegToxicityThreshold;
            }
        }

        private void ApplyMortality(EcosystemCell cell, bool droughtStress, bool toxicStress, float deltaTimeDays)
        {
            if (droughtStress)
            {
                float deathRate = parameters.VegDroughtDeathRateDay;
                
                // Forests are extremely drought resistant due to deep roots
                if (cell.VegetationState == VegetationState.Forest)
                {
                    deathRate *= 0.05f; // 95% reduction in drought mortality for forests
                }
                
                cell.ForestCover -= cell.ForestCover * deathRate * deltaTimeDays;
                cell.GrassCover -= cell.GrassCover * deathRate * deltaTimeDays;
            }
            
            if (toxicStress)
            {
                float deathRate = parameters.VegToxicityDeathRateDay;
                cell.ForestCover -= cell.ForestCover * deathRate * deltaTimeDays;
                cell.GrassCover -= cell.GrassCover * deathRate * deltaTimeDays;
            }
            
            cell.VegetationIndex = Math.Max(cell.ForestCover, cell.GrassCover);
        }

        private void ApplyGrowth(EcosystemCell cell, float deltaTimeDays)
        {
            // Forest growth: R += r_R*R*env_mult - m_R*R
            if (cell.VegetationState == VegetationState.Forest && cell.ForestCover > 0f)
            {
                float fieldCapacityVolumetric = cell.SoilProps.FieldCapacityPct / 100f * 0.45f;
                float env_mult_R = (cell.SoilMoisture / Math.Max(fieldCapacityVolumetric, 0.01f));
                env_mult_R *= (1f - cell.Toxicity / parameters.ToxicityRangeMax);
                
                float forestGrowth = parameters.ForestGrowthRateDay * cell.ForestCover * env_mult_R * deltaTimeDays;
                float forestMortality = parameters.ForestMortalityDay * cell.ForestCover * deltaTimeDays;
                cell.ForestCover += forestGrowth - forestMortality;
            }
            // Grass growth: G += r_G*(1-G)*θ/θ_fc - mortality
            else if (cell.VegetationState == VegetationState.Grass)
            {
                float fieldCapacityVolumetric = cell.SoilProps.FieldCapacityPct / 100f * 0.45f;
                float theta_ratio = cell.SoilMoisture / Math.Max(fieldCapacityVolumetric, 0.01f);
                
                float grassGrowth = parameters.VegGrowthRateDay * (1f - cell.GrassCover) * theta_ratio * deltaTimeDays;
                
                float mortality = 0.001f * cell.GrassCover * deltaTimeDays;
                if (cell.Toxicity > 1.0f)
                    mortality += 0.01f * cell.GrassCover * (cell.Toxicity / 3.0f) * deltaTimeDays;
                
                cell.GrassCover += grassGrowth - mortality;
                cell.GrassCover = Math.Clamp(cell.GrassCover, 0f, 1f);
            }
            
            cell.VegetationIndex = Math.Max(cell.ForestCover, cell.GrassCover);
        }

        private void UpdateVegetationSuccession(EcosystemCell cell)
        {
            // Barren → Fungal Mat (from spores)
            if (cell.VegetationState == VegetationState.Barren && cell.FungalMatCover > 0.2f)
            {
                cell.VegetationState = VegetationState.FungalMat;
                cell.FungalMatCover = Math.Max(cell.FungalMatCover, 0.3f);
                cell.ForestCover = 0f;
                cell.GrassCover = 0f;
                cell.VegetationIndex = cell.FungalMatCover;
                cell.FungalBiomass = cell.FungalMatCover;
                cell.DaysSinceVegEstablishment = 0f;
            }
            
            // Fungal Mat → Forest (if conditions met)
            if (cell.VegetationState == VegetationState.FungalMat)
            {
                if (cell.FungalMatCover > parameters.MatToForestThreshold && cell.SoilMoisture > 0.05f)
                {
                    float transitionDays = parameters.MatToForestDays;
                    float transitionRate = cell.FungalMatCover / transitionDays;
                    
                    cell.ForestCover += transitionRate * 0.001f;
                    cell.FungalMatCover -= transitionRate * 0.0005f;
                    
                    if (cell.ForestCover > 0.2f)
                    {
                        cell.VegetationState = VegetationState.Forest;
                        cell.ForestCover = 0.3f;
                        cell.GrassCover = 0f;
                        cell.FungalMatCover = 0.1f;
                        cell.VegetationIndex = 0.3f;
                        cell.FungalBiomass = 0.1f;
                    }
                }
            }
            
            // Grass establishment in low-toxicity areas
            if (cell.VegetationState == VegetationState.Barren && 
                cell.Toxicity < 1.0f && 
                cell.SoilMoisture > 0.05f)
            {
                cell.VegetationState = VegetationState.Grass;
                cell.GrassCover = 0.1f;
                cell.VegetationIndex = 0.1f;
                cell.FungalBiomass = 0f;
            }
        }
    }
}
