using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using Assignment3;
using GeographyTools;

namespace PopulateDatabase
{
    public class Program
    {
        public static void Main()
        {
            using var database = new AppDbContext();

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            //using var connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Database=DataAccessGUIAssignment;Integrated Security=True;");
            //connection.Open();

            // Clear the database.
            foreach (var ticket in database.Tickets)
            {
                database.Tickets.Remove(ticket);
            }
            foreach (var screening in database.Screenings)
            {
                database.Screenings.Remove(screening);
            }
            foreach (var movie in database.Movies)
            {
                database.Movies.Remove(movie);
            }
            foreach (var cinema in database.Cinemas)
            {
                database.Cinemas.Remove(cinema);
            }

            // Load movies.
            string[] movieLines = File.ReadAllLines("SampleMovies.csv");
            foreach (string line in movieLines)
            {
                string[] parts = line.Split(',');
                string title = parts[0];
                string releaseDateString = parts[1];
                string runtimeString = parts[2];
                string posterPath = parts[3];

                int releaseYear = int.Parse(releaseDateString.Split('-')[0]);
                int releaseMonth = int.Parse(releaseDateString.Split('-')[1]);
                int releaseDay = int.Parse(releaseDateString.Split('-')[2]);
                var releaseDate = new DateTime(releaseYear, releaseMonth, releaseDay);

                short runtime = short.Parse(runtimeString);


                Movie newMovie = new Movie()
                {
                    Title = title,
                    ReleaseDate = releaseDate,
                    Runtime = runtime,
                    PosterPath = posterPath
                };
                database.Add(newMovie);
                database.SaveChanges();
            }

            // Load cinemas.
            string[] cinemaLines = File.ReadAllLines("SampleCinemasWithPositions.csv");
            foreach (string line in cinemaLines)
            {
                string[] parts = line.Split(',');
                string city = parts[0];
                string name = parts[1];
                double latitude = double.Parse(parts[2]);
                double longitude = double.Parse(parts[3]);


                var cinema = new Cinema()
                {
                    City = city,
                    Name = name,
                    Coordinate = new Coordinate() { Latitude = latitude, Longitude = longitude }
                };
                database.Add(cinema);
                database.SaveChanges();
            }

            // Generate random screenings.

            // Get all cinema IDs.
            var cinemaIDs = new List<int>();
            {
                foreach (var cinema in database.Cinemas)
                {
                    cinemaIDs.Add(cinema.ID);
                }
            }

            // Get all movie IDs.
            var movieIDs = new List<int>();
            {
                foreach (var movie in database.Movies)
                {
                    movieIDs.Add(movie.ID);
                }
            }

            // Create random screenings for each cinema.
            var random = new Random();
            foreach (int cinemaID in cinemaIDs)
            {
                // Choose a random number of screenings.
                int numberOfScreenings = random.Next(2, 6);
                foreach (int n in Enumerable.Range(0, numberOfScreenings))
                {
                    // Pick a random movie.
                    int movieID = movieIDs[random.Next(movieIDs.Count)];

                    // Pick a random hour and minute.
                    int hour = random.Next(24);
                    double[] minuteOptions = { 0, 10, 15, 20, 30, 40, 45, 50 };
                    double minute = minuteOptions[random.Next(minuteOptions.Length)];
                    var time = TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);

                    // Insert the screening into the Screenings table.
                    Screening screening = new Screening()
                    {
                        MovieID = movieID,
                        CinemaID = cinemaID,
                        Time = time
                    };
                    database.Add(screening);
                    database.SaveChanges();


                }
            }
        }
    }
}
