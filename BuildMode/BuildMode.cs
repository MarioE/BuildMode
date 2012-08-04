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
            get { return "Adds a building command."; }
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
            Order = -5;
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
            if (!e.Handled && Build[e.Msg.whoAmI])
            {
                switch (e.MsgID)
                {
                    case PacketTypes.Tile:
                        {
                            int type = e.Msg.readBuffer[e.Index];
                            if (type == 1 || type == 3 || type == 5)
                            {
                                Player plr = Main.player[e.Msg.whoAmI];
                                int tile = e.Msg.readBuffer[e.Index + 9];
                                if (type == 1 || type == 3)
                                {
                                    Item lastItem = null;
                                    int tileCount = 0;
                                    foreach (Item i in plr.inventory)
                                    {
                                        if ((type == 1 && i.createTile == tile) || (type == 3 && i.createWall == tile))
                                        {
                                            lastItem = i;
                                            tileCount += i.stack;
                                        }
                                    }
                                    if (tileCount == 1)
                                    {
                                        TShock.Players[e.Msg.whoAmI].GiveItem(lastItem.type, lastItem.name, plr.width, plr.height, lastItem.maxStack);
                                    }
                                }
                                else
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
                        break;
                    case PacketTypes.TogglePvp:
                        Main.player[e.Msg.whoAmI].hostile = false;
                        NetMessage.SendData(30, -1, -1, "", e.Msg.whoAmI);
                        e.Handled = true;
                        break;
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
                switch (buffer[4])
                {
                    case 7:
                        Buffer.BlockCopy(BitConverter.GetBytes(27000), 0, buffer, 5, 4);
                        buffer[9] = 1;
                        break;
                    case 18:
                        buffer[5] = 1;
                        Buffer.BlockCopy(BitConverter.GetBytes(27000), 0, buffer, 6, 4);
                        break;
                    case 23:
                        {
                            NPC npc = Main.npc[BitConverter.ToInt16(buffer, 5)];
                            if (!npc.friendly)
                            {
                                buffer[27] = 0;
                                buffer[28] = 0;
                                buffer[45] = 0;
                                buffer[46] = 0;
                            }
                        }
                        break;
                    case 27:
                        {
                            Projectile proj = Main.projectile[BitConverter.ToInt16(buffer, 5)];
                            if (!proj.friendly)
                            {
                                buffer[30] = 0;
                            }
                        }
                        break;
                }
            }
        }
        void OnUpdate()
        {
            if ((DateTime.UtcNow - LastCheck).TotalSeconds > 1)
            {
                LastCheck = DateTime.UtcNow;
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
            for (int i = 0; i < 200; i++)
            {
                NetMessage.SendData(23, e.Player.Index, -1, "", i);
            }
            if (Build[e.Player.Index] && e.TPlayer.hostile)
            {
                e.TPlayer.hostile = false;
                NetMessage.SendData(30, -1, -1, "", e.Player.Index);
            }
        }
    }
}
