using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Hooks;
using Terraria;
using TShockAPI;

namespace BuildMode
{
    [APIVersion(1, 12)]
    public class BuildMode : TerrariaPlugin
    {
        public override string Author
        {
            get { return "MarioE"; }
        }
        private bool[] Build = new bool[256];
        public override string Description
        {
            get { return "Adds a command for builders."; }
        }
        private DateTime LastCheck = DateTime.UtcNow;
        public override string Name
        {
            get { return "BuildMode"; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public BuildMode(Main game)
            : base(game)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Update -= OnUpdate;
                NetHooks.GetData -= OnGetData;
                ServerHooks.Leave -= OnLeave;
            }
        }
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("buildmode", BuildModeCmd, "buildmode"));

            GameHooks.Update += OnUpdate;
            NetHooks.GetData += OnGetData;
            ServerHooks.Leave += OnLeave;
        }

        void OnGetData(GetDataEventArgs e)
        {
            if (e.MsgID == PacketTypes.Tile && !e.Handled && Build[e.Msg.whoAmI])
            {
                int type = e.Msg.readBuffer[e.Index];
                if (type == 1 || type == 3)
                {
                    Player plr = Main.player[e.Msg.whoAmI];
                    Item selected = plr.inventory[plr.selectedItem];
                    if (selected.stack == 1)
                    {
                        TShock.Players[e.Msg.whoAmI].GiveItem(selected.type, selected.name, plr.width, plr.height, selected.maxStack);
                    }
                }
            }
        }
        void OnLeave(int plr)
        {
            Build[plr] = false;
        }
        void OnUpdate()
        {
            if ((DateTime.UtcNow - LastCheck).TotalSeconds > 1)
            {
                LastCheck = DateTime.UtcNow;
                bool dayTime = Main.dayTime;
                double time = Main.time;
                Main.dayTime = true;
                Main.time = 27000.0;

                for (int i = 0; i < 256; i++)
                {
                    if (Build[i])
                    {
                        NetMessage.SendData(7, i);
                    }
                }
                Main.dayTime = dayTime;
                Main.time = time;
            }
        }

        void BuildModeCmd(CommandArgs e)
        {
            Build[e.Player.Index] = !Build[e.Player.Index];
            e.Player.SendMessage(String.Format("{0}abled build mode.", Build[e.Player.Index] ? "En" : "Dis"), Color.Green);
            NetMessage.SendData(7, e.Player.Index);
        }
    }
}
