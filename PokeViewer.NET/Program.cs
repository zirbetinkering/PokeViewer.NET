namespace PokeViewer.NET
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainViewer? Viewer = new();
            Application.Run(Viewer);
        }
    }
}