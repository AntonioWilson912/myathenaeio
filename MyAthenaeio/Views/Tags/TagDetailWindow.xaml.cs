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

namespace MyAthenaeio.Views.Tags
{
    /// <summary>
    /// Interaction logic for TagDetailWindow.xaml
    /// </summary>
    public partial class TagDetailWindow : Window
    {
        private int _tagId;
        private Tag? _tag;

        public TagDetailWindow(Tag tag)
        {
            InitializeComponent();
            _tagId = tag.Id;
        }
    }
}
