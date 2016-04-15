namespace Gambit.Client
{
	using System;
	using System.Threading.Tasks;
	using System.Windows;

	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class About : Window
	{

		private const int IntroTimeDelay = 3000; // in miliseconds
		public About()
		{
			InitializeComponent();

			Task.Delay(IntroTimeDelay)
				.ContinueWith(t => StartMainWindow());

		}

		private void StartMainWindow()
		{

			Application.Current.Dispatcher.Invoke((Action) delegate {
				MainWindow main = new MainWindow();
				main.Show();
				this.Close();
			});
			
		}
	}
}
