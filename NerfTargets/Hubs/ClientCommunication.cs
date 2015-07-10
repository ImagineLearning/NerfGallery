using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Microsoft.AspNet.SignalR;

namespace NerfTargets.Hubs
{
	public class ClientCommunication
	{
		private static readonly Lazy<ClientCommunication> _instance = new Lazy<ClientCommunication>();
		private readonly IHubContext _targetHub;
		private readonly Random _random = new Random();
		private readonly List<string> _clients = new List<string>();
		private readonly Dictionary<string, int> _clientsNumbersByConnectionIds = new Dictionary<string, int>();
		private readonly HashSet<string> _clientsShowingTarget = new HashSet<string>();


		public event EventHandler GoodHit = (sender, args) => { };
		public event EventHandler BadHit = (sender, args) => { };

		public ClientCommunication()
		{
			_targetHub = GlobalHost.ConnectionManager.GetHubContext<TargetHub>();

		}

		public static ClientCommunication Instance
		{
			get { return _instance.Value; }
		}

		public void GameOver(int points)
		{
			_targetHub.Clients.All.gameOver(points);
		}

		public void ShowCountdown(string text)
		{
			_targetHub.Clients.All.showCountdown(text);
		}

		public List<int> GetConnectedTargetIds()
		{
			var ids = _clientsNumbersByConnectionIds.Select(kvp => kvp.Value).ToList();
			ids.Sort();
			return ids;
		}

		public void ShowTargetByTargetNum(int targetNum, int delaySeconds = 5)
		{
			var idKvp = _clientsNumbersByConnectionIds.FirstOrDefault(c => c.Value == targetNum);
			if (!string.IsNullOrEmpty(idKvp.Key))
			{
				ShowTarget(idKvp.Key);
				var timer = new Timer();
				timer.Elapsed += (sender, args) => HideTarget(idKvp.Key);
				timer.Interval = delaySeconds * 1000;
				timer.AutoReset = false;
				timer.Start();
			}
		}


		public void ShowRandomTarget(int delay)
		{
			var eligibleClients = _clients.Except(_clientsShowingTarget).ToList();
			if (eligibleClients.Any())
			{
				var randomTarget = eligibleClients.Skip(_random.Next(0, eligibleClients.Count - 1)).First();
				ShowTarget(randomTarget);
				var timer = new Timer();
				timer.Elapsed += (sender, args) => HideTarget(randomTarget);
				timer.Interval = delay*1000;
				timer.AutoReset = false;
				timer.Start();
			}
		}

		public void HideAllTargets()
		{
			_targetHub.Clients.All.hideTarget();
			_clientsShowingTarget.Clear();
		}

		public void AddClient(string connectionId)
		{
			_clients.Add(connectionId);
		}

		public void RemoveClient(string connectionId)
		{
			_clients.Remove(connectionId);
			_clientsNumbersByConnectionIds.Remove(connectionId);
		}

		public void SetClientId(string connectionId, int targetNumber)
		{
			_clientsNumbersByConnectionIds[connectionId] = targetNumber;
		}

		public void RecordHit(string connectionId, bool good)
		{
			var targetNumber = _clientsNumbersByConnectionIds[connectionId];
			_targetHub.Clients.All.RecordHit(targetNumber, good);
			if (good)
			{
				GoodHit(this, EventArgs.Empty);
				HideTarget(connectionId);
			}
			else
			{
				BadHit(this, EventArgs.Empty);
			}
		}


		private void ShowTarget(string clientId)
		{
			_clientsShowingTarget.Add(clientId);
			_targetHub.Clients.All.showTarget(_clientsNumbersByConnectionIds[clientId]);
		}

		private void HideTarget(string clientId)
		{
			_clientsShowingTarget.Remove(clientId);
			_targetHub.Clients.All.hideTarget(_clientsNumbersByConnectionIds[clientId]);
		}

		public void LevelStart(string name)
		{
			_targetHub.Clients.All.levelStart(name);
		}

		public void LevelEnd()
		{
			_targetHub.Clients.All.levelEnd();
		}

	}
}