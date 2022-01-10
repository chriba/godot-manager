using Godot;
using GodotSharpExtras;
using System.Threading.Tasks;
using System.IO.Compression;

public class GodotLineEntry : HBoxContainer
{
    [Signal]
    public delegate void install_clicked(GodotLineEntry entry);

    [Signal]
    public delegate void uninstall_clicked(GodotLineEntry entry);

    [Signal]
    public delegate void default_selected(GodotLineEntry entry);

#region Private Node Variables
    [NodePath("vc/VersionTag")]
    private Label _label = null;
    [NodePath("vc/hc/Source")]
    private Label _source = null;
    [NodePath("vc/hc/Filesize")]
    private Label _filesize = null;
    [NodePath("Download")]
    private TextureRect _download = null;
    [NodePath("Default")]
    private TextureRect _default = null;

    [NodePath("vc/DownloadProgress")]
    private HBoxContainer _downloadProgress = null;
    [NodePath("vc/DownloadProgress/ProgressBar")]
    private ProgressBar _progressBar = null;
    [NodePath("vc/DownloadProgress/Filesize")]
    private Label _fileSize = null;

    private StreamTexture downloadIcon;
    private StreamTexture uninstallIcon;
#endregion

#region Private String Variables
    private string sLabel = "Godot Version x.x.x (Stable)";
    private string sSource = "Source: TuxFamily.org";
    private string sFilesize = "Size: 32MB";
    private bool bDownloaded = false;
    private bool bDefault = false;
    private bool bMono = false;
    private GodotVersion gvGodotVersion = null;
    private GithubVersion gvGithubVersion = null;
    private Downloader Downloader = null;
#endregion

#region Public Accessors
    public GodotVersion GodotVersion {
        get {
            return gvGodotVersion;
        }

        set {
            gvGodotVersion = value;
            if (value != null) {
                Mono = value.IsMono;
                Label = value.Tag;
            }
        }
    }

    public bool Mono {
        get {
            return bMono;
        }

        set {
            bMono = value;
            GithubVersion = gvGithubVersion;
        }
    }

    public GithubVersion GithubVersion {
        get {
            return gvGithubVersion;
        }

        set {
            gvGithubVersion = value;
            if (value == null)
                return;
            Label = value.Name;
            switch(Platform.OperatingSystem) {
                case "Windows":
                case "UWP (Windows 10)":
                    if (Mono) {
                        if (Platform.Bits == "32") {
                            Source = value.Mono.Win32;
                            Filesize = Util.FormatSize(value.Mono.Win32_Size);
                        } else if (Platform.Bits == "64") {
                            Source = value.Mono.Win64;
                            Filesize = Util.FormatSize(value.Mono.Win64_Size);                    
                        }
                    } else {
                        if (Platform.Bits == "32") {
                            Source = value.Standard.Win32;
                            Filesize = Util.FormatSize(value.Standard.Win32_Size);
                        } else if (Platform.Bits == "64") {
                            Source = value.Standard.Win64;
                            Filesize = Util.FormatSize(value.Standard.Win64_Size);                    
                        }
                    }
                    break;

                case "Linux (or BSD)":
                    if (Mono) {
                        if (Platform.Bits == "32") {
                            Source = value.Mono.Linux32;
                            Filesize = Util.FormatSize(value.Mono.Linux32_Size);
                        } else if (Platform.Bits == "64") {
                            Source = value.Mono.Linux64;
                            Filesize = Util.FormatSize(value.Mono.Linux64_Size);
                        }
                    } else {
                        if (Platform.Bits == "32") {
                            Source = value.Standard.Linux32;
                            Filesize = Util.FormatSize(value.Standard.Linux32_Size);
                        } else if (Platform.Bits == "64") {
                            Source = value.Standard.Linux64;
                            Filesize = Util.FormatSize(value.Standard.Linux64_Size);
                        }
                    }
                    break;

                case "macOS":
                    if (Mono) {
                        Source = value.Mono.OSX;
                        Filesize = Util.FormatSize(value.Mono.OSX_Size);
                    } else {
                        Source = value.Standard.OSX;
                        Filesize = Util.FormatSize(value.Standard.OSX_Size);
                    }
                    break;
                
                default:
                    break;
            }
        }
    }

	public string Label {
        get {
            return sLabel;
        }
        set {
            sLabel = value + (Mono ? " - Mono" : "");
            if (_label != null)
                _label.Text = $"Godot {value + (Mono ? " - Mono" : "")}";
        }
    }

    public string Source {
        get {
            return sSource;
        }

        set {
            sSource = value;
            if (_source != null)
                _source.Text = $"Source: {value}";
        }
    }

    public string Filesize {
        get {
            return sFilesize;
        }
        set {
            sFilesize = value;
            if (_filesize != null)
                _filesize.Text = $"Size: {value}";
        }
    }

    [Export]
    public bool Downloaded {
        get {
            return bDownloaded;
        }

        set {
            bDownloaded = value;
            if (_download != null) {
                ToggleDownloadUninstall(bDownloaded);
                _default.Visible = value;
            }
        }
    }

    public bool IsDownloaded {
        get {
            return bDownloaded;
        }
    }

    public bool IsDefault {
        get {
            return bDefault;
        }
    }
#endregion


    public override void _Ready()
    {
        this.OnReady();

        GodotVersion = gvGodotVersion;
        GithubVersion = gvGithubVersion;
        // Label = sLabel;
        // Source = sSource;
        // Filesize = sFilesize;

        _download.Connect("gui_input", this, "OnDownload_GuiInput");
        _default.Connect("gui_input", this, "OnDefault_GuiInput");
        downloadIcon = GD.Load<StreamTexture>("res://Assets/Icons/download.svg");
        uninstallIcon = GD.Load<StreamTexture>("res://Assets/Icons/uninstall.svg");
        Downloaded = bDownloaded;
        ToggleDefault(bDefault);
    }

    public void ToggleDownloadUninstall(bool value) {
        if (value) {
            _download.Texture = uninstallIcon;
            _download.SelfModulate = new Color("fc9c9c");
        }
        else {
            _download.Texture = downloadIcon;
            _download.SelfModulate = new Color("7defa7");
        }
    }

    public void ToggleDownloadProgress(bool value) {
        _downloadProgress.Visible = value;
    }

    public void UpdateProgress(int bytes, int total_bytes) {
        float progress = bytes * 100.0f / total_bytes;
        _progressBar.Value = progress;
        _fileSize.Text = $"{Util.FormatSize(bytes)}/{Util.FormatSize(total_bytes)}";
    }

    public void ToggleDefault(bool value) {
        bDefault = value;
        if (_default != null) {
            if (value) {
                _default.SelfModulate = new Color("ffd684");
            } else {
                _default.SelfModulate = new Color("ffffff");
            }
        }
    }

     void OnDownload_GuiInput(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && (ButtonList)iemb.ButtonIndex == ButtonList.Left) {
            if (_download.Texture == downloadIcon)
                EmitSignal("install_clicked", this);
            else
                EmitSignal("uninstall_clicked", this);
            ToggleDownloadUninstall((_download.Texture == downloadIcon));
        }
    }

    void OnDefault_GuiInput(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && (ButtonList)iemb.ButtonIndex == ButtonList.Left) {
            EmitSignal("default_selected", this);
        }
    }

    void OnChunkReceived(int bytes) {
        _progressBar.Value += bytes;
        _fileSize.Text = $"{Util.FormatSize(_progressBar.Value)}/{Util.FormatSize(Downloader.totalSize)}";
    }

    public GodotVersion CreateGodotVersion() {
        GodotVersion gv = new GodotVersion();
        string gdFile = Mono ? new System.Uri(GithubVersion.PlatformMonoDownloadURL).AbsolutePath.GetFile() : new System.Uri(GithubVersion.PlatformDownloadURL).AbsolutePath.GetFile();
        string gdName = Mono ? gdFile.ReplaceN(".zip","") : GithubVersion.Name;
        gv.Id = System.Guid.NewGuid().ToString();
        gv.Tag = GithubVersion.Name;
        gv.Url = Mono ? GithubVersion.PlatformMonoDownloadURL : GithubVersion.PlatformDownloadURL;
        gv.Location = $"user://versions/{gdName}";
        gv.CacheLocation = $"user://cache/Godot/{gdFile}";
        gv.DownloadedDate = System.DateTime.UtcNow;
        gv.GithubVersion = GithubVersion;
        gv.IsMono = Mono;
        GodotVersion = gv;
        return gv;
    }

    public async Task StartDownload() {
        Downloader = Downloader.DownloadGithub(GithubVersion,Mono);
        string outFile = $"user://cache/Godot/{Downloader.downloadUri.AbsolutePath.GetFile()}";
        string instDir = $"user://versions/{(Mono ? "" : GithubVersion.Name)}";
        Downloader.Connect("chunk_received", this, "OnChunkReceived");
        _progressBar.MinValue = 0;
        _progressBar.MaxValue = Downloader.totalSize;
        _progressBar.Value = 0;
        _fileSize.Text = $"{Util.FormatSize(0)}/{Util.FormatSize(Downloader.totalSize)}";
        Task<bool> bres = Downloader.DownloadFile(outFile);
        while (!bres.IsCompleted)
            await this.IdleFrame();
        if (bres.Result) {
            ZipFile.ExtractToDirectory(ProjectSettings.GlobalizePath(outFile),
                                        ProjectSettings.GlobalizePath(instDir));
        }
    }
}
