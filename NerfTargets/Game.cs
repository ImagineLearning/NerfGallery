﻿using System;
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
				ShowNewTarget();
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
			Countdown(TimeSpan.FromSeconds(5));
			PlayGame();
			GameOver();
		}

		private void GameOver()
		{
			ClientCommunication.Instance.HideAllTargets();
			ClientCommunication.Instance.GameOver(hits);
		}

		private void PlayGame()
		{
			int currentTargetNum = 0;
			var targetIds = ClientCommunication.Instance.GetConnectedTargetIds();
			foreach(var targetId in targetIds)
			{
				ClientCommunication.Instance.ShowTargetByTargetNum(targetId);

				Thread.Sleep(TimeSpan.FromSeconds(7));
				currentTargetNum++;
			}
		}

		private static void Countdown(TimeSpan time)
		{
			int countdown = time.Seconds;
			var timer = new Timer();
			timer.Elapsed += (sender, args) =>
			{
				if (countdown <= 0)
				{
					ClientCommunication.Instance.ShowText("");
					timer.Stop();
				}
				else
				{
					ClientCommunication.Instance.ShowText(countdown.ToString());
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