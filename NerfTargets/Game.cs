using System;
using System.Threading;
using System.Threading.Tasks;
using NerfTargets.Hubs;
using Timer = System.Timers.Timer;

namespace NerfTargets
{
	public class Game
	{
		readonly static Lazy<Game> _instance = new Lazy<Game>(() => new Game());

		public Game()
		{

		}

		public void Start()
		{
			StartCountdown(TimeSpan.FromSeconds(5))
				.ContinueWith(o => PlayGame(TimeSpan.FromSeconds(30)));
		}

		private void PlayGame(TimeSpan gameLength)
		{
			DateTime gameStart = DateTime.Now;
			while (DateTime.Now  - gameStart < gameLength)
			{
				ClientCommunication.Instance.ShowRandomTarget(3);

				Thread.Sleep(TimeSpan.FromSeconds(5));

			}
		}

		private static Task<object> StartCountdown(TimeSpan time)
		{
			var tcs = new TaskCompletionSource<object>();

			int countdown = time.Seconds;
			var timer = new Timer();
			timer.Elapsed += (sender, args) =>
			{
				if (countdown <= 0)
				{
					ClientCommunication.Instance.ShowText("");
					timer.Stop();
					tcs.SetResult(new object());
				}
				else
				{
					ClientCommunication.Instance.ShowText(countdown.ToString());
					countdown--;
				}
			};
			timer.Interval = 1000;
			timer.Start();

			return tcs.Task;
		}

		public static Game Instance
		{
			get { return _instance.Value; }
		}
	}
}