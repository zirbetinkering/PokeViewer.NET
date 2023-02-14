﻿using PKHeX.Core;
using PKHeX.Drawing.Misc;
using PokeViewer.NET.SubForms;
using PokeViewer.NET.WideViewForms;
using SysBot.Base;
using System.Net.Sockets;
using System.Text;
using static PokeViewer.NET.RoutineExecutor;
using static PokeViewer.NET.ViewerUtil;

namespace PokeViewer.NET
{
    public partial class MainViewer : Form
    {
        private static readonly SwitchConnectionConfig Config = new() { Protocol = SwitchProtocol.WiFi, IP = Properties.Settings.Default.SwitchIP, Port = 6000 };
        public SwitchSocketAsync SwitchConnection = new(Config);

        public MainViewer()
        {
            InitializeComponent();
        }

        private int GameType;
        private string RefreshTime = Properties.Settings.Default.RefreshRate;

        private void PokeViewerForm_Load(object sender, EventArgs e)
        {
            SwitchIP.Text = Properties.Settings.Default.SwitchIP;
            View.Visible = false;
            ViewBox.Visible = false;
            PokeSprite.Visible = false;
            LiveStats.Visible = false;
            RefreshStats.Visible = false;
            HidePIDEC.Visible = false;
            HpLabel.Visible = false;
            UniqueBox.Visible = false;
            UniqueBox2.Visible = false;
            LoadOriginDefault(sender, e);
            LoadDateTime(sender, e);
        }

        private void LoadOriginDefault(object send, EventArgs e)
        {
            string url = "https://raw.githubusercontent.com/zyro670/PokeTextures/main/OriginMarks/icon_generation_00%5Esb.png";
            OriginIcon.ImageLocation = url;
        }

        private void CheckForIP(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Text != "192.168.0.0")
            {
                Properties.Settings.Default.SwitchIP = textBox.Text;
                Config.IP = SwitchIP.Text;
            }
            Properties.Settings.Default.Save();
        }

        private void CheckForHide(object sender, EventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.Checked == true)
            {
                Properties.Settings.Default.HidePIDEC = HidePIDEC.Checked;
                HidePIDEC.Checked = true;
            }
            else
            {
                Properties.Settings.Default.HidePIDEC = false;
                HidePIDEC.Checked = false;
            }
            Properties.Settings.Default.Save();
        }

        private void LoadDateTime(object sender, EventArgs e)
        {
            TodaysDate.Text = "Met Date: " + DateTime.Today.ToString("MM/dd/yyyy");
        }

        private SwitchProtocol GetProtocol()
        {
            if (ToggleSwitchProtocol.Checked)
                return SwitchProtocol.USB;
            return SwitchProtocol.WiFi;
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            if (!SwitchConnection.Connected)
            {
                try
                {
                    SwitchConnection.Connect();
                    Connect.Text = "Disconnect";
                    View.Enabled = true;
                    SwitchIP.Enabled = false;
                    ViewBox.Visible = true;
                    PokeSprite.Visible = true;
                    LiveStats.Visible = true;
                    RefreshStats.Visible = true;
                    Refresh.Visible = true;
                    RefreshBox.Visible = true;
                    InGameScreenshot.Visible = true;
                    HidePIDEC.Visible = true;
                    HpLabel.Visible = true;
                    View.Visible = true;
                    WideView.Visible = true;
                    WideView.Enabled = true;
                    BoxViewer.Visible = true;
                    BoxViewer.Enabled = true;
                    TrainerView.Visible = true;
                    DayCareView.Visible = true;
                    OverworldView.Visible = true;
                    Raids.Visible = true;
                    Window_Loaded();
                }
                catch (SocketException err)
                {
                    MessageBox.Show(err.Message);
                    MessageBox.Show($"{Environment.NewLine}Ensure IP address is correct before connecting!");
                }
            }
            else if (SwitchConnection.Connected)
            {
                SwitchConnection.Disconnect();
                SwitchConnection.Reset();
                Connect.Text = "Connect";
                SwitchIP.Enabled = true;
                View.Enabled = false;
                ViewBox.Visible = false;
                PokeSprite.Visible = false;
                LiveStats.Visible = false;
                RefreshStats.Visible = false;
                Refresh.Visible = false;
                RefreshBox.Visible = false;
                InGameScreenshot.Visible = false;
                HidePIDEC.Visible = false;
                View.Visible = false;
                Typing1.Visible = false;
                Typing2.Visible = false;
                Specialty.Visible = false;
                HpLabel.Visible = false;
                UniqueBox.Visible = false;
                UniqueBox2.Visible = false;
                WideView.Visible = false;
                WideView.Enabled = false;
                BoxViewer.Visible = false;
                TrainerView.Visible = false;
                DayCareView.Visible = false;
                Raids.Visible = false;
                LiveStats.Clear();
                string url = "https://raw.githubusercontent.com/zyro670/PokeTextures/main/OriginMarks/icon_generation_00%5Esb.png";
                OriginIcon.ImageLocation = url;
                OverworldView.Visible = false;
                this.Close();
                Application.Restart();
            }

        }

        private void View_Click(object sender, EventArgs e)
        {
            ReadEncounter_ClickAsync(sender, e);
        }

        private async void FillPokeData(PKM pk, ulong offset, uint offset2, int size)
        {
            Specialty.Visible = false;
            var sprite = string.Empty;
            bool isValid = false;
            switch (GameType)
            {
                case (int)GameSelected.Scarlet or (int)GameSelected.Violet: isValid = PersonalTable.SV.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.SW or (int)GameSelected.SH: isValid = PersonalTable.SWSH.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.BD or (int)GameSelected.SP: isValid = PersonalTable.BDSP.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.LA:
                    {
                        isValid = PersonalTable.LA.IsPresentInGame(pk.Species, pk.Form);
                        if (!isValid)
                        {
                            if ((Species)pk.Species is Species.Decidueye or Species.Typhlosion or Species.Samurott or Species.Qwilfish or Species.Lilligant or Species.Sliggoo or Species.Goodra
                            or Species.Growlithe or Species.Arcanine or Species.Voltorb or Species.Electrode or Species.Sneasel or Species.Avalugg or Species.Zorua or Species.Zoroark or Species.Braviary)
                                isValid = true;
                        }
                        break;
                    }
                case (int)GameSelected.LGP or (int)GameSelected.LGE: isValid = pk.Species < (int)Species.Mewtwo && pk.Species != (int)Species.Meltan && pk.Species != (int)Species.Melmetal; break;
            }
            if (!isValid || pk.Species < 0 || pk.Species > (int)Species.MAX_COUNT)
            {
                ViewBox.Text = "No Pokémon present.";
                View.Enabled = true;
                sprite = "https://raw.githubusercontent.com/kwsch/PKHeX/master/PKHeX.Drawing.PokeSprite/Resources/img/Pokemon%20Sprite%20Overlays/starter.png";
                PokeSprite.Load(sprite);
                Typing1.Visible = false;
                Typing2.Visible = false;
                Specialty.Visible = false;
                LiveStats.Clear();
                return;
            }
            Typing1.Visible = true;
            Typing2.Visible = true;
            bool alpha = pk is PA8 pa8 ? pa8.IsAlpha : false;
            bool hasMark = false;
            bool isGmax = pk is PK8 pk8 ? pk8.CanGigantamax : false;
            string msg = "";
            if (pk is PK8)
            {
                hasMark = HasMark((PK8)pk, out RibbonIndex mark);
                msg = hasMark ? $"{Environment.NewLine}Mark: {mark.ToString().Replace("Mark", "")}" : "";
            }
            if (pk is PK8 && UniqueBox2.Checked)
                msg = $"{Environment.NewLine}Mark: Curry";

            string isAlpha = alpha ? $"αlpha - " : "";
            string pid = HidePIDEC.Checked ? "" : $"{Environment.NewLine}PID: {pk.PID:X8}";
            string ec = HidePIDEC.Checked ? "" : $"{Environment.NewLine}EC: {pk.EncryptionConstant:X8}";
            var form = FormOutput(pk.Species, pk.Form, out _);
            string gender = string.Empty;
            switch (pk.Gender)
            {
                case 0: gender = " (M)"; break;
                case 1: gender = " (F)"; break;
                case 2: break;
            }
            string output = $"{(pk.ShinyXor == 0 ? "■ - " : pk.ShinyXor <= 16 ? "★ - " : "")}{isAlpha}{(Species)pk.Species}{form}{gender}{ec}{pid}{Environment.NewLine}Nature: {(Nature)pk.Nature}{Environment.NewLine}Ability: {(Ability)pk.Ability}{Environment.NewLine}IVs: {pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}{msg}";
            LiveStats.Text = $"{(Move)pk.Move1} - {pk.Move1_PP}PP{Environment.NewLine}{(Move)pk.Move2} - {pk.Move2_PP}PP{Environment.NewLine}{(Move)pk.Move3} - {pk.Move3_PP}PP{Environment.NewLine}{(Move)pk.Move4} - {pk.Move4_PP}PP";
            ViewBox.Text = output;
            sprite = PokeImg(pk, isGmax);
            PokeSprite.Load(sprite);
            var imgt1 = TypeSpriteUtil.GetTypeSpriteWide(pk.PersonalInfo.Type1);
            Typing1.Image = imgt1;
            if (pk.PersonalInfo.Type1 != pk.PersonalInfo.Type2)
            {
                var imgt2 = TypeSpriteUtil.GetTypeSpriteWide(pk.PersonalInfo.Type2);
                Typing2.Image = imgt2;
            }
            if (alpha)
            {
                var url = "https://raw.githubusercontent.com/zyro670/PokeTextures/main/OriginMarks/icon_alpha.png";
                var img = DownloadRemoteImageFile(url);
                Image original;
                using (var ms = new MemoryStream(img))
                {
                    original = Image.FromStream(ms);
                }
                Specialty.Visible = true;
                Specialty.Image = original;
            }
            if (hasMark)
            {
                var info = RibbonInfo.GetRibbonInfo(pk);
                foreach (var rib in info)
                {
                    if (!rib.HasRibbon)
                        continue;

                    var mimg = RibbonSpriteUtil.GetRibbonSprite(rib.Name);
                    if (mimg is not null)
                    {
                        Specialty.Visible = true;
                        Specialty.Image = mimg;
                    }
                }
            }
            if (pk is PK8 & UniqueBox2.Checked)
            {
                var mimg = RibbonSpriteUtil.GetRibbonSprite("RibbonMarkCurry");
                if (mimg is not null)
                {
                    Specialty.Visible = true;
                    Specialty.Image = mimg;
                }
            }
            if (isGmax)
            {
                var url = $"https://raw.githubusercontent.com/zyro670/PokeTextures/main/OriginMarks/icon_daimax.png";
                var img = DownloadRemoteImageFile(url);
                Image original;
                using (var ms = new MemoryStream(img))
                {
                    original = Image.FromStream(ms);
                }
                Specialty.Visible = true;
                Specialty.Image = original;
            }
            if (RefreshStats.Checked)
            {
                var StartingHP = pk.Stat_HPCurrent;
                int.TryParse(RefreshTime, out var refr);
                while (pk.Stat_HPCurrent != 0)
                {
                    if (!SwitchConnection.Connected)
                    {
                        Application.Restart();
                        Environment.Exit(0);
                    }
                    switch (GameType)
                    {
                        case (int)GameSelected.Scarlet or (int)GameSelected.Violet: pk = await ReadInBattlePokemonSV(offset, size).ConfigureAwait(false); break;
                        case (int)GameSelected.SW or (int)GameSelected.SH: pk = await ReadInBattlePokemonSWSH(offset2, size).ConfigureAwait(false); break;
                        case (int)GameSelected.BD or (int)GameSelected.SP: pk = await ReadInBattlePokemonBDSP(offset, size).ConfigureAwait(false); break;
                        case (int)GameSelected.LA: pk = await ReadInBattlePokemonLA(offset, size).ConfigureAwait(false); break;
                        case (int)GameSelected.LGP or (int)GameSelected.LGE: pk = await ReadInBattlePokemonLGPE(offset2, size).ConfigureAwait(false); break;
                    }
                    LiveStats.Text = $"{GameInfo.GetStrings(1).Move[pk.Move1]} - {pk.Move1_PP}PP{Environment.NewLine}{GameInfo.GetStrings(1).Move[pk.Move2]} - {pk.Move2_PP}PP{Environment.NewLine}{GameInfo.GetStrings(1).Move[pk.Move3]} - {pk.Move3_PP}PP{Environment.NewLine}{GameInfo.GetStrings(1).Move[pk.Move4]} - {pk.Move4_PP}PP";
                    HpLabel.Text = $"HP - {(pk.Stat_HPCurrent / StartingHP) * 100}%";
                    await Task.Delay(refr, CancellationToken.None).ConfigureAwait(false); // Wait time between reads
                }
                LiveStats.Clear();
                HpLabel.Text = "          HP%";
                ViewBox.Text = "No Pokémon present.";
                sprite = "https://raw.githubusercontent.com/kwsch/PKHeX/master/PKHeX.Drawing.PokeSprite/Resources/img/Pokemon%20Sprite%20Overlays/starter.png";
                PokeSprite.Load(sprite);
                if (pk is PK9)
                {
                    PokeSprite.SizeMode = PictureBoxSizeMode.AutoSize;
                }
                Typing1.Visible = false;
                Typing1.Image = null;
                Typing2.Visible = false;
                Typing2.Image = null;
                Specialty.Visible = false;
                Specialty.Image = null;
            }
            View.Enabled = true;
        }


        public async Task<PK9> ReadInBattlePokemonSV(ulong offset, int size)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, CancellationToken.None).ConfigureAwait(false);
            var pk = new PK9(data);
            if (UniqueBox.Checked)
            {
                var ptr = new long[] { 0x42F3DD8, 0xD8, 0x48, 0x18, 0xD8, 0x1E0 };
                var ofs = await SwitchConnection.PointerAll(ptr, CancellationToken.None).ConfigureAwait(false);
                data = await SwitchConnection.ReadBytesAbsoluteAsync(ofs, size, CancellationToken.None).ConfigureAwait(false); // RaidLobbyPokemon
                pk = new PK9(data);
                return pk;
            }
            return pk;
        }

        public async Task<PK8> ReadInBattlePokemonSWSH(uint offset, int size)
        {
            var token = CancellationToken.None;
            byte[]? data = { 0 };
            PK8 pk = new();
            if (UniqueBox.Checked)
            {
                offset = 0x886A95B8;
                data = await SwitchConnection.ReadBytesAsync(offset, size, token).ConfigureAwait(false); // RaidPokemon
                pk = new PK8(data);
                return pk;
            }
            if (UniqueBox2.Checked)
            {
                IReadOnlyList<long>[] campers =
                {
                    new long[] { 0x2636120, 0x280, 0xD8, 0x78, 0x10, 0x98, 0x00 },
                    new long[] { 0x2636170, 0x2F0, 0x58, 0x130, 0x138, 0xD0 },
                    new long[] { 0x28ED668, 0x68, 0x1E8, 0x1D0, 0x128 },
                    new long[] { 0x296C030, 0x60, 0x40, 0x1B0, 0x58, 0x00 }
                };

                for (int i = 0; i < campers.Length; i++)
                {
                    var pointer = campers[i];
                    var ofs = await SwitchConnection.PointerAll(pointer, token).ConfigureAwait(false);
                    data = await SwitchConnection.ReadBytesAbsoluteAsync(ofs, size, token).ConfigureAwait(false);
                    pk = new PK8(data);
                    if (pk.Species != 0 && pk.Species < (int)Species.MAX_COUNT)
                        return pk;
                }
            }
            data = await SwitchConnection.ReadBytesAsync(offset, size, token).ConfigureAwait(false); // WildPokemon
            pk = new PK8(data);
            if (pk.Species == 0 || pk.Species > (int)Species.MAX_COUNT)
            {
                data = await SwitchConnection.ReadBytesAsync(0x886BC348, size, token).ConfigureAwait(false); // LegendaryPokemon
                pk = new PK8(data);
            }
            return pk;
        }

        public async Task<PA8> ReadInBattlePokemonLA(ulong offset, int size)
        {
            var token = CancellationToken.None;
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
            var pk = new PA8(data);
            return pk;
        }

        public async Task<PB8> ReadInBattlePokemonBDSP(ulong offset, int size)
        {
            var token = CancellationToken.None;
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
            var pk = new PB8(data);
            return pk;
        }

        public async Task<PB7> ReadInBattlePokemonLGPE(uint offset, int size)
        {
            var token = CancellationToken.None;
            var data = await SwitchConnection.ReadBytesMainAsync(offset, size, token).ConfigureAwait(false);
            var pk = new PB7(data);
            if (pk.Species == 0 || pk.Species > (int)Species.MAX_COUNT)
            {
                data = await SwitchConnection.ReadBytesAsync(0x9A118D68, size, token).ConfigureAwait(false);
                pk = new PB7(data);
            }
            return pk;
        }

        protected async Task<(bool, ulong)> ValidatePointerAll(IEnumerable<long> jumps, CancellationToken token)
        {
            var solved = await SwitchConnection.PointerAll(jumps, token).ConfigureAwait(false);
            return (solved != 0, solved);
        }

        public async Task<bool> IsOnOverworldTitle(CancellationToken token)
        {
            var ptr = new long[] { 0 };
            switch (GameType)
            {
                case (int)GameSelected.Scarlet or (int)GameSelected.Violet:
                    ptr = new long[] { 0x43A7848, 0x348, 0x10, 0xD8, 0x28 }; break;
                case (int)GameSelected.LA:
                    ptr = new long[] { 0x42C30E8, 0x1A9 }; break;
                case (int)GameSelected.BD:
                    ptr = new long[] { 0x4C59C98, 0xB8, 0x3C }; break;
                case (int)GameSelected.SP:
                    ptr = new long[] { 0x4E70D70, 0xB8, 0x3C }; break;
                case (int)GameSelected.SW or (int)GameSelected.SH:
                    {
                        var data = await SwitchConnection.ReadBytesAsync((uint)(GameType == (int)GameVersion.SH ? 0x3F128626 : 0x3F128624), 1, token).ConfigureAwait(false);
                        return data[0] == (GameType == (int)GameSelected.SH ? 0x40 : 0x41);
                    }
                case (int)GameSelected.LGP or (int)GameSelected.LGE:
                    {
                        var data = await SwitchConnection.ReadBytesMainAsync(0x163F694, 1, token).ConfigureAwait(false);
                        return data[0] == 0;
                    }
            }
            var (valid, offset) = await ValidatePointerAll(ptr, token).ConfigureAwait(false);
            if (!valid)
                return false;
            return await IsOnOverworld(offset, token).ConfigureAwait(false);
        }

        public async Task<bool> IsOnOverworld(ulong offset, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public static bool HasMark(IRibbonIndex pk, out RibbonIndex result)
        {
            result = default;
            for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
            {
                if (pk.GetRibbon((int)mark))
                {
                    result = mark;
                    return true;
                }
            }
            return false;
        }

        private void SanityCheck(PKM pk)
        {
            bool isValid = false;
            switch (GameType)
            {
                case (int)GameSelected.Scarlet or (int)GameSelected.Violet: isValid = PersonalTable.SV.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.SW or (int)GameSelected.SH: isValid = PersonalTable.SWSH.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.BD or (int)GameSelected.SP: isValid = PersonalTable.BDSP.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.LA: isValid = PersonalTable.LA.IsPresentInGame(pk.Species, pk.Form); break;
                case (int)GameSelected.LGP or (int)GameSelected.LGE: isValid = pk.Species < (int)Species.Mewtwo && pk.Species != (int)Species.Meltan && pk.Species != (int)Species.Melmetal; break;
            }
            if (!isValid || pk.Species < 0 || pk.Species > (int)Species.MAX_COUNT)
            {
                ViewBox.Text = "No Pokémon present.";
                View.Enabled = true;
                var sprite = "https://raw.githubusercontent.com/kwsch/PKHeX/master/PKHeX.Drawing.PokeSprite/Resources/img/Pokemon%20Sprite%20Overlays/starter.png";
                PokeSprite.Load(sprite);
                Typing1.Visible = false;
                Typing2.Visible = false;
                Specialty.Visible = false;
                LiveStats.Clear();
                return;
            }
        }
        private async void ReadEncounter_ClickAsync(object sender, EventArgs e)
        {
            var token = CancellationToken.None;
            View.Enabled = false;
            if (SwitchConnection.Connected)
            {
                ViewBox.Text = "Reading encounter...";
                switch (GameType)
                {
                    case (int)GameSelected.Scarlet or (int)GameSelected.Violet:
                        {
                            var ptr = new long[] { 0x42FD3C0, 0x10, 0x2D0, 0x2A0, 0x48, 0x2E0 };
                            var ofs = await SwitchConnection.PointerAll(ptr, token).ConfigureAwait(false);
                            var size = 0x158;
                            var pk = await ReadInBattlePokemonSV(ofs, size).ConfigureAwait(false);
                            SanityCheck(pk);
                            FillPokeData(pk, ofs, 0, size);
                            return;
                        }
                    case (int)GameSelected.SW or (int)GameSelected.SH:
                        {
                            if (UniqueBox.Checked && UniqueBox2.Checked)
                            {
                                MessageBox.Show("You have both unique boxes checked! Please select only one!");
                                System.Media.SystemSounds.Beep.Play();
                                View.Enabled = true;
                                UniqueBox.Checked = false;
                                UniqueBox2.Checked = false;
                                ViewBox.Text = "Click View!";
                                return;
                            }
                            uint ufs = 0x8FEA3648;
                            int size = 0x158;
                            var pk = await ReadInBattlePokemonSWSH(ufs, size).ConfigureAwait(false);
                            SanityCheck(pk);
                            FillPokeData(pk, 0, ufs, size);
                            break;
                        }
                    case (int)GameSelected.LA:
                        {
                            var ptr = new long[] { 0x42A6F00, 0xD0, 0xB8, 0x300, 0x70, 0x60, 0x98, 0x10, 0x00 };
                            var ofs = await SwitchConnection.PointerAll(ptr, token).ConfigureAwait(false);
                            var size = 0x168;
                            var pk = await ReadInBattlePokemonLA(ofs, size).ConfigureAwait(false);
                            SanityCheck(pk);
                            FillPokeData(pk, ofs, 0, size);
                        }; break;
                    case (int)GameSelected.BD:
                        {
                            var ptr = new long[] { 0x4C59EF0, 0x20, 0x98, 0x00, 0x20 };
                            var ofs = await SwitchConnection.PointerAll(ptr, token).ConfigureAwait(false);
                            var size = 0x168;
                            var pk = await ReadInBattlePokemonBDSP(ofs, size).ConfigureAwait(false);
                            SanityCheck(pk);
                            FillPokeData(pk, ofs, 0, size);
                        }; break;
                    case (int)GameSelected.SP:
                        {
                            var ptr = new long[] { 0x4E70FC8, 0x20, 0x98, 0x00, 0x20 };
                            var ofs = await SwitchConnection.PointerAll(ptr, token).ConfigureAwait(false);
                            int size = 0x168;
                            var pk = await ReadInBattlePokemonBDSP(ofs, size).ConfigureAwait(false);
                            SanityCheck(pk);
                            FillPokeData(pk, ofs, 0, size);
                        }; break;
                    case (int)GameSelected.LGP or (int)GameSelected.LGE:
                        {
                            uint ufs = 0x163EDC0;
                            int size = 0x158;
                            var pk = await ReadInBattlePokemonLGPE(ufs, size).ConfigureAwait(false);
                            SanityCheck(pk);
                            FillPokeData(pk, 0, ufs, size);
                            break;
                        }
                }
            }
        }

        private async void Window_Loaded()
        {
            var token = CancellationToken.None;
            int type = 0;
            string url = "https://raw.githubusercontent.com/zyro670/PokeTextures/main/icon_version/64x64/icon_version_";
            string title = await SwitchConnection.GetTitleID(token).ConfigureAwait(false);
            switch (title)
            {
                case ScarletID:
                    {
                        url += "SC.png";
                        type = (int)GameSelected.Scarlet;
                        WideView.Visible = false;
                        WideView.Text = "Raid View";
                        View.Enabled = false;
                        TrainerView.Visible = false;
                        DayCareView.Visible = false;
                        UniqueBox.Visible = true;
                        UniqueBox.Text = "Raid Lobby";
                        UniqueBox.Enabled = false;
                        OverworldView.Visible = true;
                        OverworldView.Enabled = false;
                        DayCareView.Visible = true;
                        break;
                    }
                case VioletID:
                    {
                        url += "VI.png";
                        type = (int)GameSelected.Violet;
                        WideView.Visible = false;
                        WideView.Text = "Raid View";
                        View.Enabled = false;
                        TrainerView.Visible = false;
                        DayCareView.Visible = false;
                        UniqueBox.Visible = true;
                        UniqueBox.Text = "Raid Lobby";
                        UniqueBox.Enabled = false;
                        OverworldView.Visible = true;
                        OverworldView.Enabled = false;
                        DayCareView.Visible = true;
                        break;
                    }
                case LegendsArceusID:
                    {
                        url += "LA.png";
                        type = (int)GameSelected.LA;
                        TrainerView.Visible = false;
                        DayCareView.Visible = false;
                        OverworldView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
                case ShiningPearlID:
                    {
                        url += "SP.png";
                        type = (int)GameSelected.SP;
                        TrainerView.Visible = false;
                        OverworldView.Visible = false;
                        DayCareView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
                case BrilliantDiamondID:
                    {
                        url += "BD.png";
                        type = (int)GameSelected.BD;
                        TrainerView.Visible = false;
                        OverworldView.Visible = false;
                        DayCareView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
                case SwordID:
                    {
                        url += "SW.png";
                        type = (int)GameSelected.SW;
                        UniqueBox.Visible = true; UniqueBox2.Visible = true;
                        UniqueBox.Text = "Raid"; UniqueBox2.Text = "Curry";
                        OverworldView.Visible = false;
                        DayCareView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
                case ShieldID:
                    {
                        url += "SH.png";
                        type = (int)GameSelected.SH;
                        UniqueBox.Visible = true; UniqueBox2.Visible = true;
                        UniqueBox.Text = "Raid"; UniqueBox2.Text = "Curry";
                        OverworldView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
                case EeveeID:
                    {
                        url += "LGE.png"; type = (int)GameSelected.LGE;
                        WideView.Enabled = false;
                        DayCareView.Visible = false;
                        OverworldView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
                case PikachuID:
                    {
                        url += "LGP.png"; type = (int)GameSelected.LGP;
                        WideView.Enabled = false;
                        DayCareView.Visible = false;
                        OverworldView.Visible = false;
                        Raids.Visible = false;
                        break;
                    }
            }

            OriginIcon.ImageLocation = url;
            GameType = type;
            ViewBox.Text = "Click View!";
            var bg = "https://raw.githubusercontent.com/kwsch/PKHeX/master/PKHeX.Drawing.PokeSprite/Resources/img/Pokemon%20Sprite%20Overlays/starter.png";
            PokeSprite.ImageLocation = bg;
        }

        private void CaptureWindow_Click(object sender, EventArgs e)
        {
            Bitmap FormScreenShot = new(Width, Height);
            Graphics G = Graphics.FromImage(FormScreenShot);
            G.CopyFromScreen(Location, new Point(0, 0), Size);
            Clipboard.SetImage(FormScreenShot);
        }

        private void RefreshStats_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chextBox = (CheckBox)sender;
            if (chextBox.Checked)
                Properties.Settings.Default.RefreshStats = true;
            else
                Properties.Settings.Default.RefreshStats = false;

            Properties.Settings.Default.Save();
        }

        private void WideView_Click(object sender, EventArgs e)
        {
            switch (GameType)
            {
                case (int)GameSelected.Scarlet or (int)GameSelected.Violet:
                    {
                        MessageBox.Show("Wide View currently not supported");
                        break;
                    }
                case (int)GameSelected.LA:
                    {
                        using WideViewerLA WideForm = new(SwitchConnection);
                        WideForm.ShowDialog();
                        break;
                    }
                case (int)GameSelected.BD or (int)GameSelected.SP:
                    {
                        using WideViewerBDSP WideForm = new(SwitchConnection);
                        WideForm.ShowDialog();
                        break;
                    }
                case (int)GameSelected.SW or (int)GameSelected.SH:
                    {
                        WideView.Text = "Preparing...";
                        WideView.Enabled = false;
                        using WideViewerSWSH WideForm = new(SwitchConnection);
                        WideForm.ShowDialog();
                        WideView.Text = "WideView";
                        WideView.Enabled = true;
                        break;
                    }
            }
        }

        private void BoxView_Click(object sender, EventArgs e)
        {
            using BoxViewerMode BoxForm = new(GameType, SwitchConnection);
            BoxForm.ShowDialog();
        }

        private void BattleView_Click(object sender, EventArgs e)
        {
            using TrainerViewer Form = new(GameType, SwitchConnection);
            Form.ShowDialog();
        }

        private void OverworldView_Click(object sender, EventArgs e)
        {
            using OverworldViewSV WideForm = new(SwitchConnection);
            WideForm.ShowDialog();
        }

        private void InGameScreenshot_Click(object sender, EventArgs e)
        {
            var fn = "screenshot.jpg";
            if (!SwitchConnection.Connected)
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show($"No device connected! In-Game Screenshot not possible!");
                return;
            }
            var bytes = SwitchConnection.Screengrab(CancellationToken.None).Result;
            File.WriteAllBytes(fn, bytes);
            FileStream stream = new(fn, FileMode.Open);
            var img = Image.FromStream(stream);
            Clipboard.SetImage(img);
            using (Form form = new())
            {
                Bitmap vimg = (Bitmap)img;

                form.StartPosition = FormStartPosition.CenterScreen;

                Bitmap original = vimg;
                Bitmap resized = new(original, new Size(original.Width / 2, original.Height / 2));

                PictureBox pb = new()
                {
                    Dock = DockStyle.Fill,
                    Image = resized
                };
                form.Size = resized.Size;

                form.Controls.Add(pb);
                form.ShowDialog();
            }
            stream.Dispose();
            File.Delete(fn);
        }

        private void DayCareView_Click(object sender, EventArgs e)
        {
            using Egg_Viewer WideForm = new(SwitchConnection);
            WideForm.ShowDialog();
        }

        private void Raids_Click(object sender, EventArgs e)
        {
            using RaidCodeEntry WideForm = new(SwitchConnection);
            WideForm.ShowDialog();
        }        
    }
}