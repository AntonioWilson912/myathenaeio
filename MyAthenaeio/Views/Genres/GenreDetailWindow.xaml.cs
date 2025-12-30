using MyAthenaeio.Models.Entities;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace MyAthenaeio.Views.Genres
{
    /// <summary>
    /// Interaction logic for GenreDetailWindow.xaml
    /// </summary>
    public partial class GenreDetailWindow : Window
    {
        private int _genreId;
        private Genre? _genre;
        public GenreDetailWindow(Genre genre)
        {
            InitializeComponent();
            _genreId = genre.Id;
        }
    }
}
