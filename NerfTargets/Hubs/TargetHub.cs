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

		public void RecordHit(bool good)
		{
			ClientCommunication.Instance.RecordHit(Context.ConnectionId, good);
		}
	}
}