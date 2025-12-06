using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAthenaeio.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            try
            {
                using var context = new AppDbContext();

                // Create database and apply migrations
                context.Database.Migrate();

                Debug.WriteLine($"Database initialized at: {AppDbContext.DbPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        public static bool DatabaseExists()
        {
            return System.IO.File.Exists(AppDbContext.DbPath);
        }
    }
}
