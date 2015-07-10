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

		public void ShowText(string text)
		{
			_targetHub.Clients.All.showText(text);
		}

		private readonly HashSet<string> _clientsShowingTarget = new HashSet<string>();

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
		}

		public void RecordHit(string connectionId, bool good)
		{
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
			_targetHub.Clients.Client(clientId).showTarget();
		}

		private void HideTarget(string clientId)
		{
			_clientsShowingTarget.Remove(clientId);
			_targetHub.Clients.Client(clientId).hideTarget();
		}
	}
}