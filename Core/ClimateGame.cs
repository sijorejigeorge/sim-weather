using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace TerrainGame
{
    public class ClimateGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        
        // Original terrain system
        private TerrainGenerator terrainGenerator;
        private TerrainRenderer terrainRenderer;
        
        // New climate simulation overlay
        private ClimateSimulation climateSimulation;
        private ClimateVisualization climateVisualization;
        
        private Camera camera;
        private SpriteFont font;
        private Texture2D pixelTexture;
        
        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;
        private bool isDragging = false;
        private Vector2 lastMousePosition;
        private bool showStats = true;
        private bool showMinimap = true;
        private const int TILE_SIZE = 8;
        private const int MAP_WIDTH = 200;
        private const int MAP_HEIGHT = 150;

        public ClimateGame()
        {
            Console.WriteLine("ClimateGame: Constructor starting...");
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            
            // Set window size
            _graphics.PreferredBackBufferWidth = 1200;
            _graphics.PreferredBackBufferHeight = 900;
            
            Console.WriteLine("ClimateGame: Constructor complete");
        }

        protected override void Initialize()
        {
            Console.WriteLine("ClimateGame: Initialize starting...");
            base.Initialize();

            // Initialize camera
            camera = new Camera(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            
            Console.WriteLine("ClimateGame: Creating terrain generator...");
            // Generate base terrain (preserving original system)
            terrainGenerator = new TerrainGenerator(MAP_WIDTH, MAP_HEIGHT);
            var terrainMap = terrainGenerator.GenerateMap();
            
            Console.WriteLine("ClimateGame: Creating terrain renderer...");
            // Initialize original terrain renderer
            terrainRenderer = new TerrainRenderer(GraphicsDevice, terrainGenerator, TILE_SIZE);
            
            Console.WriteLine("ClimateGame: Creating climate simulation...");
            // Initialize climate simulation (overlays on top of terrain)
            climateSimulation = new ClimateSimulation(terrainGenerator);
            Console.WriteLine("ClimateGame: Climate simulation created");
            
            // Connect terrain renderer to climate simulation for fungal mat display
            terrainRenderer.SetClimateSimulation(climateSimulation);
            
            Console.WriteLine("ClimateGame: Creating climate visualization...");
            // Initialize climate visualization
            climateVisualization = new ClimateVisualization(GraphicsDevice, climateSimulation, terrainRenderer);
            
            // Center camera on map
            camera.Position = new Vector2(
                (MAP_WIDTH * TILE_SIZE - camera.ViewportWidth) / 2,
                (MAP_HEIGHT * TILE_SIZE - camera.ViewportHeight) / 2
            );
            
            Console.WriteLine("Climate Simulation Initialized");
            Console.WriteLine($"Map Size: {MAP_WIDTH} x {MAP_HEIGHT}");
            Console.WriteLine($"Base Terrain Generated with {CountTerrainTypes()} different terrain types");
            Console.WriteLine($"Climate simulation running on top of terrain");
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Try to load font, but continue without it if not found
            try
            {
                // font = Content.Load<SpriteFont>("Fonts/Arial");
            }
            catch
            {
                font = null;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            // Update climate simulation
            if (climateSimulation != null)
            {
                climateSimulation.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            else
            {
                Console.WriteLine("ClimateGame: climateSimulation is null!");
            }

            // Handle input
            HandleInput(keyboardState, mouseState, gameTime);

            // Update camera
            camera.Update(gameTime, terrainGenerator, TILE_SIZE);
            
            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;
            
            base.Update(gameTime);
        }

        private void HandleInput(KeyboardState keyboardState, MouseState mouseState, GameTime gameTime)
        {
            // Camera movement
            Vector2 moveDirection = Vector2.Zero;
            
            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
                moveDirection.Y -= 1;
            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
                moveDirection.Y += 1;
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
                moveDirection.X -= 1;
            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
                moveDirection.X += 1;

            if (moveDirection != Vector2.Zero)
            {
                moveDirection.Normalize();
                camera.Move(moveDirection, gameTime);
            }

            // Mouse controls
            HandleMouseInput(mouseState, gameTime);

            // Visualization toggles (as requested)
            if (keyboardState.IsKeyDown(Keys.D1) && !previousKeyboardState.IsKeyDown(Keys.D1))
                climateVisualization.ToggleVisualization(VisualizationMode.Spores);
            if (keyboardState.IsKeyDown(Keys.D2) && !previousKeyboardState.IsKeyDown(Keys.D2))
                climateVisualization.ToggleVisualization(VisualizationMode.Wind);
            if (keyboardState.IsKeyDown(Keys.D3) && !previousKeyboardState.IsKeyDown(Keys.D3))
                climateVisualization.ToggleVisualization(VisualizationMode.Temperature);
            if (keyboardState.IsKeyDown(Keys.D4) && !previousKeyboardState.IsKeyDown(Keys.D4))
                climateVisualization.ToggleVisualization(VisualizationMode.Toxicity);
            if (keyboardState.IsKeyDown(Keys.D5) && !previousKeyboardState.IsKeyDown(Keys.D5))
                climateVisualization.ToggleVisualization(VisualizationMode.Vegetation);
            if (keyboardState.IsKeyDown(Keys.D6) && !previousKeyboardState.IsKeyDown(Keys.D6))
                climateVisualization.ToggleVisualization(VisualizationMode.SoilMoisture);
            if (keyboardState.IsKeyDown(Keys.D7) && !previousKeyboardState.IsKeyDown(Keys.D7))
                climateVisualization.ToggleVisualization(VisualizationMode.AirToxicity);
            // Key 8 removed - fungal mats show directly on terrain map

            // Simulation speed controls
            if (keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add))
            {
                climateSimulation.SimulationSpeed = Math.Min(10.0f, climateSimulation.SimulationSpeed * 1.05f);
            }
            if (keyboardState.IsKeyDown(Keys.OemMinus) || keyboardState.IsKeyDown(Keys.Subtract))
            {
                climateSimulation.SimulationSpeed = Math.Max(0.1f, climateSimulation.SimulationSpeed * 0.95f);
            }
            if (keyboardState.IsKeyDown(Keys.D0) && !previousKeyboardState.IsKeyDown(Keys.D0))
            {
                climateSimulation.SimulationSpeed = 1.0f;
            }

            // Pause/Resume
            if (keyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space))
            {
                climateSimulation.IsPaused = !climateSimulation.IsPaused;
            }

            // Neutralization (Valley intervention)
            if (mouseState.LeftButton == ButtonState.Pressed && keyboardState.IsKeyDown(Keys.LeftShift))
            {
                Vector2 worldPos = new Vector2(mouseState.X, mouseState.Y) + camera.Position;
                int tileX = (int)(worldPos.X / TILE_SIZE);
                int tileY = (int)(worldPos.Y / TILE_SIZE);
                climateSimulation.ApplyNeutralization(tileX, tileY);
            }

            // Regenerate map
            if (keyboardState.IsKeyDown(Keys.R) && !previousKeyboardState.IsKeyDown(Keys.R))
            {
                terrainGenerator = new TerrainGenerator(MAP_WIDTH, MAP_HEIGHT);
                terrainRenderer = new TerrainRenderer(GraphicsDevice, terrainGenerator, TILE_SIZE);
                climateSimulation = new ClimateSimulation(terrainGenerator);
                terrainRenderer.SetClimateSimulation(climateSimulation);
                climateVisualization = new ClimateVisualization(GraphicsDevice, climateSimulation, terrainRenderer);
            }

            // Toggle stats
            if (keyboardState.IsKeyDown(Keys.F1) && !previousKeyboardState.IsKeyDown(Keys.F1))
                showStats = !showStats;

            // Toggle minimap
            if (keyboardState.IsKeyDown(Keys.F2) && !previousKeyboardState.IsKeyDown(Keys.F2))
                showMinimap = !showMinimap;
        }

        private void HandleMouseInput(MouseState mouseState, GameTime gameTime)
        {
            // Mouse wheel for camera speed
            if (mouseState.ScrollWheelValue != previousMouseState.ScrollWheelValue)
            {
                float speedChange = (mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue) * 0.01f;
                camera.Speed = Math.Max(50f, Math.Min(1000f, camera.Speed + speedChange));
            }

            // Mouse drag for camera movement
            if (mouseState.LeftButton == ButtonState.Pressed && !Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                if (!isDragging)
                {
                    isDragging = true;
                    lastMousePosition = new Vector2(mouseState.X, mouseState.Y);
                }
                else
                {
                    Vector2 currentMousePosition = new Vector2(mouseState.X, mouseState.Y);
                    Vector2 mouseDelta = lastMousePosition - currentMousePosition;
                    camera.Position += mouseDelta;
                    lastMousePosition = currentMousePosition;
                }
            }
            else
            {
                isDragging = false;
            }

            // Right click to jump to position
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
                camera.Position = mousePos - new Vector2(camera.ViewportWidth / 2, camera.ViewportHeight / 2);
            }

            // Middle click to center on map
            if (mouseState.MiddleButton == ButtonState.Pressed && previousMouseState.MiddleButton == ButtonState.Released)
            {
                camera.Position = new Vector2(
                    (MAP_WIDTH * TILE_SIZE - camera.ViewportWidth) / 2,
                    (MAP_HEIGHT * TILE_SIZE - camera.ViewportHeight) / 2
                );
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Draw terrain and climate visualization
            climateVisualization.Draw(_spriteBatch, camera, TILE_SIZE, gameTime);

            // Begin SpriteBatch for UI elements
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            // Draw UI
            if (showStats)
            {
                DrawUI();
            }

            // Draw minimap if enabled
            if (showMinimap)
            {
                DrawMinimap();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawUI()
        {
            // Draw hotkeys with toggle status
            DrawHotkeyList();
            
            // Draw climate simulation info
            if (font != null)
            {
                string climateInfo = climateVisualization.GetVisualizationInfo();
                _spriteBatch.DrawString(font, climateInfo, new Vector2(10, 450), Color.Cyan);
            }
        }

        private void DrawHotkeyList()
        {
            // Create pixel texture for drawing (reuse if possible)
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
            }

            // Draw visualization overlays as colored rectangles with status indicators
            int startX = 10;
            int startY = 10;
            int spacing = 25;
            int rectWidth = 200;
            int rectHeight = 20;
            
            // Title bar
            Rectangle titleRect = new Rectangle(startX, startY, rectWidth + 150, rectHeight);
            _spriteBatch.Draw(pixelTexture, titleRect, Color.DarkBlue * 0.8f);
            
            int currentY = startY + spacing + 10;
            
            // Draw each visualization mode
            DrawVisualizationBar("1 - Spores", VisualizationMode.Spores, startX, ref currentY, Color.Lime);
            DrawVisualizationBar("2 - Wind Flow", VisualizationMode.Wind, startX, ref currentY, Color.CornflowerBlue);
            DrawVisualizationBar("3 - Temperature", VisualizationMode.Temperature, startX, ref currentY, Color.Orange);
            DrawVisualizationBar("4 - Soil Toxicity", VisualizationMode.Toxicity, startX, ref currentY, Color.Red);
            DrawVisualizationBar("5 - Vegetation", VisualizationMode.Vegetation, startX, ref currentY, Color.Green);
            DrawVisualizationBar("6 - Soil Moisture", VisualizationMode.SoilMoisture, startX, ref currentY, Color.Blue);
            DrawVisualizationBar("7 - Air Toxicity", VisualizationMode.AirToxicity, startX, ref currentY, Color.Orange);
            // Fungal mats shown directly on terrain map (no overlay needed)
            
            // Add spacing and system status
            currentY += 10;
            
            // System status
            Rectangle systemRect = new Rectangle(startX, currentY, rectWidth, rectHeight);
            Color systemColor = climateSimulation.IsPaused ? Color.Red : Color.Green;
            _spriteBatch.Draw(pixelTexture, systemRect, systemColor * 0.6f);
            
            currentY += spacing;
            
            // Speed indicator 
            Rectangle speedRect = new Rectangle(startX, currentY, (int)(rectWidth * (climateSimulation.SimulationSpeed / 3.0f)), rectHeight / 2);
            _spriteBatch.Draw(pixelTexture, speedRect, Color.Yellow * 0.8f);
        }

        private void DrawVisualizationToggle(string text, VisualizationMode mode, ref Vector2 position, Color layerColor)
        {
            if (font == null) return;
            
            bool isActive = climateVisualization.IsVisualizationActive(mode);
            Color textColor = isActive ? Color.White : Color.Gray;
            
            // Draw status indicator
            Rectangle indicator = new Rectangle((int)position.X, (int)position.Y + 2, 12, 12);
            Texture2D pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            Color indicatorColor = isActive ? layerColor : Color.DarkGray;
            _spriteBatch.Draw(pixelTexture, indicator, indicatorColor);
            
            // Draw text
            Vector2 textPos = new Vector2(position.X + 18, position.Y);
            _spriteBatch.DrawString(font, text, textPos, textColor);
            
            position.Y += 22;
        }

        private void DrawVisualizationBar(string text, VisualizationMode mode, int x, ref int y, Color layerColor)
        {
            bool isActive = climateVisualization.IsVisualizationActive(mode);
            
            // Background bar
            Rectangle bgRect = new Rectangle(x, y, 180, 20);
            Color bgColor = isActive ? layerColor * 0.6f : Color.Gray * 0.3f;
            _spriteBatch.Draw(pixelTexture, bgRect, bgColor);
            
            // Status indicator (small square)
            Rectangle indicator = new Rectangle(x + 185, y + 2, 16, 16);
            Color indicatorColor = isActive ? layerColor : Color.DarkGray;
            _spriteBatch.Draw(pixelTexture, indicator, indicatorColor);
            
            y += 25;
        }

        private void DrawMinimap()
        {
            // Simple minimap in bottom-right corner
            int minimapSize = 150;
            int minimapX = _graphics.PreferredBackBufferWidth - minimapSize - 10;
            int minimapY = _graphics.PreferredBackBufferHeight - minimapSize - 10;
            
            Rectangle minimapBounds = new Rectangle(minimapX, minimapY, minimapSize, minimapSize);
            
            // Draw minimap background
            Texture2D pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            _spriteBatch.Draw(pixelTexture, minimapBounds, Color.Black * 0.5f);
            
            // Draw camera position indicator
            Vector2 cameraRatio = new Vector2(
                camera.Position.X / (MAP_WIDTH * TILE_SIZE),
                camera.Position.Y / (MAP_HEIGHT * TILE_SIZE)
            );
            
            Vector2 minimapCameraPos = new Vector2(
                minimapX + cameraRatio.X * minimapSize,
                minimapY + cameraRatio.Y * minimapSize
            );
            
            Rectangle cameraIndicator = new Rectangle(
                (int)minimapCameraPos.X - 2,
                (int)minimapCameraPos.Y - 2,
                4, 4
            );
            
            _spriteBatch.Draw(pixelTexture, cameraIndicator, Color.Red);
        }

        private int CountTerrainTypes()
        {
            var typesFound = new System.Collections.Generic.HashSet<TerrainType>();
            
            for (int x = 0; x < MAP_WIDTH; x++)
            {
                for (int y = 0; y < MAP_HEIGHT; y++)
                {
                    var tile = terrainGenerator.GetTile(x, y);
                    if (tile != null)
                    {
                        typesFound.Add(tile.Type);
                    }
                }
            }
            
            return typesFound.Count;
        }

        protected override void UnloadContent()
        {
            climateVisualization?.Dispose();
            base.UnloadContent();
        }
    }
}