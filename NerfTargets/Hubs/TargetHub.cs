using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

		public void RecordHit()
		{
			ClientCommunication.Instance.RecordHit(Context.ConnectionId);
		}
	}


	public class ClientCommunication
	{
		private static readonly Lazy<ClientCommunication> _instance = new Lazy<ClientCommunication>();
		private readonly IHubContext _targetHub;
		private readonly Random _random = new Random();
		private readonly List<string> _clients = new List<string>();
		private string _currentTarget;

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

		public void ShowRandomTarget()
		{
			_currentTarget = _clients[_random.Next(_clients.Count - 1)];
			_targetHub.Clients.Client(_currentTarget).showTarget();
		}

		public void HideAllTargets()
		{
			_currentTarget = null;
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

		public void RecordHit(string connectionId)
		{
			if (connectionId == _currentTarget)
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