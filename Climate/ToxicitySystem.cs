using System;

namespace TerrainGame
{
    /// <summary>
    /// Handles toxicity dynamics including natural decay, fungal pollution, and forest purification
    /// </summary>
    public class ToxicitySystem
    {
        private readonly ClimateParameters parameters;

        public ToxicitySystem(ClimateParameters parameters)
        {
            this.parameters = parameters;
        }

        public void UpdateToxicity(EcosystemCell cell, float deltaTimeDays)
        {
            // Natural decay
            cell.Toxicity -= parameters.ToxicityNaturalDecayDay * deltaTimeDays;
            cell.AirToxicity -= parameters.ToxicityNaturalDecayDay * deltaTimeDays * 0.2f;
            
            // Fungal mats POLLUTE
            if (cell.VegetationState == VegetationState.FungalMat)
            {
                float toxicityIncrease = parameters.ToxicityFungalBoostDay * cell.FungalMatCover;
                cell.Toxicity += toxicityIncrease * deltaTimeDays;
                cell.AirToxicity += toxicityIncrease * deltaTimeDays;
            }
            // Forests PURIFY soil/water
            else if (cell.VegetationState == VegetationState.Forest)
            {
                float detoxRate = parameters.ToxicityForestPurifyDay * cell.ForestCover;
                cell.Toxicity -= detoxRate * deltaTimeDays;
            }
            // Grasslands provide mild detoxification
            else if (cell.VegetationState == VegetationState.Grass)
            {
                float detoxRate = parameters.ToxicityForestPurifyDay * cell.GrassCover * 0.3f;
                cell.Toxicity -= detoxRate * deltaTimeDays;
            }
            
            // Clamp values
            cell.Toxicity = Math.Clamp(cell.Toxicity, 0f, parameters.ToxicityRangeMax);
            cell.AirToxicity = Math.Clamp(cell.AirToxicity, 0f, parameters.ToxicityRangeMax);
        }
    }
}
