﻿using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static PokeViewer.NET.RoutineExecutor;
using PKHeX.Drawing.PokeSprite;
using Newtonsoft.Json;
using System.Text;
using PokeViewer.NET.Properties;

namespace PokeViewer.NET.SubForms
{
    public partial class Egg_Viewer : Form
    {
        private readonly static SwitchConnectionConfig Config = new() { Protocol = SwitchProtocol.WiFi, IP = Properties.Settings.Default.SwitchIP, Port = 6000 };
        public SwitchSocketAsync SwitchConnection = new(Config);
        private readonly FormWindowState _WindowState;
        public Egg_Viewer()
        {
            InitializeComponent();
            SwitchConnection.Connect();
        }
        private int eggcount = 0;
        private int sandwichcount = 0;
        private int shinycount = 0;
        private readonly uint EggData = 0x04386040;
        private readonly uint PicnicMenu = 0x04416020;
        private readonly byte[] BlankVal = { 0x01 };
        public IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x43A7848, 0x348, 0x10, 0xD8, 0x28 };
        private ulong OverworldOffset;

        public string DumpFolder { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        private async void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.WebHook))
                WebHookText.Text = Settings.Default.WebHook;

            if (FetchButton.Enabled == true)
                DisableOptions();
            var token = CancellationToken.None;
            eggcount = 0;

            // Blank out previous egg data
            await SwitchConnection.WriteBytesMainAsync(new byte[344], EggData, token).ConfigureAwait(false);
            await SwitchConnection.WriteBytesMainAsync(BlankVal, PicnicMenu, token).ConfigureAwait(false);

            if (EatOnStart.Checked)
            {
                await MakeSandwich(token).ConfigureAwait(false);
                await WaitForEggs(token).ConfigureAwait(false);
            }
            else
                await WaitForEggs(token).ConfigureAwait(false);
        }

        private async Task WaitForEggs(CancellationToken token)
        {           
            PK9 pkprev = new();
            while (!token.IsCancellationRequested)
            {
                var wait = TimeSpan.FromMinutes(30);
                var endTime = DateTime.Now + wait;
                var ctr = 0;
                var waiting = 0;                
                while (DateTime.Now < endTime)
                {
                    NextSanwichLabel.Text = $"Next Sandwich: {endTime:hh\\:mm\\:ss}";
                    var pk = await ReadPokemonSV(EggData, 344, token).ConfigureAwait(false);
                    while (pk == null ||pkprev.EncryptionConstant == pk.EncryptionConstant || (Species)pk.Species == Species.None)
                    {
                        waiting++;
                        await Task.Delay(1_500, token).ConfigureAwait(false);
                        pk = await ReadPokemonSV(EggData, 344, token).ConfigureAwait(false);
                        if (waiting == 120)
                        {
                            await ReopenPicnic(token).ConfigureAwait(false);
                            await MakeSandwich(token).ConfigureAwait(false);
                            await ReopenPicnic(token).ConfigureAwait(false);
                            wait = TimeSpan.FromMinutes(30);
                            endTime = DateTime.Now + wait;
                            waiting = 0;
                            ctr = 0;
                        }
                    }

                    while (pk != null && (Species)pk.Species != Species.None && pkprev.EncryptionConstant != pk.EncryptionConstant)
                    {
                        waiting = 0;
                        ctr++;
                        eggcount++;
                        BasketCount.Text = $"Basket Count: {ctr}";
                        string pid = $"{Environment.NewLine}PID: {pk.PID:X8}";
                        string ec = $"{Environment.NewLine}EC: {pk.EncryptionConstant:X8}";
                        var form = FormOutput(pk.Species, pk.Form, out _);
                        string gender = string.Empty;
                        switch (pk.Gender)
                        {
                            case 0: gender = " (M)"; break;
                            case 1: gender = " (F)"; break;
                            case 2: break;
                        }
                        string output = $"{$"Egg #{eggcount}"}{Environment.NewLine}{(pk.ShinyXor == 0 ? "■ - " : pk.ShinyXor <= 16 ? "★ - " : "")}{(Species)pk.Species}{form}{gender}{pid}{ec}{Environment.NewLine}Nature: {(Nature)pk.Nature}{Environment.NewLine}Ability: {(Ability)pk.Ability}{Environment.NewLine}IVs: {pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
                        PokeStats.Text = output;
                        var sprite = PokeImg(pk, false);
                        PokeSpriteBox.Load(sprite);
                        var ballsprite = SpriteUtil.GetBallSprite(pk.Ball);
                        BallBox.Image = ballsprite;

                        await Task.Delay(0_500, token).ConfigureAwait(false);
                        if (pk.IsShiny)
                        {
                            shinycount++;
                            ShinyFoundLabel.Text = $"Shinies Found: {shinycount}";
                        }

                        if (pk.IsShiny && (Species)pk.Species != Species.None && StopOnShiny.Checked)
                        {                            
                            if ((Species)pk.Species is Species.Dunsparce or Species.Tandemaus && pk.EncryptionConstant % 100 != 0 && CheckBoxOf3.Checked)
                                break;

                            if ((Species)pk.Species is Species.Dunsparce or Species.Tandemaus && pk.EncryptionConstant % 100 == 0 && CheckBoxOf3.Checked)
                            {
                                await Click(HOME, 0_500, token).ConfigureAwait(false);
                                SendNotifications(output, sprite);
                                EnableOptions();
                                WindowState = _WindowState;
                                Activate();
                                MessageBox.Show("Rare Shiny Found!");                                
                                return;
                            }

                            await Click(HOME, 0_500, token).ConfigureAwait(false);
                            SendNotifications(output, sprite);
                            EnableOptions();
                            WindowState = _WindowState;
                            Activate();
                            MessageBox.Show("Match found!");
                            return;
                        }

                        pkprev = pk;                        
                    }
                    if (ctr == 10)
                    {
                        BasketCount.Text = $"Resetting..";
                        await ReopenPicnic(token).ConfigureAwait(false);
                        ctr = 0;
                        waiting = 0;
                        BasketCount.Text = $"Basket Count: {ctr}";
                    }
                }
                await MakeSandwich(token).ConfigureAwait(false);
            }
        }
        private async Task<PK9> ReadPokemonSV(uint offset, int size, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesMainAsync(offset, size, token).ConfigureAwait(false);
            var pk = new PK9(data);
            return pk;
        }

        public async Task SetStick(SwitchStick stick, short x, short y, int delay, CancellationToken token)
        {
            var cmd = SwitchCommand.SetStick(stick, x, y, true);
            await SwitchConnection.SendAsync(cmd, token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        private async Task ReopenPicnic(CancellationToken token)
        {
            await Task.Delay(0_500, token).ConfigureAwait(false);
            await Click(Y, 1_500, token).ConfigureAwait(false);
            var overworldWaitCycles = 0;
            var hasReset = false;
            OverworldOffset = await SwitchConnection.PointerAll(OverworldPointer, token).ConfigureAwait(false);
            while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            {
                await Click(A, 1_000, token).ConfigureAwait(false);
                overworldWaitCycles++;

                if (overworldWaitCycles == 10)
                {
                    for (int i = 0; i < 5; i++)
                        await Click(B, 0_500, token).ConfigureAwait(false);

                    await Click(Y, 1_500, token).ConfigureAwait(false);
                    await Click(A, 1_000, token).ConfigureAwait(false);

                    for (int i = 0; i < 4; i++)
                        await Click(B, 0_500, token).ConfigureAwait(false); 
                }
            }
            for (int i = 0; i < 10; i++)
                await Click(A, 0_500, token).ConfigureAwait(false); 
            await Click(X, 1_500, token).ConfigureAwait(false);
            if (hasReset) 
            {
                await Click(DRIGHT, 0_250, token).ConfigureAwait(false);
                await Click(DDOWN, 0_250, token).ConfigureAwait(false);
                await Click(DDOWN, 0_250, token).ConfigureAwait(false);
            }
            await Click(A, 7_000, token).ConfigureAwait(false); 
        }

        private async Task MakeSandwich(CancellationToken token)
        {
            await Click(MINUS, 0_500, token).ConfigureAwait(false);
            await SetStick(LEFT, 0, 30000, 0_700, token).ConfigureAwait(false); // Face up to table
            await SetStick(LEFT, 0, 0, 0, token).ConfigureAwait(false);
            await Click(A, 1_500, token).ConfigureAwait(false);
            await Click(A, 4_000, token).ConfigureAwait(false);
            await Click(X, 1_500, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(Item1Value.Text))
            {
                // Lettuce
                var m1 = Convert.ToInt32(Item1Value.Text);

                for (int i = 0; i < m1; i++)
                {
                    if (checkBox5.Checked)
                        await Click(DUP, 0_800, token).ConfigureAwait(false);
                    else
                    await Click(DDOWN, 0_800, token).ConfigureAwait(false);
                }
            }

            await Click(A, 0_800, token).ConfigureAwait(false);
            await Click(PLUS, 0_800, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(Item2Value.Text))
            {
                // Mystica Salt
                var m2 = Convert.ToInt32(Item2Value.Text);

                for (int i = 0; i < m2; i++)
                {
                    if (checkBox6.Checked)
                        await Click(DUP, 0_800, token).ConfigureAwait(false);
                    else
                        await Click(DDOWN, 0_800, token).ConfigureAwait(false);
                }
            }

            await Click(A, 0_800, token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(Item3Value.Text))
            {
                // Mystica Sweet
                var m3 = Convert.ToInt32(Item3Value.Text);

                for (int i = 0; i < m3; i++)
                {
                    if (checkBox7.Checked)
                        await Click(DUP, 0_800, token).ConfigureAwait(false);
                    else
                        await Click(DDOWN, 0_800, token).ConfigureAwait(false);
                }
            } 

            await Click(A, 0_800, token).ConfigureAwait(false);
            await Click(PLUS, 0_800, token).ConfigureAwait(false);
            // Set pick
            await Click(A, 8_000, token).ConfigureAwait(false);
            //Wait for bread

            var fillingtime = Convert.ToInt32(FillingHoldTime.Text);
            await SetStick(LEFT, 0, 30000, 0_000 + fillingtime, token).ConfigureAwait(false); // Navigate to ingredients
            await SetStick(LEFT, 0, 0, 0, token).ConfigureAwait(false);
            await Task.Delay(0_500, token).ConfigureAwait(false);

            if (HoldIngredients.Checked)
            {
                for (int i = 0; i < NumberOfFillings.Value; i++) // Amount of ingredients to drop
                {
                    await Hold(A, 0_800, token).ConfigureAwait(false);

                    await SetStick(LEFT, 0, -30000, 0_000 + fillingtime, token).ConfigureAwait(false); // Navigate to ingredients
                    await SetStick(LEFT, 0, 0, 0, token).ConfigureAwait(false);
                    await Task.Delay(0_500, token).ConfigureAwait(false);
                    await Release(A, 0_800, token).ConfigureAwait(false);

                    await SetStick(LEFT, 0, 30000, 0_000 + fillingtime, token).ConfigureAwait(false); // Navigate to ingredients
                    await SetStick(LEFT, 0, 0, 0, token).ConfigureAwait(false);
                    await Task.Delay(0_500, token).ConfigureAwait(false);
                }
            }

            sandwichcount++;
            SandwichCount.Text = $"Sandwiches Made: {sandwichcount}";
            for (int i = 0; i < 5; i++)
                await Click(A, 0_800, token).ConfigureAwait(false);

            bool inPicnic = await IsInPicnic(token).ConfigureAwait(false);

            while (!inPicnic)
            {
                await Click(A, 3_000, token).ConfigureAwait(false);
                inPicnic = await IsInPicnic(token).ConfigureAwait(false);
            }

            if (inPicnic)
            {
                await Task.Delay(2_500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, -10000, 0_500, token).ConfigureAwait(false); // Face down to basket
                await SetStick(LEFT, 0, 0, 0, token).ConfigureAwait(false);
                await Task.Delay(1_000, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 5000, 0_200, token).ConfigureAwait(false); // Face up to basket
                await SetStick(LEFT, 0, 0, 0, token).ConfigureAwait(false);
            }
        }

        public new async Task Click(SwitchButton b, int delay, CancellationToken token)
        {
            await SwitchConnection.SendAsync(SwitchCommand.Click(b, true), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task Hold(SwitchButton b, int delay, CancellationToken token)
        {
            await SwitchConnection.SendAsync(SwitchCommand.Hold(b, true), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task Release(SwitchButton b, int delay, CancellationToken token)
        {
            await SwitchConnection.SendAsync(SwitchCommand.Release(b, true), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SwitchConnection.Reset();
            this.Close();  
            Application.Restart();
        }        

        private async Task<bool> IsInPicnic(CancellationToken token)
        {
            var Data = await SwitchConnection.ReadBytesMainAsync(PicnicMenu, 1, token).ConfigureAwait(false);
            return Data[0] == 0x01; // 1 when in picnic, 2 in sandwich menu, 3 when eating, 2 when done eating
        }

        public async Task<bool> IsOnOverworld(ulong offset, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 1, token).ConfigureAwait(false);
            return data[0] == 0x11;
        }

        private void DisableOptions()
        {
            FetchButton.Enabled = false;
            Item1Value.Enabled = false;
            Item2Value.Enabled = false;
            Item3Value.Enabled = false;
            checkBox5.Enabled = false;
            checkBox6.Enabled = false;
            checkBox7.Enabled = false;
            FillingHoldTime.Enabled = false;
            NumberOfFillings.Enabled = false;
        }

        private void EnableOptions()
        {
            FetchButton.Enabled = true;
            Item1Value.Enabled = true;
            Item2Value.Enabled = true;
            Item3Value.Enabled = true;
            checkBox5.Enabled = true;
            checkBox6.Enabled = true;
            checkBox7.Enabled = true;
            FillingHoldTime.Enabled = true;
            NumberOfFillings.Enabled = true;
        }

        private static HttpClient? _client;
        public static HttpClient Client
        {
            get
            {
                if (_client == null)
                    _client = new HttpClient();
                return _client;
            }
        }

        public static string[]? DiscordWebhooks;

        public async void SendNotifications(string results, string thumbnail)
        {
            if (string.IsNullOrEmpty(results))
                return;
            DiscordWebhooks = WebHookText.Text.Split(',');
            if (DiscordWebhooks == null)
                return;
            var webhook = GenerateWebhook(results, thumbnail);
            var content = new StringContent(JsonConvert.SerializeObject(webhook), Encoding.UTF8, "application/json");
            foreach (var url in DiscordWebhooks)
                await Client.PostAsync(url, content).ConfigureAwait(false);
        }

        public static object GenerateWebhook(string results, string thumbnail)
        {
            var WebHook = new
            {
                username = $"EggViewer.NET",
                content = $"<@{Settings.Default.UserDiscordID}>",
                embeds = new List<object>
                {
                    new
                    {
                        title = $"Match Found!",                        
                        thumbnail = new
                        {
                            url = thumbnail
                        },
                        fields = new List<object>
                        {
                            new { name = "Description               ", value = results, inline = true, },
                        },
                    }
                }
            };
            return WebHook;
        }

        private void SaveHookURL_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(WebHookText.Text) || string.IsNullOrEmpty(UserDiscordIDText.Text))
            {
                MessageBox.Show("Please fill the fields before attempting to save.");
                return;
            }

            if (!string.IsNullOrEmpty(WebHookText.Text))
            {
                Settings.Default.WebHook = WebHookText.Text;
                Settings.Default.Save();
            }
            if (!string.IsNullOrEmpty(UserDiscordIDText.Text))
            {
                Settings.Default.UserDiscordID = UserDiscordIDText.Text;
                Settings.Default.Save();
            }

            MessageBox.Show("Done. Reloading form to show changes.");
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Bitmap FormScreenShot = new(Width, Height);
            Graphics G = Graphics.FromImage(FormScreenShot);
            G.CopyFromScreen(Location, new Point(0, 0), Size);
            Clipboard.SetImage(FormScreenShot);
        }
    }
}
