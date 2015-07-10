using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		public readonly byte[] _levels = new byte[512];

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

		public byte this[int i] {
			set {
				_levels[i - 1] = value;
			}
		}


		public void SetValues() {
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
				SetGenaralColor(lights, 0, 0, 0);

				//LevelEnd(lights, "2");

				targetProxy.On<string>("ShowTarget", t => ShowTarget(lights, t));
				targetProxy.On<string>("HideTarget", t => HideTarget(lights, t));
				targetProxy.On<string, bool>("RecordHit", (t, success) =>
				{
					if (success)
						RecordHit(lights, t);
				});
				targetProxy.On<string>("LevelStart", t => LevelStart(lights, t));
				targetProxy.On<string>("LevelEnd", t => LevelEnd(lights, t));

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
						LevelStart(lights, id);
					}

					if (command.StartsWith("end"))
					{
						var id = command.Substring("end".Length + 1);
						LevelStart(lights, id);
					}
				}
			}
		}

		private static void LevelEnd(LightBoard lights, string level)
		{
			PlayAudio(@"..\..\..\NerfTargets\content\voice\" + level + "outro.wav");

			if (level == "1") {
				SetGenaralColor(lights, 50, 0, 0);
			} else if (level == "2") {
				SetGenaralColor(lights, 0, 30, 0);


			}
		}


		private static void PlayAudio(string file)
		{
			file = Path.Combine(Environment.CurrentDirectory, file);

//			System.Media.SoundPlayer myPlayer = new System.Media.SoundPlayer();
//myPlayer.SoundLocation = file;
//myPlayer.Play();
			var musicplayer = new MusicPlayer(file);
			musicplayer.Play(false);

			//file = Path.Combine(Environment.CurrentDirectory, file);
			//Console.WriteLine("Playing Audio " + file);
			//var p1 = new System.Windows.Media.MediaPlayer();
			//p1.Open(new System.Uri(file));
			//p1.Play();
		}

		private static void LevelStart(LightBoard lights, string level)
		{
			PlayAudio(@"..\..\..\NerfTargets\content\voice\" + level + "intro.wav");

			if (level == "1") {
				SetGenaralColor(lights, 20, 20, 20);
			} else if (level == "2") {
				SetGenaralColor(lights, 0, 0, 20);
			}
		}

		private static void RecordHit(LightBoard lights, string id)
		{
			PlayAudio(@"..\..\..\NerfTargets\content\sounds\hit.wav");

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

		private static void SetGenaralColor(LightBoard lights, byte r, byte g, byte b) 
		{
			Action<int> setFixture = (chan) => {
				lights[chan + 0] = 0;
				lights[chan + 1] = 255;
				lights[chan + 2] = 0;
				for (int i = 0; i < 4; ++i) {

					lights[chan + 3 + 3 * i] = r;
					lights[chan + 3 + 3 * i + 1] = g;
					lights[chan + 3 + 3 * i + 2] = b;
				}
			};

			setFixture(278);
			setFixture(293);

			lights.SetValues();

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
