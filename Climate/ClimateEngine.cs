using System;
using Microsoft.Xna.Framework;

namespace TerrainGame
{
    /// <summary>
    /// Main coordinator for the climate simulation, managing all subsystems
    /// </summary>
    public partial class ClimateSimulation
    {
        // Core data
        private ClimateParameters parameters;
        private EcosystemCell[,] cellGrid;
        private TerrainGenerator terrainGenerator;
        private Random random;
        
        // Grid dimensions
        private int gridWidth;
        private int gridHeight;
        
        // Time tracking
        private float simulationTime; // in days
        private float lastWeatherUpdate;
        private float lastEcologyUpdate;

        // Subsystems
        private WeatherSystem weatherSystem;
        private HydrologySystem hydrologySystem;
        private FungalSystem fungalSystem;
        private ToxicitySystem toxicitySystem;
        private VegetationSystem vegetationSystem;
        private SporeDispersalSystem sporeDispersalSystem;

        // Public properties
        public ClimateParameters Parameters => parameters;
        public WeatherState CurrentWeather => weatherSystem.CurrentWeather;
        public float SimulationTime => simulationTime;
        public bool IsPaused { get; set; }
        public float SimulationSpeed { get; set; } = 1.0f;

        public ClimateSimulation(TerrainGenerator terrain, string parameterFile = null)
        {
            Console.WriteLine("ClimateSimulation: Constructor starting...");
            terrainGenerator = terrain;
            gridWidth = terrain.MapWidth;
            gridHeight = terrain.MapHeight;
            random = new Random();
            
            // Initialize simulation state
            IsPaused = false;
            SimulationSpeed = 1.0f;
            simulationTime = 0f;
            lastWeatherUpdate = 0f;
            lastEcologyUpdate = 0f;
            
            Console.WriteLine($"Grid size: {gridWidth}x{gridHeight}");
            
            // Load parameters
            string paramFile = parameterFile ?? @"c:\Users\ANNA\Desktop\Project\spreadsheet\ClimateParameters.csv";
            parameters = ClimateParameters.LoadFromCSV(paramFile);
            
            // Initialize subsystems
            weatherSystem = new WeatherSystem(parameters, random);
            hydrologySystem = new HydrologySystem(parameters, gridWidth, gridHeight);
            fungalSystem = new FungalSystem(parameters);
            toxicitySystem = new ToxicitySystem(parameters);
            vegetationSystem = new VegetationSystem(parameters);
            sporeDispersalSystem = new SporeDispersalSystem(parameters, random, gridWidth, gridHeight);
            
            Console.WriteLine("ClimateSimulation: Initializing simulation...");
            InitializeSimulation();
            Console.WriteLine("ClimateSimulation: Constructor complete!");
        }

        private void InitializeSimulation()
        {
            // Initialize cell grid
            cellGrid = new EcosystemCell[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = new EcosystemCell();
                    var terrainTile = terrainGenerator.GetTile(x, y);
                    
                    if (terrainTile != null)
                    {
                        cell.BaseTerrainType = terrainTile.Type;
                        cell.Elevation = GetElevationFromTerrainType(terrainTile.Type);
                        cell.SoilProps = parameters.GetSoilProperties(terrainTile.Type);
                        cell.IsLowToxicityZone = terrainTile.IsLowToxicityZone;
                        
                        InitializeCellState(cell);
                    }
                    
                    cellGrid[x, y] = cell;
                }
            }
            
            simulationTime = 0f;
            lastWeatherUpdate = 0f;
            lastEcologyUpdate = 0f;
        }

        private void InitializeCellState(EcosystemCell cell)
        {
            var soilProps = cell.SoilProps;
            
            // Soil moisture in m³/m³
            float fieldCapacity_volumetric = soilProps.FieldCapacityPct / 100f;
            cell.SoilMoisture = (soilProps.InitialMoisturePct / 100f) * fieldCapacity_volumetric;
            cell.Toxicity = soilProps.BaseToxicity;
            cell.Temperature = parameters.TempMeanC;
            cell.LocalWind = new Vector2(parameters.WindMeanMs, 0);

            cell.Humidity = cell.BaseTerrainType switch
            {
                TerrainType.Desert => parameters.HumidityDesertPct,
                TerrainType.Grassland => parameters.HumidityGrasslandPct,
                TerrainType.Forest => parameters.HumidityForestPct,
                TerrainType.Water => 95f,
                _ => parameters.HumidityDesertPct
            };
            
            // Initialize vegetation based on terrain
            if (cell.BaseTerrainType == TerrainType.Forest)
            {
                cell.VegetationState = VegetationState.Forest;
                cell.ForestCover = 0.8f;
                cell.GrassCover = 0f;
                cell.FungalMatCover = 0f;
                cell.VegetationIndex = 0.8f;
                cell.FungalBiomass = 0f;
                cell.Toxicity = 0.8f;
            }
            else if (cell.BaseTerrainType == TerrainType.Desert)
            {
                cell.VegetationState = VegetationState.Barren;
                cell.ForestCover = 0f;
                cell.GrassCover = 0f;
                cell.FungalMatCover = 0f;
                cell.VegetationIndex = 0f;
                cell.FungalBiomass = 0f;
                
                if (cell.IsLowToxicityZone)
                {
                    cell.Toxicity = random.NextSingle() * 1.0f;
                }
                else
                {
                    cell.Toxicity = 2.2f;
                }
            }
            else if (cell.BaseTerrainType == TerrainType.Grassland)
            {
                cell.VegetationState = VegetationState.Grass;
                cell.ForestCover = 0f;
                cell.GrassCover = 0.4f;
                cell.FungalMatCover = 0f;
                cell.VegetationIndex = 0.4f;
                cell.FungalBiomass = 0f;
                cell.Toxicity = random.NextSingle() * 1.0f;
            }
            else
            {
                cell.VegetationState = VegetationState.Barren;
                cell.ForestCover = 0f;
                cell.GrassCover = 0f;
                cell.FungalMatCover = 0f;
                cell.VegetationIndex = 0f;
                cell.FungalBiomass = 0f;
            }
            
            cell.DaysSinceLastStorm = 999f;
        }

        private float GetElevationFromTerrainType(TerrainType terrainType)
        {
            return terrainType switch
            {
                TerrainType.Plateau => 200f,
                TerrainType.Canyon => -50f,
                TerrainType.Valley => -20f,
                TerrainType.Forest => 50f,
                TerrainType.Grassland => 20f,
                TerrainType.Water => 0f,
                TerrainType.Desert => 10f,
                _ => 0f
            };
        }

        public void Update(float deltaTime)
        {
            if (IsPaused) return;
            
            float scaledDeltaTime = deltaTime * SimulationSpeed;
            simulationTime += scaledDeltaTime / 86400f; // Convert to days
            
            // Weather updates
            float timeSinceWeatherUpdate = simulationTime - lastWeatherUpdate;
            float weatherThreshold = parameters.TimeStepWeatherS / 86400f;
            if (timeSinceWeatherUpdate >= weatherThreshold)
            {
                weatherSystem.UpdateGlobalWeather(simulationTime);
                weatherSystem.UpdateCellWeather(cellGrid, gridWidth, gridHeight);
                lastWeatherUpdate = simulationTime;
                
                // Log weather updates to show clear progression  
                if ((int)(simulationTime * 5) % 1 == 0) // Every 0.2 days
                {
                    Console.WriteLine($"Day {simulationTime:F2}: Weather - Temp: {CurrentWeather.Temperature:F1}°C, Wind: {CurrentWeather.WindSpeed:F1}m/s, Stormy: {CurrentWeather.IsStormy}");
                }
            }
            
            // Ecology updates
            float timeSinceEcologyUpdate = simulationTime - lastEcologyUpdate;
            float ecologyThreshold = parameters.TimeStepEcologyS / 86400f;
            if (timeSinceEcologyUpdate >= ecologyThreshold)
            {
                UpdateEcology(timeSinceEcologyUpdate);
                lastEcologyUpdate = simulationTime;
                
                // Log ecology updates to show clear progression
                if ((int)(simulationTime * 5) % 1 == 0) // Every 0.2 days
                {
                    // Sample a forest cell to see activity
                    EcosystemCell sampleCell = null;
                    for (int x = 0; x < gridWidth && sampleCell == null; x++)
                    {
                        for (int y = 0; y < gridHeight && sampleCell == null; y++)
                        {
                            if (cellGrid[x, y]?.BaseTerrainType == TerrainType.Forest)
                            {
                                sampleCell = cellGrid[x, y];
                            }
                        }
                    }
                    if (sampleCell == null) sampleCell = cellGrid[gridWidth/2, gridHeight/2];
                    
                    Console.WriteLine($"Day {simulationTime:F2}: Ecology - Soil:{sampleCell?.SoilMoisture:F3}, Toxicity:{sampleCell?.Toxicity:F2}, Fungal:{sampleCell?.FungalMatCover:F2}, Forest:{sampleCell?.ForestCover:F2}, Spores:{sampleCell?.SeedSporeLoad:F1}/{sampleCell?.NonSeedSporeLoad:F1}");
                    
                    Console.WriteLine($"Day {simulationTime:F2}: Ecology - Soil:{sampleCell?.SoilMoisture:F3}, Toxicity:{sampleCell?.Toxicity:F2}, Fungal:{sampleCell?.FungalMatCover:F2}, Forest:{sampleCell?.ForestCover:F2}, Spores:{sampleCell?.SeedSporeLoad:F1}/{sampleCell?.NonSeedSporeLoad:F1}");
                }
            }
        }

        private void UpdateEcology(float deltaTimeDays)
        {
            // 1. Hydrology
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    if (cell == null) continue;
                    
                    hydrologySystem.UpdateSoilMoisture(cell, cellGrid, x, y, CurrentWeather, deltaTimeDays);
                }
            }
            
            // 2. Spore dispersal
            sporeDispersalSystem.UpdateSporeDispersal(cellGrid, CurrentWeather, deltaTimeDays);
            
            // 3. Fungal colonization, toxicity, and vegetation
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    if (cell == null) continue;
                    
                    fungalSystem.UpdateFungalDynamics(cell, deltaTimeDays);
                    toxicitySystem.UpdateToxicity(cell, deltaTimeDays);
                    vegetationSystem.UpdateVegetation(cell, deltaTimeDays);
                }
            }
        }

        public EcosystemCell GetCell(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                return null;
            return cellGrid[x, y];
        }

        public void ApplyNeutralization(int centerX, int centerY)
        {
            NeutralizeArea(centerX, centerY);
        }

        public void NeutralizeArea(int centerX, int centerY)
        {
            int radius = parameters.NeutralizeRadiusCells;
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        var cell = cellGrid[x, y];
                        if (cell != null)
                        {
                            cell.Toxicity *= (1f - parameters.NeutralizeToxicityDrop);
                            cell.AirToxicity = 0f;
                            float moistureBoost = (parameters.NeutralizeMoistureBoostPct / 100f) * 0.45f;
                            cell.SoilMoisture = Math.Min(cell.SoilMoisture + moistureBoost, 0.45f);
                            cell.FungalMatCover *= (1f - parameters.NeutralizeFungalClear);
                            cell.FungalBiomass = cell.FungalMatCover;
                            cell.LastNeutralizationTime = simulationTime;
                        }
                    }
                }
            }
        }

        public float GetAverageToxicity()
        {
            float totalToxicity = 0f;
            int count = 0;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    if (cell != null)
                    {
                        totalToxicity += cell.Toxicity;
                        count++;
                    }
                }
            }

            return count > 0 ? totalToxicity / count : 0f;
        }

        public float GetTotalFungalCover()
        {
            float totalCover = 0f;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = cellGrid[x, y];
                    if (cell != null)
                    {
                        totalCover += cell.FungalMatCover;
                    }
                }
            }

            return totalCover;
        }
    }
}
