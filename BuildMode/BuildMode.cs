using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            Order = 20;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Update -= OnUpdate;
                NetHooks.GetData -= OnGetData;
                NetHooks.SendBytes -= OnSendBytes;
                ServerHooks.Leave -= OnLeave;
            }
        }
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("buildmode", BuildModeCmd, "buildmode"));

            GameHooks.Update += OnUpdate;
            NetHooks.GetData += OnGetData;
            NetHooks.SendBytes += OnSendBytes;
            ServerHooks.Leave += OnLeave;
        }

        void OnGetData(GetDataEventArgs e)
        {
            if (e.MsgID == PacketTypes.Tile && !e.Handled && Build[e.Msg.whoAmI])
            {
                int type = e.Msg.readBuffer[e.Index];
                if (type == 1 || type == 3 || type == 5)
                {
                    Player plr = Main.player[e.Msg.whoAmI];
                    Item selected = plr.inventory[plr.selectedItem];
                    int tile = e.Msg.readBuffer[e.Index + 9];
                    if (selected.stack == 1 && ((type == 1 && selected.createTile == tile) || (type == 3 && selected.createWall == tile)))
                    {
                        TShock.Players[e.Msg.whoAmI].GiveItem(selected.type, selected.name, plr.width, plr.height, selected.maxStack);
                    }
                    else if (type == 5)
                    {
                        int wireCount = 0;
                        foreach (Item i in plr.inventory)
                        {
                            if (i.type == 530)
                            {
                                wireCount += i.stack;
                            }
                        }
                        if (wireCount == 1)
                        {
                            TShock.Players[e.Msg.whoAmI].GiveItem(530, "Wire", plr.width, plr.height, 250);
                        }
                    }
                }
            }
        }
        void OnLeave(int plr)
        {
            Build[plr] = false;
        }
        void OnSendBytes(ServerSock sock, byte[] buffer, int offset, int count, HandledEventArgs e)
        {
            if (Build[sock.whoAmI] && !e.Handled)
            {
                if (buffer[offset + 4] == 7)
                {
                    byte[] raw = new byte[count];
                    Buffer.BlockCopy(buffer, offset, raw, 0, count);
                    Buffer.BlockCopy(BitConverter.GetBytes(27000), 0, raw, 5, 4);
                    raw[9] = 1;
                    TShock.Players[sock.whoAmI].SendRawData(raw);
                    e.Handled = true;
                }
                else if (buffer[offset + 4] == 18)
                {
                    byte[] raw = new byte[count];
                    Buffer.BlockCopy(buffer, offset, raw, 0, count);
                    raw[5] = 1;
                    Buffer.BlockCopy(BitConverter.GetBytes(27000), 0, raw, 6, 4);
                    TShock.Players[sock.whoAmI].SendRawData(raw);
                    e.Handled = true;
                }
            }
        }
        void OnUpdate()
        {
            if ((DateTime.UtcNow - LastCheck).TotalSeconds > 1)
            {
                for (int i = 0; i < 256; i++)
                {
                    if (Build[i])
                    {
                        NetMessage.SendData(18, i);
                    }
                }
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
