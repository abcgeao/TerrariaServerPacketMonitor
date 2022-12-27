using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TerrariaServerPacketMonitor
{
    [ApiVersion(2, 1)]
    public class MainPlugin : TerrariaPlugin
    {
        public override string Author => "Leader&Jonesn";
        public override string Description => "数据包监控";
        public override string Name => "TerrariaServerPacketMonitor";
        public override Version Version => new Version(1, 0, 0, 0);
        internal List<Packet> Receive { get; set; } = new List<Packet>();
        internal List<Packet> Send { get; set; } = new List<Packet>();
        public MainPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
			ServerApi.Hooks.NetSendBytes.Register(this, OnNetSendBytes);
            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
            Commands.ChatCommands.Add(new Command("ana.admin", analyze, "ana"));
            if (!File.Exists("analyze.html"))
            {
                ExtractResFile("TerrariaServerPacketMonitor.analyze.html", "analyze.html");
            }

		}
		public static void ExtractResFile(string resFileName, string outputFile)
		{
			BufferedStream inStream = null;
			FileStream outStream = null;
			try
			{
				Assembly asm = Assembly.GetExecutingAssembly();
				inStream = new BufferedStream(asm.GetManifestResourceStream(resFileName));
				outStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

				byte[] buffer = new byte[1024];
				int length;

				while ((length = inStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					outStream.Write(buffer, 0, length);
				}
				outStream.Flush();
			}
			finally
			{
				if (outStream != null)
				{
					outStream.Close();
				}
				if (inStream != null)
				{
					inStream.Close();
				}
			}
		}

		private void analyze(CommandArgs args)
        {
            
            {
                Receive.Sort((x, y) => y.Count.CompareTo(x.Count));
                Send.Sort((x, y) => y.Count.CompareTo(x.Count));
				List<string> SendlegendData = new List<string>();
				foreach (Packet pac in Send)
				{
					SendlegendData.Add(pac.name);
				}
				List<string> ReceivelegendData = new List<string>();
				foreach (Packet pac in Receive)
				{
					ReceivelegendData.Add(pac.name);
				}
				var text = JsonConvert.SerializeObject(new Analyze()
                {
                    TotalPackets = Receive.Select(x => x.Count).Sum() + Send.Select(x => x.Count).Sum(),
                    TotalBytes = Receive.Select(x => x.value).Sum() + Send.Select(x => x.value).Sum(),
					SendLegendData = SendlegendData.ToArray(),
                    ReceiveLegendData = ReceivelegendData.ToArray(),
					SendSeriesData = Send.ToArray(),
                    SendBytes = Send.Select(x => x.value).Sum(),
                    SendPackets = Send.Select(x => x.Count).Sum(),
					ReceiveSeriesData = Receive.ToArray(),
                    ReceiveBytes = Receive.Select(x => x.value).Sum(),
                    ReceivePackets = Receive.Select(x => x.Count).Sum()
                }, Formatting.Indented);;;;
                File.WriteAllText("analyze.js", "var htmlobj=" + text);
            }
            args.Player.SendSuccessMessage("数据包分析(analyze.js)已生成，请查看analyze.html");
		}

        private void OnNetGetData(GetDataEventArgs args)
        {
            var data = Receive.Find(x => x.Type == args.MsgID);
            if ( data== null)
            {
                Receive.Add(new Packet() { Type = args.MsgID, Count = 1, value = args.Msg.totalData, name = args.MsgID.ToString() + "(" + (int)args.MsgID + ")" });
            }
            else
            {
                data.Count++;
                data.value += args.Msg.totalData;
            }
        }

        private void OnNetSendBytes(SendBytesEventArgs args)
        {
            var type = (PacketTypes)args.Buffer[2];
            var data = Send.Find(x => x.Type == type);
            if (data == null)
            {
                Send.Add(new Packet() { Type = type, Count = 1, value = args.Count, name = type.ToString()+"("+(int)type+")" }); 
            }
            else
            {
                data.Count++;
                data.value += args.Count;
            }
        }
    }
}