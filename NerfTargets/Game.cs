using System;
using System.Threading.Tasks;
using System.Timers;
using NerfTargets.Hubs;

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
			StartCountdown(5);
		}

		private static Task<object> StartCountdown(int time)
		{
			var tcs = new TaskCompletionSource<object>();

			int countdown = time;
			var timer = new Timer();
			timer.Elapsed += (sender, args) =>
			{
				ClientCommunication.Instance.ShowText(countdown.ToString());
				if (countdown <= 0)
				{
					timer.Stop();
					tcs.SetResult(new object());
				}
				countdown--;
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