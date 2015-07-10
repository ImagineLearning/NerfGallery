using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Threading;
using basic_light_board;
using Microsoft.AspNet.SignalR.Client;

namespace LightController
{
	public class LightBoard : IDisposable
	{
		private readonly VComWrapper _com;
		readonly byte[] _levels = new byte[512];

		public LightBoard()
		{
			_com = new VComWrapper();

			var ports = System.IO.Ports.SerialPort.GetPortNames();
			_com.initPro(ports.First());

		}

		public void SetValue(int[] channels, byte level)
		{
			foreach (var channel in channels)
			{
				_levels[channel - 1] = level;
			}
			_com.sendDMXPacketRequest(_levels);
		}

		public void Dispose()
		{
			_com.sendDMXPacketRequest(new byte[512]);
			_com.detatchPro();
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			HubConnection hubConnection;
			if (Debugger.IsAttached)
			{
				hubConnection = new HubConnection("http://localhost:11796/");
			}
			else
			{
				hubConnection = new HubConnection("http://nerf.azurewebsites.net/");				
			}

			IHubProxy targetProxy = hubConnection.CreateHubProxy("TargetHub");

			hubConnection.Start();

			using (var lights = new LightBoard())
			{
				targetProxy.On<string>("ShowTarget", t => ShowTarget(lights, t));
				targetProxy.On<string>("HideTarget", t => HideTarget(lights, t));
				targetProxy.On<string, bool>("RecordHit", (t, success) =>
				{
					if (success)
						RecordHit(lights, t);
				});
				targetProxy.On<string>("LevelStart", LevelStart);
				targetProxy.On<string>("LevelEnd", LevelEnd);

				var allLights = Enumerable.Range(1, 10).Select(t => t.ToString()).ToList();

				while (true)
				{
					var command = Console.ReadLine();
					if (command == null)
						continue;

					if (command == "quit")
						break;

					if (command.StartsWith("hit"))
					{
						var id = command.Substring("hit".Length + 1);
						RecordHit(lights, id);
					}

					if (command.StartsWith("start"))
					{
						var id = command.Substring("start".Length + 1);
						LevelStart(id);
					}

					if (command.StartsWith("end"))
					{
						var id = command.Substring("end".Length + 1);
						LevelStart(id);
					}
				}
			}
		}

		private static void LevelEnd(string level)
		{
			PlayAudio(@"..\..\music\" + level + "_outro.wav");
		}

		private static void PlayAudio(string file)
		{
			var p1 = new System.Windows.Media.MediaPlayer();
			p1.Open(new System.Uri(file));
			p1.Play();
		}

		private static void LevelStart(string level)
		{
			PlayAudio(@"..\..\music\" + level + "_into.wav");
		}

		private static void RecordHit(LightBoard lights, string id)
		{
			PlayAudio(@"..\..\sounds\hit.mp3");

			var channels = GetChannelsForTarget(id);

			for (int i = 0; i < 3; ++i)
			{
				lights.SetValue(channels, 0x00);
				Thread.Sleep(100);
				lights.SetValue(channels, 0xFF);
				Thread.Sleep(100);
			}
			lights.SetValue(channels, 0x00);

		}

		private static readonly Dictionary<string, int[]> TargetsToLightsMap = new Dictionary<string, int[]>
		{
			{"1", new[] {55, 56, 57}},
			{"2", new[] {58, 59, 60}},
			{"3", new[] {61, 62, 63}},
			{"4", new[] {64, 65, 66}},
			{"5", new[] {67, 68, 69}},
			{"6", new[] {70, 71, 72}},
			{"7", new[] {73, 74, 75}},
			{"8", new[] {76, 77, 78}},
			{"9", new[] {79, 80, 81}},
			{"10", new[] {82, 83, 84}},
		};

		private static void HideTarget(LightBoard lights, string id)
		{
			Console.WriteLine("Hiding target " + id);
			lights.SetValue(GetChannelsForTarget(id), 0x00);

		}

		private static void ShowTarget(LightBoard lights, string id)
		{
			Console.WriteLine("Showing target " + id);
			lights.SetValue(GetChannelsForTarget(id), 0xFF);
		}

		private static int[] GetChannelsForTarget(string id)
		{
			if (!TargetsToLightsMap.ContainsKey(id))
			{
				Console.WriteLine("No lights for target: " + id);
				return new int[0];
			}
			return TargetsToLightsMap[id];
		}
	}
}
