using System;

namespace SparkUp.Business
{
    /// <summary>
    /// This class was previously used to seed the database with sample data.
    /// All seeding logic has been removed as part of cleanup.
    /// For development or testing purposes, please use SQL scripts in the project root
    /// or create a dedicated seeding tool.
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Legacy method for seeding data into the database.
        /// This method is left as a placeholder and no longer contains active seeding logic.
        /// </summary>
        /// <param name="context">The database context</param>
        public static void SeedData(AppDbContext context)
        {
            // This method has been intentionally emptied as part of cleanup.
            // For development/testing data seeding, refer to SQL scripts in the project root.
            Console.WriteLine("DataSeeder.SeedData was called but contains no active code.");
            Console.WriteLine("For development data, please use the SQL scripts in the project root.");
        }
    }
}
