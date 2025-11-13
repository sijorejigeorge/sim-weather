using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TerrainGame
{
    public class TerrainRenderer
    {
        private SpriteBatch spriteBatch;
        private Texture2D pixelTexture;
        private Dictionary<TerrainType, Color> terrainColors;
        private TerrainGenerator terrainGenerator;
        private ClimateSimulation climateSimulation;
        private int tileSize;

        public TerrainRenderer(GraphicsDevice graphicsDevice, TerrainGenerator generator, int tileSize = 8)
        {
            this.terrainGenerator = generator;
            this.tileSize = tileSize;
            
            spriteBatch = new SpriteBatch(graphicsDevice);
            
            // Create a 1x1 white pixel texture for drawing colored squares
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Define colors for each terrain type
            terrainColors = new Dictionary<TerrainType, Color>
            {
                { TerrainType.Desert, new Color(238, 203, 173) },     // Sandy beige
                { TerrainType.Water, new Color(64, 164, 223) },       // Blue
                { TerrainType.Grassland, new Color(124, 252, 0) },    // Lawn green
                { TerrainType.Forest, new Color(34, 139, 34) },       // Forest green
                { TerrainType.Plateau, new Color(205, 170, 125) },    // Light brown
                { TerrainType.Canyon, new Color(139, 69, 19) }        // Dark brown
            };
        }

        public void SetClimateSimulation(ClimateSimulation simulation)
        {
            this.climateSimulation = simulation;
        }

        public void Draw(GameTime gameTime, Camera camera = null)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            // Calculate which tiles are visible on screen
            int startX = 0;
            int startY = 0;
            int endX = terrainGenerator.MapWidth;
            int endY = terrainGenerator.MapHeight;

            if (camera != null)
            {
                startX = (int)MathHelper.Max(0, camera.Position.X / tileSize);
                startY = (int)MathHelper.Max(0, camera.Position.Y / tileSize);
                endX = (int)MathHelper.Min(terrainGenerator.MapWidth, 
                    (camera.Position.X + camera.ViewportWidth) / tileSize + 1);
                endY = (int)MathHelper.Min(terrainGenerator.MapHeight, 
                    (camera.Position.Y + camera.ViewportHeight) / tileSize + 1);
            }

            // Draw visible terrain tiles
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    var tile = terrainGenerator.GetTile(x, y);
                    if (tile != null)
                    {
                        Color color;
                        
                        // Check for fungal mats first (if climate simulation available)
                        if (climateSimulation != null)
                        {
                            var cell = climateSimulation.GetCell(x, y);
                            if (cell != null && cell.VegetationState == VegetationState.FungalMat && cell.FungalMatCover >= 0.001f)
                            {
                                // Show fungal mats as purple with intensity based on coverage
                                float intensity = MathHelper.Clamp(cell.FungalMatCover, 0f, 1f);
                                color = Color.Lerp(terrainColors[tile.Type], Color.Purple, intensity * 0.8f);
                            }
                            else
                            {
                                // Apply shadow effects only to plateaus for elevated look
                                if (tile.Type == TerrainType.Plateau)
                                {
                                    color = CalculatePlateauShadow(tile, x, y);
                                }
                                else
                                {
                                    color = terrainColors[tile.Type];
                                }
                            }
                        }
                        else
                        {
                            // Apply shadow effects only to plateaus for elevated look
                            if (tile.Type == TerrainType.Plateau)
                            {
                                color = CalculatePlateauShadow(tile, x, y);
                            }
                            else
                            {
                                color = terrainColors[tile.Type];
                            }
                        }
                        
                        var position = new Vector2(x * tileSize, y * tileSize);
                        
                        if (camera != null)
                        {
                            position -= camera.Position;
                        }

                        var rectangle = new Rectangle((int)position.X, (int)position.Y, tileSize, tileSize);
                        spriteBatch.Draw(pixelTexture, rectangle, color);

                        // Add subtle border for better visibility
                        DrawBorder(rectangle, Color.Black * 0.1f);
                    }
                }
            }

            spriteBatch.End();
        }

        public void DrawMinimap(Vector2 position, int minimapWidth, int minimapHeight)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            float scaleX = (float)minimapWidth / terrainGenerator.MapWidth;
            float scaleY = (float)minimapHeight / terrainGenerator.MapHeight;

            for (int x = 0; x < terrainGenerator.MapWidth; x++)
            {
                for (int y = 0; y < terrainGenerator.MapHeight; y++)
                {
                    var tile = terrainGenerator.GetTile(x, y);
                    if (tile != null)
                    {
                        Color color;
                        
                        // Check for fungal mats first (if climate simulation available)
                        if (climateSimulation != null)
                        {
                            var cell = climateSimulation.GetCell(x, y);
                            if (cell != null && cell.VegetationState == VegetationState.FungalMat && cell.FungalMatCover >= 0.001f)
                            {
                                // Show fungal mats as purple in minimap too
                                float intensity = MathHelper.Clamp(cell.FungalMatCover, 0f, 1f);
                                color = Color.Lerp(terrainColors[tile.Type], Color.Purple, intensity * 0.8f);
                            }
                            else
                            {
                                // Apply shadow effects only to plateaus in minimap too
                                if (tile.Type == TerrainType.Plateau)
                                {
                                    color = CalculatePlateauShadow(tile, x, y);
                                }
                                else
                                {
                                    color = terrainColors[tile.Type];
                                }
                            }
                        }
                        else
                        {
                            // Apply shadow effects only to plateaus in minimap too
                            if (tile.Type == TerrainType.Plateau)
                            {
                                color = CalculatePlateauShadow(tile, x, y);
                            }
                            else
                            {
                                color = terrainColors[tile.Type];
                            }
                        }
                        
                        var tilePosition = new Vector2(
                            position.X + x * scaleX,
                            position.Y + y * scaleY);

                        var rectangle = new Rectangle(
                            (int)tilePosition.X, 
                            (int)tilePosition.Y, 
                            (int)MathHelper.Max(1, scaleX), 
                            (int)MathHelper.Max(1, scaleY));

                        spriteBatch.Draw(pixelTexture, rectangle, color);
                    }
                }
            }

            // Draw minimap border
            DrawBorder(new Rectangle((int)position.X, (int)position.Y, minimapWidth, minimapHeight), Color.White);

            spriteBatch.End();
        }

        private Color CalculatePlateauShadow(TerrainTile tile, int x, int y)
        {
            var baseColor = terrainColors[tile.Type];
            
            // Light source coming from northwest (top-left)
            Vector2 lightDirection = new Vector2(-0.7f, -0.7f);
            
            float shadowEffect = 1.2f; // Start with elevated brightness for plateau
            
            // Check neighboring tiles to create shadow effects
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int checkX = x + dx;
                    int checkY = y + dy;
                    
                    var neighborTile = terrainGenerator.GetTile(checkX, checkY);
                    if (neighborTile != null)
                    {
                        // If neighbor is not a plateau, this plateau should be brighter (elevated)
                        if (neighborTile.Type != TerrainType.Plateau)
                        {
                            Vector2 neighborDirection = new Vector2(dx, dy);
                            if (neighborDirection.Length() > 0)
                            {
                                neighborDirection.Normalize();
                                
                                // Calculate alignment with light direction
                                float alignment = Vector2.Dot(neighborDirection, lightDirection);
                                
                                // Create highlight effect on plateau edges facing the light
                                if (alignment > 0.3f)
                                {
                                    shadowEffect += 0.15f; // Brighter on light-facing edges
                                }
                                else if (alignment < -0.3f)
                                {
                                    shadowEffect -= 0.1f; // Slightly darker on shadow-facing edges
                                }
                            }
                        }
                    }
                }
            }
            
            // Clamp the shadow effect
            shadowEffect = MathHelper.Clamp(shadowEffect, 0.7f, 1.5f);
            
            // Apply the effect to the base color
            return new Color(
                (int)(baseColor.R * shadowEffect),
                (int)(baseColor.G * shadowEffect),
                (int)(baseColor.B * shadowEffect),
                baseColor.A
            );
        }

        private void DrawBorder(Rectangle rectangle, Color color)
        {
            // Top
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), color);
            // Bottom
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - 1, rectangle.Width, 1), color);
            // Left
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), color);
            // Right
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.X + rectangle.Width - 1, rectangle.Y, 1, rectangle.Height), color);
        }

        public void DrawTerrainStats(SpriteFont font, Vector2 position)
        {
            spriteBatch.Begin();

            var stats = terrainGenerator.GetTerrainStats();
            int totalTiles = terrainGenerator.MapWidth * terrainGenerator.MapHeight;

            float yOffset = 0;
            foreach (var kvp in stats)
            {
                float percentage = (float)kvp.Value / totalTiles * 100f;
                string text = $"{kvp.Key}: {kvp.Value} tiles ({percentage:F1}%)";
                
                var textPosition = position + new Vector2(0, yOffset);
                spriteBatch.DrawString(font, text, textPosition, terrainColors[kvp.Key]);
                
                yOffset += font.LineSpacing;
            }

            spriteBatch.End();
        }

        public Color GetTerrainColor(TerrainType terrainType)
        {
            return terrainColors.ContainsKey(terrainType) ? terrainColors[terrainType] : Color.White;
        }

        public void Dispose()
        {
            spriteBatch?.Dispose();
            pixelTexture?.Dispose();
        }
    }

    // Simple camera class for viewport management
    public class Camera
    {
        public Vector2 Position { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }
        public float Speed { get; set; } = 200f;

        public Camera(int viewportWidth, int viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            Position = Vector2.Zero;
        }

        public void Update(GameTime gameTime, TerrainGenerator terrainGenerator, int tileSize)
        {
            // Keep camera within map bounds
            float maxX = terrainGenerator.MapWidth * tileSize - ViewportWidth;
            float maxY = terrainGenerator.MapHeight * tileSize - ViewportHeight;

            Position = new Vector2(
                MathHelper.Clamp(Position.X, 0, MathHelper.Max(0, maxX)),
                MathHelper.Clamp(Position.Y, 0, MathHelper.Max(0, maxY))
            );
        }

        public void Move(Vector2 direction, GameTime gameTime)
        {
            Position += direction * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}