using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using SharpDX.Toolkit;

namespace BodyBasicsSharpDx
{
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        private Game _game;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // initialize the components (controls) of the window
            this.InitializeComponent();

            _game = new BodyBasicsGame();
            _game.Run(surface);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _game.Dispose();
        }
    }
}
