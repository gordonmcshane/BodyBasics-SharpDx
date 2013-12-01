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
        public BodyBasicsGame Game { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            Game = new BodyBasicsGame();

            this.InitializeComponent();

            
            Game.Run(Surface);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Game.Dispose();
        }
    }
}
