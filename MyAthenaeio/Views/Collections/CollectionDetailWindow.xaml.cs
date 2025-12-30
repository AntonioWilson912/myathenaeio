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
using MyAthenaeio.Models.Entities;

namespace MyAthenaeio.Views.Collections
{
    /// <summary>
    /// Interaction logic for CollectionDetailWindow.xaml
    /// </summary>
    public partial class CollectionDetailWindow : Window
    {
        private int _collectionId;
        private Collection? _collection;
        public CollectionDetailWindow(Collection collection)
        {
            InitializeComponent();
            _collectionId = collection.Id;
        }
    }
}
