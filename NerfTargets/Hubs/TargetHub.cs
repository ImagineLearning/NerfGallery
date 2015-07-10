using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNet.SignalR;

namespace NerfTargets.Hubs
{
	public class TargetHub : Hub
	{
		
		public override Task OnConnected()
		{
			ClientCommunication.Instance.AddClient(Context.ConnectionId);
			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			ClientCommunication.Instance.RemoveClient(Context.ConnectionId);
			return base.OnDisconnected(stopCalled);
		}

		public void RecordHit(bool good)
		{
			ClientCommunication.Instance.RecordHit(Context.ConnectionId, good);
		}
	}


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
		public static ClientCommunication Instance { get { return _instance.Value; } }

		public void ShowText(string text)
		{
			_targetHub.Clients.All.showText(text);
		}

		public void ShowRandomTarget(int delay)
		{
			var target = _clients[_random.Next(_clients.Count - 1)];
			_targetHub.Clients.Client(target).showTarget();
			var timer = new Timer();
			timer.Elapsed += (sender, args) => _targetHub.Clients.Client(target).hideTarget();
			timer.Interval = delay*1000;
			timer.AutoReset = false;
			timer.Start();
		}

		public void HideAllTargets()
		{
			_targetHub.Clients.All.hideTarget();
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
			}
			else
			{
				BadHit(this, EventArgs.Empty);
			}
		}
	}
}