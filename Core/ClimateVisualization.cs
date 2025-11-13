using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TerrainGame
{
    public enum VisualizationMode
    {
        Terrain = 0,
        Spores = 1,          // NEW: Show spore distribution
        Wind = 2,
        Temperature = 3,
        Toxicity = 4,        // Soil/water toxicity
        Vegetation = 5,
        SoilMoisture = 6,
        AirToxicity = 7,     // Air toxicity from spores
        FungalSpread = 8     // Moved to 8
    }

    public class ClimateVisualization
    {
        private Texture2D pixelTexture;
        private Dictionary<VisualizationMode, bool> activeOverlays;
        private ClimateSimulation climateSimulation;
        private TerrainRenderer baseRenderer;
        private SpriteFont font;
        
        public ClimateVisualization(GraphicsDevice graphicsDevice, ClimateSimulation simulation, TerrainRenderer baseRenderer, SpriteFont font = null)
        {
            this.climateSimulation = simulation;
            this.baseRenderer = baseRenderer;
            this.font = font;
            
            // Create pixel texture for overlays
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            // Initialize overlay toggles
            activeOverlays = new Dictionary<VisualizationMode, bool>();
            foreach (VisualizationMode mode in Enum.GetValues<VisualizationMode>())
            {
                activeOverlays[mode] = false;
            }
            
            // Show terrain by default
            activeOverlays[VisualizationMode.Terrain] = true;
        }

        public void ToggleVisualization(VisualizationMode mode)
        {
            activeOverlays[mode] = !activeOverlays[mode];
        }

        public bool IsVisualizationActive(VisualizationMode mode)
        {
            return activeOverlays.TryGetValue(mode, out bool value) && value;
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, int tileSize, GameTime gameTime)
        {
            // Draw base terrain first (it handles its own SpriteBatch)
            if (IsVisualizationActive(VisualizationMode.Terrain))
            {
                baseRenderer.Draw(gameTime, camera);
            }
            
            // Begin SpriteBatch for overlays
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            
            // Now draw overlays
            if (IsVisualizationActive(VisualizationMode.Spores))
            {
                DrawSporeOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.FungalSpread))
            {
                DrawFungalOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.Temperature))
            {
                DrawTemperatureOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.Toxicity))
            {
                DrawToxicityOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.AirToxicity))
            {
                DrawAirToxicityOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.Vegetation))
            {
                DrawVegetationOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.SoilMoisture))
            {
                DrawSoilMoistureOverlay(spriteBatch, camera, tileSize);
            }
            
            if (IsVisualizationActive(VisualizationMode.Wind))
            {
                DrawWindOverlay(spriteBatch, camera, tileSize);
            }
            
            // End SpriteBatch for overlays
            spriteBatch.End();
        }

        private void DrawSporeOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null) continue;
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    // Show both seed and non-seed spores with different colors
                    float seedSpores = cell.SeedSporeLoad;
                    float nonseedSpores = cell.NonSeedSporeLoad;
                    
                    if (seedSpores > 0.1f)
                    {
                        // Green overlay for seed spores (higher importance)
                        float seedIntensity = Math.Min(1.0f, seedSpores / 20f); // Scale to reasonable range
                        Color seedColor = Color.Lime * (seedIntensity * 0.7f);
                        
                        Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                        spriteBatch.Draw(pixelTexture, rect, seedColor);
                    }
                    else if (nonseedSpores > 1.0f)
                    {
                        // Orange overlay for non-seed spores (lower priority, shown only if no seed spores)
                        float nonseedIntensity = Math.Min(1.0f, nonseedSpores / 200f); // Scale to reasonable range
                        Color nonseedColor = Color.Orange * (nonseedIntensity * 0.5f);
                        
                        Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                        spriteBatch.Draw(pixelTexture, rect, nonseedColor);
                    }
                }
            }
        }

        private void DrawFungalOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null || cell.FungalMatCover < 0.001f) continue;
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    // Purple overlay with intensity based on fungal biomass
                    float intensity = cell.FungalMatCover;
                    Color fungalColor = Color.Purple * (intensity * 0.6f);
                    
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                    spriteBatch.Draw(pixelTexture, rect, fungalColor);
                }
            }
        }

        private void DrawTemperatureOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null) continue;
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    Color tempColor = GetTemperatureColor(cell.Temperature);
                    tempColor *= 0.4f; // Semi-transparent overlay
                    
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                    spriteBatch.Draw(pixelTexture, rect, tempColor);
                }
            }
        }

        private void DrawToxicityOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null) continue;
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    // Enhanced visibility red overlay
                    // Toxicity 0 = 30% opacity (light pink - clearly visible)
                    // Toxicity 3 = 100% opacity (solid dark red)
                    float toxicity = Math.Clamp(cell.Toxicity, 0f, 3f);
                    float alpha = 0.3f + (toxicity / 3.0f) * 0.7f; // 0.3 to 1.0
                    
                    // Pure red - same color for all, only transparency varies
                    Color toxicColor = Color.Red * alpha;
                    
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                    spriteBatch.Draw(pixelTexture, rect, toxicColor);
                }
            }
        }

        private void DrawAirToxicityOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null || cell.AirToxicity < 0.02f) continue; // Lower threshold for visibility
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    // Bright yellow/orange overlay for air toxicity (more visible)
                    float intensity = Math.Min(1.0f, cell.AirToxicity / 2.0f); // Scale to 0-2 for better visibility
                    Color airToxicColor = Color.Lerp(Color.Yellow, Color.OrangeRed, intensity) * (intensity * 0.8f); // Brighter
                    
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                    spriteBatch.Draw(pixelTexture, rect, airToxicColor);
                }
            }
        }

        private void DrawVegetationOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null) continue;
                    
                    // Show fungal mats even with low VegetationIndex, and other vegetation normally
                    bool showCell = cell.ForestCover >= 0.05f || 
                                   (cell.VegetationState == VegetationState.FungalMat && cell.FungalMatCover >= 0.001f);
                    
                    if (!showCell) continue;
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    Color vegColor = cell.VegetationState switch
                    {
                        VegetationState.Grass => Color.LightGreen,
                        VegetationState.Shrub => Color.Green,
                        VegetationState.FungalMat => Color.Purple,      // Purple for fungal mats
                        VegetationState.Forest => Color.DarkGreen,
                        _ => Color.Transparent
                    };
                    
                    // Use appropriate intensity based on vegetation type
                    float intensity = cell.VegetationState == VegetationState.FungalMat ? 
                                     cell.FungalMatCover * 0.8f :  // Use fungal mat cover for mats
                                     cell.ForestCover * 0.5f; // Use forest cover for others
                    
                    vegColor *= intensity;
                    
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                    spriteBatch.Draw(pixelTexture, rect, vegColor);
                }
            }
        }

        private void DrawSoilMoistureOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var bounds = GetVisibleBounds(camera, tileSize);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    var cell = climateSimulation.GetCell(x, y);
                    if (cell == null) continue;
                    
                    Vector2 worldPos = new Vector2(x * tileSize, y * tileSize);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    // Blue overlay with intensity based on soil moisture
                    // SoilMoisture is in m³/m³ (0-0.45), FieldCapacityPct is percentage (8-28)
                    // Convert field capacity to volumetric: (FieldCapacityPct / 100) * 0.45
                    float fieldCapacityVolumetric = (cell.SoilProps.FieldCapacityPct / 100f) * 0.45f;
                    float intensity = Math.Clamp(cell.SoilMoisture / fieldCapacityVolumetric, 0f, 1f);
                    Color moistureColor = Color.CornflowerBlue * (intensity * 0.7f); // 0-70% opacity
                    
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileSize, tileSize);
                    spriteBatch.Draw(pixelTexture, rect, moistureColor);
                }
            }
        }

        private void DrawWindOverlay(SpriteBatch spriteBatch, Camera camera, int tileSize)
        {
            var weather = climateSimulation.CurrentWeather;
            if (weather == null) return;
            
            var bounds = GetVisibleBounds(camera, tileSize);
            
            // Draw wind arrows at regular intervals
            int spacing = Math.Max(2, tileSize / 4);
            
            for (int x = bounds.X; x < bounds.X + bounds.Width; x += spacing)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y += spacing)
                {
                    Vector2 worldPos = new Vector2(x * tileSize + tileSize/2, y * tileSize + tileSize/2);
                    Vector2 screenPos = worldPos - camera.Position;
                    
                    DrawWindArrow(spriteBatch, screenPos, weather.WindDirection, weather.WindSpeed, tileSize);
                }
            }
        }

        private void DrawWindArrow(SpriteBatch spriteBatch, Vector2 position, Vector2 direction, float speed, int tileSize)
        {
            if (direction.Length() == 0) return;
            
            // Arrow length based on wind speed
            float maxSpeed = climateSimulation.Parameters.WindStormMs;
            float arrowLength = (speed / maxSpeed) * tileSize * 0.8f;
            
            Vector2 arrowEnd = position + Vector2.Normalize(direction) * arrowLength;
            
            // Draw arrow shaft
            DrawLine(spriteBatch, position, arrowEnd, Color.Yellow * 0.8f, 2);
            
            // Draw arrowhead
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
            perpendicular = Vector2.Normalize(perpendicular);
            
            Vector2 arrowHead1 = arrowEnd - Vector2.Normalize(direction) * (arrowLength * 0.3f) + perpendicular * (arrowLength * 0.2f);
            Vector2 arrowHead2 = arrowEnd - Vector2.Normalize(direction) * (arrowLength * 0.3f) - perpendicular * (arrowLength * 0.2f);
            
            DrawLine(spriteBatch, arrowEnd, arrowHead1, Color.Yellow * 0.8f, 2);
            DrawLine(spriteBatch, arrowEnd, arrowHead2, Color.Yellow * 0.8f, 2);
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            
            Rectangle rect = new Rectangle(
                (int)start.X,
                (int)start.Y,
                (int)edge.Length(),
                thickness
            );
            
            Vector2 origin = new Vector2(0, thickness / 2f);
            spriteBatch.Draw(pixelTexture, rect, null, color, angle, origin, SpriteEffects.None, 0);
        }

        private Color GetTemperatureColor(float temperature)
        {
            // Temperature color mapping: Blue (cold) -> Green -> Yellow -> Red (hot)
            float normalizedTemp = (temperature + 10f) / 60f; // Map -10°C to 50°C to 0-1
            normalizedTemp = Math.Clamp(normalizedTemp, 0f, 1f);
            
            if (normalizedTemp < 0.33f)
            {
                // Blue to Green
                float t = normalizedTemp / 0.33f;
                return Color.Lerp(Color.Blue, Color.Green, t);
            }
            else if (normalizedTemp < 0.66f)
            {
                // Green to Yellow
                float t = (normalizedTemp - 0.33f) / 0.33f;
                return Color.Lerp(Color.Green, Color.Yellow, t);
            }
            else
            {
                // Yellow to Red
                float t = (normalizedTemp - 0.66f) / 0.34f;
                return Color.Lerp(Color.Yellow, Color.Red, t);
            }
        }

        private Rectangle GetVisibleBounds(Camera camera, int tileSize)
        {
            int startX = Math.Max(0, (int)(camera.Position.X / tileSize) - 1);
            int startY = Math.Max(0, (int)(camera.Position.Y / tileSize) - 1);
            int endX = Math.Min(200, (int)((camera.Position.X + camera.ViewportWidth) / tileSize) + 2); // Assuming max 200 width
            int endY = Math.Min(150, (int)((camera.Position.Y + camera.ViewportHeight) / tileSize) + 2); // Assuming max 150 height
            
            return new Rectangle(startX, startY, endX - startX, endY - startY);
        }

        public string GetVisualizationInfo()
        {
            var weather = climateSimulation.CurrentWeather;
            string info = $"Climate Simulation (Day {climateSimulation.SimulationTime:F1})\n" +
                         $"Temperature: {weather.Temperature:F1}°C\n" +
                         $"Humidity: {weather.Humidity:F1}%\n" +
                         $"Wind: {weather.WindSpeed:F1} m/s\n" +
                         $"Precipitation: {weather.Precipitation:F1} mm/day\n" +
                         $"Average Toxicity: {climateSimulation.GetAverageToxicity():F2}\n" +
                         $"Fungal Coverage: {climateSimulation.GetTotalFungalCover():F1}%\n\n";
            
            info += "Active Overlays:\n";
            foreach (var overlay in activeOverlays)
            {
                if (overlay.Value)
                {
                    info += $"• {overlay.Key}\n";
                }
            }
            
            return info;
        }

        public void Dispose()
        {
            pixelTexture?.Dispose();
        }
    }
}