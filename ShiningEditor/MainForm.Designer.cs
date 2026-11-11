namespace ShiningEditor;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip _menu;
    private ToolStripMenuItem _mnuFile, _mnuOpen, _mnuSave, _mnuReload, _mnuExit;
    private ToolStripMenuItem _mnuGame;
    private ToolStripMenuItem _mnuHelp, _mnuInstructions, _mnuViewLog, _mnuClearLog, _mnuAbout;

    private Panel _header;
    private Label _lblFile, _lblInfo, _lblTitle;
    private Panel _goldPanel;
    private Label _lblGame, _lblGold;
    private ComboBox _cboGame;
    private NumericUpDown _numGold;
    private Button _btnOpen, _btnSave;

    private Label _lblBanner;

    private Panel _leftPanel;
    private Label _lblParty;
    private ListBox _lstChars;

    private Panel _divider;

    private Panel _coverPanel;
    private PictureBox _picCover;

    private Panel _rightPanel;
    private Panel _namePlate;
    private ShiningTabControl _tabs;
    private Panel _tabStatus, _tabItems, _tabMagic;
    private TableLayoutPanel _fieldTable;
    private Label _lblItemsHdr, _itemsNote, _lblMagicHdr, _magicNote;
    private ListBox _itemList, _spellList;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _menu = new MenuStrip();
        _mnuFile = new ToolStripMenuItem();
        _mnuOpen = new ToolStripMenuItem();
        _mnuSave = new ToolStripMenuItem();
        _mnuReload = new ToolStripMenuItem();
        _mnuExit = new ToolStripMenuItem();
        _mnuGame = new ToolStripMenuItem();
        _mnuHelp = new ToolStripMenuItem();
        _mnuInstructions = new ToolStripMenuItem();
        _mnuViewLog = new ToolStripMenuItem();
        _mnuClearLog = new ToolStripMenuItem();
        _mnuAbout = new ToolStripMenuItem();
        _header = new Panel();
        _lblTitle = new Label();
        _goldPanel = new Panel();
        _lblGame = new Label();
        _cboGame = new ComboBox();
        _lblGold = new Label();
        _numGold = new NumericUpDown();
        _btnOpen = new Button();
        _btnSave = new Button();
        _lblInfo = new Label();
        _lblFile = new Label();
        _lblBanner = new Label();
        _leftPanel = new Panel();
        _lstChars = new ListBox();
        _lblParty = new Label();
        _divider = new Panel();
        _coverPanel = new Panel();
        _picCover = new PictureBox();
        _rightPanel = new Panel();
        _tabs = new ShiningTabControl();
        _tabStatus = new Panel();
        _fieldTable = new TableLayoutPanel();
        _tabItems = new Panel();
        _itemList = new ListBox();
        _lblItemsHdr = new Label();
        _itemsNote = new Label();
        _tabMagic = new Panel();
        _spellList = new ListBox();
        _lblMagicHdr = new Label();
        _magicNote = new Label();
        _namePlate = new Panel();
        _menu.SuspendLayout();
        _header.SuspendLayout();
        _goldPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_numGold).BeginInit();
        _leftPanel.SuspendLayout();
        _coverPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_picCover).BeginInit();
        _rightPanel.SuspendLayout();
        _tabs.SuspendLayout();
        _tabStatus.SuspendLayout();
        _tabItems.SuspendLayout();
        _tabMagic.SuspendLayout();
        SuspendLayout();
        //
        // _menu
        //
        _menu.ImageScalingSize = new Size(24, 24);
        _menu.Items.AddRange(new ToolStripItem[] { _mnuFile, _mnuGame, _mnuHelp });
        _menu.Location = new Point(0, 0);
        _menu.Name = "_menu";
        _menu.Padding = new Padding(9, 3, 0, 3);
        _menu.Size = new Size(1452, 35);
        _menu.TabIndex = 6;
        //
        // _mnuFile
        //
        _mnuFile.DropDownItems.AddRange(new ToolStripItem[] { _mnuOpen, _mnuSave, _mnuReload, new ToolStripSeparator(), _mnuExit });
        _mnuFile.Name = "_mnuFile";
        _mnuFile.Text = "&File";
        _mnuOpen.Name = "_mnuOpen";
        _mnuOpen.ShortcutKeys = Keys.Control | Keys.O;
        _mnuOpen.Text = "&Open Save State…";
        _mnuSave.Name = "_mnuSave";
        _mnuSave.ShortcutKeys = Keys.Control | Keys.S;
        _mnuSave.Text = "&Save";
        _mnuReload.Name = "_mnuReload";
        _mnuReload.Text = "&Reload from Disk (discard changes)";
        _mnuExit.Name = "_mnuExit";
        _mnuExit.Text = "E&xit";
        //
        // _mnuGame (items are added at runtime from Games.All)
        //
        _mnuGame.Name = "_mnuGame";
        _mnuGame.Text = "&Game";
        //
        // _mnuHelp
        //
        _mnuHelp.DropDownItems.AddRange(new ToolStripItem[] { _mnuInstructions, new ToolStripSeparator(), _mnuViewLog, _mnuClearLog, new ToolStripSeparator(), _mnuAbout });
        _mnuHelp.Name = "_mnuHelp";
        _mnuHelp.Text = "&Help";
        _mnuInstructions.Name = "_mnuInstructions";
        _mnuInstructions.ShortcutKeys = Keys.F1;
        _mnuInstructions.Text = "How to Use…";
        _mnuViewLog.Name = "_mnuViewLog";
        _mnuViewLog.Text = "View Error Log…";
        _mnuClearLog.Name = "_mnuClearLog";
        _mnuClearLog.Text = "Clear Error Log";
        _mnuAbout.Name = "_mnuAbout";
        _mnuAbout.Text = "About Shining Editor…";
        //
        // _header
        //
        _header.Controls.Add(_lblTitle);
        _header.Controls.Add(_goldPanel);
        _header.Controls.Add(_lblInfo);
        _header.Controls.Add(_lblFile);
        _header.Dock = DockStyle.Top;
        _header.Location = new Point(0, 35);
        _header.Name = "_header";
        _header.Padding = new Padding(17, 13, 17, 13);
        _header.Size = new Size(1452, 153);
        _header.TabIndex = 5;
        //
        // _lblTitle
        //
        _lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _lblTitle.Location = new Point(1012, 13);
        _lblTitle.Name = "_lblTitle";
        _lblTitle.Size = new Size(423, 62);
        _lblTitle.TabIndex = 3;
        _lblTitle.Text = "SHINING EDITOR";
        _lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        //
        // _goldPanel
        //
        _goldPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _goldPanel.Controls.Add(_lblGame);
        _goldPanel.Controls.Add(_cboGame);
        _goldPanel.Controls.Add(_lblGold);
        _goldPanel.Controls.Add(_numGold);
        _goldPanel.Controls.Add(_btnOpen);
        _goldPanel.Controls.Add(_btnSave);
        _goldPanel.Location = new Point(20, 87);
        _goldPanel.Name = "_goldPanel";
        _goldPanel.Size = new Size(1415, 57);
        _goldPanel.TabIndex = 0;
        //
        // _lblGame
        //
        _lblGame.AutoSize = true;
        _lblGame.Location = new Point(0, 12);
        _lblGame.Name = "_lblGame";
        _lblGame.TabIndex = 0;
        _lblGame.Text = "GAME";
        //
        // _cboGame
        //
        _cboGame.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboGame.FlatStyle = FlatStyle.Flat;
        _cboGame.Location = new Point(80, 8);
        _cboGame.Name = "_cboGame";
        _cboGame.Size = new Size(290, 33);
        _cboGame.TabIndex = 1;
        //
        // _lblGold
        //
        _lblGold.AutoSize = true;
        _lblGold.Location = new Point(400, 10);
        _lblGold.Name = "_lblGold";
        _lblGold.TabIndex = 2;
        _lblGold.Text = "GOLD";
        //
        // _numGold
        //
        _numGold.Location = new Point(480, 6);
        _numGold.Name = "_numGold";
        _numGold.Size = new Size(220, 31);
        _numGold.TabIndex = 3;
        _numGold.ThousandsSeparator = true;
        _numGold.TextAlign = HorizontalAlignment.Right;
        //
        // _btnOpen
        //
        _btnOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnOpen.Location = new Point(1060, 0);
        _btnOpen.Name = "_btnOpen";
        _btnOpen.Size = new Size(169, 53);
        _btnOpen.TabIndex = 4;
        _btnOpen.Text = "Open…";
        //
        // _btnSave
        //
        _btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnSave.Location = new Point(1243, 0);
        _btnSave.Name = "_btnSave";
        _btnSave.Size = new Size(169, 53);
        _btnSave.TabIndex = 5;
        _btnSave.Text = "Save";
        //
        // _lblInfo
        //
        _lblInfo.AutoSize = true;
        _lblInfo.Location = new Point(20, 50);
        _lblInfo.Name = "_lblInfo";
        _lblInfo.TabIndex = 1;
        //
        // _lblFile
        //
        _lblFile.AutoSize = true;
        _lblFile.Location = new Point(20, 17);
        _lblFile.Name = "_lblFile";
        _lblFile.TabIndex = 2;
        _lblFile.Text = "No save state loaded.";
        //
        // _lblBanner
        //
        _lblBanner.Dock = DockStyle.Top;
        _lblBanner.Location = new Point(0, 188);
        _lblBanner.Name = "_lblBanner";
        _lblBanner.Padding = new Padding(17, 0, 17, 0);
        _lblBanner.Size = new Size(1452, 0);
        _lblBanner.TabIndex = 4;
        _lblBanner.TextAlign = ContentAlignment.MiddleLeft;
        _lblBanner.Visible = false;
        //
        // _leftPanel
        //
        _leftPanel.Controls.Add(_lstChars);
        _leftPanel.Controls.Add(_lblParty);
        _leftPanel.Dock = DockStyle.Left;
        _leftPanel.Location = new Point(0, 188);
        _leftPanel.Name = "_leftPanel";
        _leftPanel.Padding = new Padding(17, 17, 9, 20);
        _leftPanel.Size = new Size(430, 983);
        _leftPanel.TabIndex = 3;
        //
        // _lstChars
        //
        _lstChars.BorderStyle = BorderStyle.None;
        _lstChars.Dock = DockStyle.Fill;
        _lstChars.DrawMode = DrawMode.OwnerDrawFixed;
        _lstChars.IntegralHeight = false;
        _lstChars.ItemHeight = 120;
        _lstChars.Name = "_lstChars";
        _lstChars.TabIndex = 0;
        //
        // _lblParty
        //
        _lblParty.Dock = DockStyle.Top;
        _lblParty.Name = "_lblParty";
        _lblParty.Size = new Size(404, 47);
        _lblParty.TabIndex = 1;
        _lblParty.Text = "FORCE MEMBERS";
        //
        // _divider
        //
        _divider.Dock = DockStyle.Left;
        _divider.Name = "_divider";
        _divider.Size = new Size(3, 983);
        _divider.TabIndex = 2;
        //
        // _coverPanel
        //
        _coverPanel.Controls.Add(_picCover);
        _coverPanel.Dock = DockStyle.Left;
        _coverPanel.Name = "_coverPanel";
        _coverPanel.Padding = new Padding(24, 26, 24, 26);
        _coverPanel.Size = new Size(320, 983);
        _coverPanel.TabIndex = 1;
        //
        // _picCover
        //
        _picCover.Dock = DockStyle.Top;
        _picCover.Name = "_picCover";
        _picCover.Size = new Size(272, 380);
        _picCover.SizeMode = PictureBoxSizeMode.Zoom;
        _picCover.TabIndex = 0;
        _picCover.TabStop = false;
        //
        // _rightPanel
        //
        _rightPanel.Controls.Add(_tabs);
        _rightPanel.Controls.Add(_namePlate);
        _rightPanel.Dock = DockStyle.Fill;
        _rightPanel.Name = "_rightPanel";
        _rightPanel.Padding = new Padding(23, 20, 23, 20);
        _rightPanel.TabIndex = 0;
        //
        // _tabs
        //
        // Pages are attached at runtime with _tabs.SetPages (depends on the game).
        _tabs.Dock = DockStyle.Fill;
        _tabs.Name = "_tabs";
        _tabs.TabIndex = 0;
        //
        // _tabStatus
        //
        _tabStatus.AutoScroll = true;
        _tabStatus.Controls.Add(_fieldTable);
        _tabStatus.Name = "_tabStatus";
        _tabStatus.Padding = new Padding(23, 20, 23, 20);
        _tabStatus.TabIndex = 0;
        _tabStatus.Text = "STATUS";
        //
        // _fieldTable
        //
        _fieldTable.ColumnCount = 4;
        _fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        // Sized explicitly to the page width in MainForm.FitFieldTable.
        _fieldTable.Name = "_fieldTable";
        _fieldTable.TabIndex = 0;
        //
        // _tabItems
        //
        _tabItems.Controls.Add(_itemList);
        _tabItems.Controls.Add(_lblItemsHdr);
        _tabItems.Controls.Add(_itemsNote);
        _tabItems.Name = "_tabItems";
        _tabItems.Padding = new Padding(23, 20, 23, 20);
        _tabItems.TabIndex = 1;
        _tabItems.Text = "ITEM";
        //
        // _itemList
        //
        _itemList.BorderStyle = BorderStyle.None;
        _itemList.Dock = DockStyle.Fill;
        _itemList.DrawMode = DrawMode.OwnerDrawFixed;
        _itemList.IntegralHeight = false;
        _itemList.ItemHeight = 34;
        _itemList.Name = "_itemList";
        _itemList.SelectionMode = SelectionMode.None;
        _itemList.TabIndex = 0;
        //
        // _lblItemsHdr
        //
        _lblItemsHdr.Dock = DockStyle.Top;
        _lblItemsHdr.Name = "_lblItemsHdr";
        _lblItemsHdr.Size = new Size(669, 43);
        _lblItemsHdr.TabIndex = 1;
        _lblItemsHdr.Text = "ITEMS";
        //
        // _itemsNote
        //
        _itemsNote.Dock = DockStyle.Bottom;
        _itemsNote.Name = "_itemsNote";
        _itemsNote.Size = new Size(669, 37);
        _itemsNote.TabIndex = 2;
        //
        // _tabMagic
        //
        _tabMagic.Controls.Add(_spellList);
        _tabMagic.Controls.Add(_lblMagicHdr);
        _tabMagic.Controls.Add(_magicNote);
        _tabMagic.Name = "_tabMagic";
        _tabMagic.Padding = new Padding(23, 20, 23, 20);
        _tabMagic.TabIndex = 2;
        _tabMagic.Text = "MAGIC";
        //
        // _spellList
        //
        _spellList.BorderStyle = BorderStyle.None;
        _spellList.Dock = DockStyle.Fill;
        _spellList.DrawMode = DrawMode.OwnerDrawFixed;
        _spellList.IntegralHeight = false;
        _spellList.ItemHeight = 34;
        _spellList.Name = "_spellList";
        _spellList.SelectionMode = SelectionMode.None;
        _spellList.TabIndex = 0;
        //
        // _lblMagicHdr
        //
        _lblMagicHdr.Dock = DockStyle.Top;
        _lblMagicHdr.Name = "_lblMagicHdr";
        _lblMagicHdr.Size = new Size(669, 43);
        _lblMagicHdr.TabIndex = 1;
        _lblMagicHdr.Text = "MAGIC";
        //
        // _magicNote
        //
        _magicNote.Dock = DockStyle.Bottom;
        _magicNote.Name = "_magicNote";
        _magicNote.Size = new Size(669, 37);
        _magicNote.TabIndex = 2;
        //
        // _namePlate
        //
        _namePlate.Dock = DockStyle.Top;
        _namePlate.Name = "_namePlate";
        _namePlate.Size = new Size(723, 107);
        _namePlate.TabIndex = 1;
        //
        // MainForm
        //
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1452, 1100);
        Controls.Add(_rightPanel);
        Controls.Add(_coverPanel);
        Controls.Add(_divider);
        Controls.Add(_leftPanel);
        Controls.Add(_lblBanner);
        Controls.Add(_header);
        Controls.Add(_menu);
        KeyPreview = true;
        MainMenuStrip = _menu;
        MinimumSize = new Size(1300, 900);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Shining Editor";
        _menu.ResumeLayout(false);
        _menu.PerformLayout();
        _header.ResumeLayout(false);
        _header.PerformLayout();
        _goldPanel.ResumeLayout(false);
        _goldPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_numGold).EndInit();
        _leftPanel.ResumeLayout(false);
        _coverPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_picCover).EndInit();
        _rightPanel.ResumeLayout(false);
        _tabs.ResumeLayout(false);
        _tabStatus.ResumeLayout(false);
        _tabStatus.PerformLayout();
        _tabItems.ResumeLayout(false);
        _tabMagic.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
