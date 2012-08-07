using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
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
        private Timer Timer;
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
                NetHooks.GetData -= OnGetData;
                NetHooks.SendBytes -= OnSendBytes;
                ServerHooks.Leave -= OnLeave;
            }
        }
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("buildmode", BuildModeCmd, "buildmode"));

            NetHooks.GetData += OnGetData;
            NetHooks.SendBytes += OnSendBytes;
            ServerHooks.Leave += OnLeave;

            Timer = new Timer(1000);
            Timer.Elapsed += OnElapsed;
            Timer.Start();
        }

        void OnElapsed(object sender, ElapsedEventArgs e)
        {
            for (int i = 0; i < 256; i++)
            {
                if (Build[i])
                {
                    NetMessage.SendData(7, i);
                    TShock.Players[i].SetBuff(11, Int16.MaxValue);
                }
            }
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
                                    if (tileCount <= 5)
                                    {
                                        TShock.Players[e.Msg.whoAmI].GiveItem(lastItem.type, lastItem.name, plr.width, plr.height, lastItem.maxStack + 1 - tileCount);
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
            bool build = Build[sock.whoAmI];
            switch (buffer[4])
            {
                case 7:
                    Buffer.BlockCopy(BitConverter.GetBytes(build ? 27000 : (int)Main.time), 0, buffer, 5, 4);
                    buffer[9] = (byte)(Main.dayTime || build ? 1 : 0);
                    Buffer.BlockCopy(BitConverter.GetBytes(build ? Main.maxTilesY : (int)Main.worldSurface), 0, buffer, 28, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(build ? Main.maxTilesY : (int)Main.rockLayer), 0, buffer, 32, 4);
                    break;
                case 18:
                    buffer[5] = (byte)(Main.dayTime || build ? 1 : 0);
                    Buffer.BlockCopy(BitConverter.GetBytes(build ? 27000 : (int)Main.time), 0, buffer, 6, 4);
                    break;
                case 23:
                    NPC npc = Main.npc[BitConverter.ToInt16(buffer, 5)];
                    if (!npc.friendly)
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes(build ? 0 : npc.life), 0, buffer, 27, 4);
                    }
                    break;
                case 27:
                    short id = BitConverter.ToInt16(buffer, 5);
                    int owner = buffer[29];
                    Projectile proj = Main.projectile[TShock.Utils.SearchProjectile(id, owner)];
                    if (!proj.friendly)
                    {
                        buffer[30] = (byte)(build ? 0 : proj.type);
                    }
                    break;
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