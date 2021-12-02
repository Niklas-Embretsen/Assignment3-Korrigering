using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GeographyTools;
using Windows.Devices.Geolocation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment3
{
    public class Cinema
    {
        [Required]
        public int ID { get; set; }
        [Required, MaxLength(255)]
        public string Name { get; set; }
        [Required, MaxLength(255),]
        public string City { get; set; }
        [Required]
        public Coordinate Coordinate { get; set; }

    }

    public class Movie
    {
        [Required]
        public int ID { get; set; }
        [Required, MaxLength(255)]
        public string Title { get; set; }
        [Required]
        public Int16 Runtime { get; set; }
        [Required, Column(TypeName = "date")]
        public DateTime ReleaseDate { get; set; }
        [Required, MaxLength(255)]
        public string PosterPath { get; set; }
    }

    public class Screening
    {
        [Required]
        public int ID { get; set; }
        [Required, Column(TypeName = "time(0)")]
        public TimeSpan Time { get; set; }
        [Required]
        public int MovieID { get; set; }
        [Required]
        public Movie Movie { get; set; }
        [Required]
        public int CinemaID { get; set; }
        [Required]
        public Cinema Cinema { get; set; }
    }

    public class Ticket
    {
        [Required]
        public int ID { get; set; }
        [Required]
        public int ScreeningID { get; set; }
        [Required]
        public Screening Screening { get; set; }
        [Required, Column(TypeName = "datetime")]
        public DateTime TimePurchased { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Screening> Screenings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Cinema>()
                .HasIndex(c => new { c.Name })
                .IsUnique(true);

            model.Entity<Cinema>().OwnsOne(cinema => cinema.Coordinate, coordinate =>
            {
                coordinate.Property(coordinate => coordinate.Longitude).HasColumnName("Longitude");
                coordinate.Property(coordinate => coordinate.Latitude).HasColumnName("Latitude");
                coordinate.Ignore("Altitude");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Users\nikla\Assignment 3 TestDatabase.mdf"";Integrated Security=True;Connect Timeout=30");
        }
    }

    public partial class MainWindow : Window
    {
        private Thickness spacing = new Thickness(5);
        private FontFamily mainFont = new FontFamily("Constantia");

        // Some GUI elements that we need to access in multiple methods.
        private ComboBox cityComboBox;
        private ListBox cinemaListBox;
        private StackPanel screeningPanel;
        private StackPanel ticketPanel;

        // An SQL connection that we will keep open for the entire program.
        //private SqlConnection connection;
        //***

        public MainWindow()
        {
            InitializeComponent();
            Start();
            cityComboBox.Items.Add("Cinemas within 100 km");

        }

        private async Task<List<Cinema>> GeolocationAsync(Task<Coordinate> coordTask)
            //GetCinemasWithin100km_Async***
            //GetCinemas100km_Async
        {
            using var database = new AppDbContext();
            List<Cinema> cinemaList = new List<Cinema>();
            var coordinate = await coordTask;

            foreach (var cinema in database.Cinemas)
            {
                if (Geography.Distance(coordinate, cinema.Coordinate) <= 100000)
                {
                    cinemaList.Add(cinema);
                }
            }
            return cinemaList;
        }

        private async Task<Coordinate> LocationTaskAsync()
            //GetUserPosition_Async***
            //GetPosition_Async
        {

            GeolocationAccessStatus acesStatus = await Geolocator.RequestAccessAsync();
            //Antagligen så kanske man kan ta bort accessStatus eller ***
            //if (accessStatus == GeolocationAccessStatus.Allowed)
            //{
            //    Geoposition geoposition = await new Geolocator().GetGeopositionAsync();
            //    return new Coordinate()
            //    {
            //        Latitude = geoposition.Coordinate.Latitude,
            //        Longitude = geoposition.Coordinate.Longitude
            //    };
            //}
            //else
            //{
            //    throw new Exception("Couldnt access user position");
            //}

            Geoposition geoposition = await new Geolocator().GetGeopositionAsync();
            return new Coordinate()
            {
                Latitude = geoposition.Coordinate.Latitude,
                Longitude = geoposition.Coordinate.Longitude
            };
        }

        private void Start()
        {
            //connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Database=DataAccessGUIAssignment;Integrated Security=True;");
            //connection.Open();
            //***

            // Window options
            Title = "Cinemania";
            Width = 1000;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Black;

            // Main grid
            var grid = new Grid();
            Content = grid;
            grid.Margin = spacing;
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            AddToGrid(grid, CreateCinemaGUI(), 0, 0);
            AddToGrid(grid, CreateScreeningGUI(), 0, 1);
            AddToGrid(grid, CreateTicketGUI(), 0, 2);
        }

        // Create the cinema part of the GUI: the left column.
        private UIElement CreateCinemaGUI()
        {
            var grid = new Grid
            {
                MinWidth = 200
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Cinema",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            // Create the dropdown of cities.
            cityComboBox = new ComboBox
            {
                Margin = spacing
            };
            foreach (string city in GetCities())
            {
                cityComboBox.Items.Add(city);
            }
            cityComboBox.SelectedIndex = 0;
            AddToGrid(grid, cityComboBox, 1, 0);

            // When we select a city, update the GUI with the cinemas in the currently selected city.
            cityComboBox.SelectionChanged += (sender, e) =>
            {
                UpdateCinemaList();
            };

            // Create the dropdown of cinemas.
            cinemaListBox = new ListBox
            {
                Margin = spacing
            };
            AddToGrid(grid, cinemaListBox, 2, 0);
            UpdateCinemaList();

            // When we select a cinema, update the GUI with the screenings in the currently selected cinema.
            cinemaListBox.SelectionChanged += (sender, e) =>
            {
                UpdateScreeningList();
            };

            return grid;
        }

        // Create the screening part of the GUI: the middle column.
        private UIElement CreateScreeningGUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Screening",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            screeningPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = screeningPanel;

            UpdateScreeningList();

            return grid;
        }

        // Create the ticket part of the GUI: the right column.
        private UIElement CreateTicketGUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "My Tickets",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            ticketPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = ticketPanel;

            // Update the GUI with the initial list of tickets.
            UpdateTicketList();

            return grid;
        }

        // Get a list of all cities that have cinemas in them.
        private IEnumerable<string> GetCities()
        {
            using AppDbContext database = new AppDbContext();
            List<string> cities = new List<string>();
            foreach (var city in database.Cinemas.Select(c => c.City).Distinct())
            {
                cities.Add(city);
            }
            return cities;
        }




        // Get a list of all cinemas in the currently selected city.
        private async Task<IEnumerable<string>> GetCinemasInSelectedCity()
        {
            using var database = new AppDbContext();

            if (cityComboBox.SelectedItem.ToString() == "Cinemas within 100 km")
            {
                var cinemaList = await GeolocationAsync(LocationTaskAsync());
                List<string> cinemas = new List<string>();
                foreach (var cinema in cinemaList)
                {
                    cinemas.Add(cinema.Name);
                }
                return cinemas;
            }
            else
            {
                var cinemas = new List<string>();
                foreach (var cinema in database.Cinemas.Where(c => c.City == cityComboBox.SelectedItem))
                {
                    cinemas.Add(cinema.Name);

                }
                return cinemas;

            }
        }

        // Update the GUI with the cinemas in the currently selected city.
        private async void UpdateCinemaList()
        {
            cinemaListBox.Items.Clear();
            foreach (string cinema in await GetCinemasInSelectedCity())
            {
                cinemaListBox.Items.Add(cinema);
            }
        }

        // Update the GUI with the screenings in the currently selected cinema.
        private void UpdateScreeningList()
        {
            using var database = new AppDbContext();

            screeningPanel.Children.Clear();

            screeningPanel.Children.Clear();
            if (cinemaListBox.SelectedIndex == -1)
            {
                return;
            }

            foreach (var screening in database.Screenings.Where(s => s.Cinema.Name == (string)cinemaListBox.SelectedItem)
                .Include(s => s.Movie)
                .Include(s => s.Cinema))
                //Kanske inte behöver Cinema ***
                //ScreeningGUI:n använder nog ingen info om Cinema
                //Man kan ju redan se vilken Cinema som är markerad i
                //CinemaGUI:n
            {
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = spacing,
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                screeningPanel.Children.Add(button);

                button.Click += (sender, e) =>
                {
                    BuyTicket(screening.ID);
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                button.Content = grid;

                var image = CreateImage(@"Posters\" + screening.Movie.PosterPath);
                image.Width = 50;
                image.Margin = spacing;
                image.ToolTip = new ToolTip { Content = screening.Movie.PosterPath };
                AddToGrid(grid, image, 0, 0);
                Grid.SetRowSpan(image, 3);

                var time = screening.Time;
                var timeHeading = new TextBlock
                {
                    Text = TimeSpanToString(time),
                    Margin = spacing,
                    FontFamily = new FontFamily("Corbel"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Yellow
                };
                AddToGrid(grid, timeHeading, 0, 1);

                var titleHeading = new TextBlock
                {
                    Text = screening.Movie.Title,
                    Margin = spacing,
                    FontFamily = mainFont,
                    FontSize = 16,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, titleHeading, 1, 1);

                var releaseDate = screening.Movie.ReleaseDate;
                int runtimeMinutes = screening.Movie.Runtime;
                var runtime = TimeSpan.FromMinutes(runtimeMinutes);
                string runtimeString = runtime.Hours + "h " + runtime.Minutes + "m";
                var details = new TextBlock
                {
                    Text = "📆 " + releaseDate.Year + "     ⏳ " + runtimeString,
                    Margin = spacing,
                    FontFamily = new FontFamily("Corbel"),
                    Foreground = Brushes.Silver
                };
                AddToGrid(grid, details, 2, 1);
            }



            //original code below***
            //Antar att man kan ta bort den här kommentaren

        }

        // Buy a ticket for the specified screening and update the GUI with the latest list of tickets.
        private void BuyTicket(int screeningID)
        {
            using var database = new AppDbContext();

            // First check if we already have a ticket for this screening.
            int count = database.Tickets.Where(t => t.ScreeningID == screeningID).Count();

            // If we don't, add it.
            if (count == 0)
            {
                Ticket ticket = new Ticket()
                {
                    ScreeningID = screeningID,
                    TimePurchased = DateTime.Now
                };
                database.Add(ticket);
                database.SaveChanges();
                UpdateTicketList();
            }
        }

        // Update the GUI with the latest list of tickets
        private void UpdateTicketList()
        {
            ticketPanel.Children.Clear();

            using var database = new AppDbContext();
            foreach (var ticket in database.Tickets
                .Include(t => t.Screening)
                .Include(t => t.Screening.Cinema)
                .Include(t => t.Screening.Movie))
            {
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = spacing,
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                ticketPanel.Children.Add(button);
                button.Click += (sender, e) =>
                {
                    RemoveTicket(ticket.ID);
                };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                button.Content = grid;

                var image = CreateImage(@"Posters\" + ticket.Screening.Movie.PosterPath);
                image.Width = 30;
                image.Margin = spacing;
                image.ToolTip = new ToolTip { Content = ticket.Screening.Movie.Title };
                AddToGrid(grid, image, 0, 0);
                Grid.SetRowSpan(image, 2);

                var titleHeading = new TextBlock
                {
                    Text = Convert.ToString(ticket.Screening.Movie.Title),
                    Margin = spacing,
                    FontFamily = mainFont,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, titleHeading, 0, 1);

                var time = ticket.Screening.Time;
                string timeString = TimeSpanToString(time);
                var timeAndCinemaHeading = new TextBlock
                {
                    Text = timeString + " - " + ticket.Screening.Cinema.Name,
                    Margin = spacing,
                    FontFamily = new FontFamily("Corbel"),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Yellow,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, timeAndCinemaHeading, 1, 1);

            }

        }

        // Remove the ticket for the specified screening and update the GUI with the latest list of tickets.
        private void RemoveTicket(int ticketID)
        {
            using var database = new AppDbContext();
            database.Remove(database.Tickets.First(t => t.ID == ticketID));
            database.SaveChanges();

            UpdateTicketList();

        }

        // Helper method to add a GUI element to the specified row and column in a grid.
        private void AddToGrid(Grid grid, UIElement element, int row, int column)
        {
            grid.Children.Add(element);
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
        }

        // Helper method to create a high-quality image for the GUI.
        private Image CreateImage(string filePath)
        {
            ImageSource source = new BitmapImage(new Uri(filePath, UriKind.RelativeOrAbsolute));
            Image image = new Image
            {
                Source = source,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        // Helper method to turn a TimeSpan object into a string, such as 2:05.
        private string TimeSpanToString(TimeSpan timeSpan)
        {
            string hourString = timeSpan.Hours.ToString().PadLeft(2, '0');
            string minuteString = timeSpan.Minutes.ToString().PadLeft(2, '0');
            string timeString = hourString + ":" + minuteString;
            return timeString;
        }
    }
}
