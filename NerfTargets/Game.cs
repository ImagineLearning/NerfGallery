using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NerfTargets.Hubs;
using Timer = System.Timers.Timer;

namespace NerfTargets
{
	public class Game
	{
		readonly static Lazy<Game> _instance = new Lazy<Game>(() => new Game());
		private IHubContext _scoreHub;

		public int hits = 0;
		public int misses = 0;
		public Game()
		{
			ClientCommunication.Instance.GoodHit += (sender, args) =>
			{
				hits++;
				UpdateScores();
			};

			ClientCommunication.Instance.BadHit += (sender, args) =>
			{
				misses++;
				UpdateScores();
			};
			_scoreHub = GlobalHost.ConnectionManager.GetHubContext<ScoreHub>();
		}

		private void UpdateScores()
		{
			_scoreHub.Clients.All.updateScores(hits, misses);
		}

		private void ShowNewTarget()
		{
			ClientCommunication.Instance.ShowRandomTarget(3);
		}

		public void Start()
		{
			hits = 0;
			misses = 0;
			UpdateScores();

			Task.Run(() => GameThread());
		}

		private void GameThread()
		{
			Part1();
			Part2();
			GameOver();
		}

		private void GameOver()
		{
			ClientCommunication.Instance.LevelEnd("part1");
			ClientCommunication.Instance.HideAllTargets();
			ClientCommunication.Instance.GameOver(hits);
		}



		private void Part1()
		{
			var levelName = "part1";
			ClientCommunication.Instance.RestartGame();
			ClientCommunication.Instance.LevelStart(levelName);
			Thread.Sleep(TimeSpan.FromSeconds(6));
			Countdown(TimeSpan.FromSeconds(5));
			
			var targetIds = ClientCommunication.Instance.GetConnectedTargetIds();
			foreach(var targetId in targetIds)
			{
				ClientCommunication.Instance.ShowTargetByTargetNum(targetId);
			}

			while (hits < targetIds.Count)
			{
				Thread.Sleep(100);
			}

			ClientCommunication.Instance.LevelEnd(levelName);
			Thread.Sleep(TimeSpan.FromSeconds(8));
		}

		private void Part2()
		{
			var levelName = "part2";
			ClientCommunication.Instance.RestartGame();
			ClientCommunication.Instance.LevelStart(levelName);
			Thread.Sleep(TimeSpan.FromSeconds(4));
			Countdown(TimeSpan.FromSeconds(5));
			int currentTargetNum = 0;
			var targetIds = ClientCommunication.Instance.GetConnectedTargetIds();
			foreach (var targetId in targetIds.Take(2))
			{
				ClientCommunication.Instance.ShowTargetByTargetNum(targetId, TimeSpan.FromSeconds(10));
				Thread.Sleep(1000);
			}

			Thread.Sleep(TimeSpan.FromSeconds(10));
			ClientCommunication.Instance.HideAllTargets();

			foreach (var targetId in targetIds.Skip(2).Take(3))
			{
				ClientCommunication.Instance.ShowTargetByTargetNum(targetId, TimeSpan.FromSeconds(10));
				Thread.Sleep(1000);

			}

			Thread.Sleep(TimeSpan.FromSeconds(10));
			ClientCommunication.Instance.HideAllTargets();

			ClientCommunication.Instance.LevelEnd(levelName);
			Thread.Sleep(TimeSpan.FromSeconds(17));

		}

		private static void Countdown(TimeSpan time)
		{
			int countdown = time.Seconds;
			var timer = new Timer();
			timer.Elapsed += (sender, args) =>
			{
				if (countdown <= 0)
				{
					ClientCommunication.Instance.ShowCountdown("");
					timer.Stop();
				}
				else
				{
					ClientCommunication.Instance.ShowCountdown(countdown.ToString());
					countdown--;
				}
			};
			timer.Interval = 1000;
			timer.Start();

			while (timer.Enabled)
			{
				Thread.Sleep(100);
			}
		}

		public static Game Instance
		{
			get { return _instance.Value; }
		}
	}
}