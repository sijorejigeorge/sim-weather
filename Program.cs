using System;

namespace TerrainGame
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Program: Starting main...");
            
            try
            {
                // Use the climate simulation version
                using (var game = new ClimateGame())
                    game.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
                
            Console.WriteLine("Program: Exiting...");
        }
    }
}