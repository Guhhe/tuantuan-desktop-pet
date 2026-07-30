using System.Drawing;
using Forms = System.Windows.Forms;

namespace TuantuanDesktopPet;

internal sealed class TrayService : IDisposable
{
    private const string ApplicationName = "团团桌宠";

    private readonly Forms.NotifyIcon _icon;
    private readonly Icon _applicationIcon;
    private readonly Forms.ToolStripMenuItem _pause;
    private readonly Forms.ToolStripMenuItem _topmost;
    private readonly Forms.ToolStripMenuItem _mouseFollow;
    private readonly Forms.ToolStripMenuItem _walking;
    private readonly Forms.ToolStripMenuItem _sizeMenu;
    private readonly Forms.ToolStripMenuItem _autoStart;
    private readonly Forms.ToolStripMenuItem _autoHide;
    private readonly Forms.ToolStripMenuItem _pets;
    private readonly ScalePicker _scalePicker;
    private readonly Action<string> _selectPet;

    internal TrayService(
        Action togglePause,
        Action toggleTopmost,
        Action toggleMouseFollow,
        Action toggleWalking,
        Action<double> setScale,
        Action toggleAutoStart,
        Action toggleAutoHide,
        Action importPet,
        Action<string> selectPet,
        Action resetPosition,
        Action exit,
        Action wake)
    {
        _selectPet = selectPet;
        var menu = new Forms.ContextMenuStrip();

        _pets = new Forms.ToolStripMenuItem("默认宠物");
        _pets.DropDownItems.Add(new Forms.ToolStripMenuItem("导入新宠物…", null, (_, _) => importPet()));
        _pets.DropDownItems.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_pets);
        menu.Items.Add(new Forms.ToolStripSeparator());

        _pause = new Forms.ToolStripMenuItem("暂停", null, (_, _) => togglePause());
        _topmost = new Forms.ToolStripMenuItem("始终置顶", null, (_, _) => toggleTopmost());
        _mouseFollow = new Forms.ToolStripMenuItem("跟随鼠标", null, (_, _) => toggleMouseFollow());
        _walking = new Forms.ToolStripMenuItem("自主走动", null, (_, _) => toggleWalking());
        menu.Items.Add(_pause);
        menu.Items.Add(_topmost);
        menu.Items.Add(_mouseFollow);
        menu.Items.Add(_walking);

        _scalePicker = new ScalePicker(setScale);
        _sizeMenu = new Forms.ToolStripMenuItem("尺寸：75%");
        _sizeMenu.DropDownItems.Add(new Forms.ToolStripControlHost(_scalePicker)
        {
            AutoSize = false,
            Size = _scalePicker.Size,
            Margin = Forms.Padding.Empty,
            Padding = Forms.Padding.Empty
        });
        menu.Items.Add(_sizeMenu);

        _autoStart = new Forms.ToolStripMenuItem("开机启动", null, (_, _) => toggleAutoStart());
        _autoHide = new Forms.ToolStripMenuItem("全屏自动隐藏", null, (_, _) => toggleAutoHide());
        menu.Items.Add(_autoStart);
        menu.Items.Add(_autoHide);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("重置位置", null, (_, _) => resetPosition()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("退出", null, (_, _) => exit()));

        _applicationIcon = LoadApplicationIcon();
        _icon = new Forms.NotifyIcon
        {
            Text = ApplicationName,
            Icon = _applicationIcon,
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => wake();
    }

    internal void UpdatePets(
        IReadOnlyList<PetDescriptor> pets,
        string selectedPetId,
        string selectedDisplayName)
    {
        while (_pets.DropDownItems.Count > 2)
        {
            _pets.DropDownItems.RemoveAt(2);
        }

        foreach (var pet in pets)
        {
            var capturedId = pet.Id;
            var label = pet.IsBuiltIn ? $"{pet.DisplayName}（内置）" : pet.DisplayName;
            var item = new Forms.ToolStripMenuItem(label, null, (_, _) => _selectPet(capturedId))
            {
                Checked = string.Equals(pet.Id, selectedPetId, StringComparison.OrdinalIgnoreCase),
                ToolTipText = pet.Description
            };
            _pets.DropDownItems.Add(item);
        }

        var text = $"{ApplicationName} · {selectedDisplayName}";
        _icon.Text = text.Length <= 63 ? text : text[..63];
    }

    internal void Update(
        bool paused,
        bool topmost,
        bool mouseFollow,
        bool walking,
        double scale,
        bool autoStart,
        bool autoHide)
    {
        _pause.Text = paused ? "继续" : "暂停";
        _pause.Checked = paused;
        _topmost.Checked = topmost;
        _mouseFollow.Checked = mouseFollow;
        _walking.Checked = walking;
        _autoStart.Checked = autoStart;
        _autoHide.Checked = autoHide;

        var percent = (int)Math.Round(scale * 100);
        _sizeMenu.Text = $"尺寸：{percent}%";
        _scalePicker.SetScale(scale);
    }

    internal void ShowMenu()
    {
        _icon.ContextMenuStrip?.Show(Forms.Cursor.Position);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _applicationIcon.Dispose();
    }

    private static Icon LoadApplicationIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                using var extracted = Icon.ExtractAssociatedIcon(executablePath);
                if (extracted is not null)
                {
                    return (Icon)extracted.Clone();
                }
            }
        }
        catch
        {
            // A generic Windows icon remains a safe fallback.
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private sealed class ScalePicker : Forms.UserControl
    {
        private readonly Forms.TrackBar _slider;
        private readonly Forms.NumericUpDown _number;
        private readonly Action<double> _changed;
        private bool _updating;

        internal ScalePicker(Action<double> changed)
        {
            _changed = changed;
            Size = new Size(250, 58);
            BackColor = SystemColors.Control;

            _slider = new Forms.TrackBar
            {
                Minimum = 50,
                Maximum = 200,
                Value = 75,
                TickFrequency = 25,
                SmallChange = 5,
                LargeChange = 25,
                AutoSize = false,
                Size = new Size(165, 48),
                Location = new Point(2, 4)
            };
            _number = new Forms.NumericUpDown
            {
                Minimum = 50,
                Maximum = 200,
                Value = 75,
                Increment = 5,
                TextAlign = Forms.HorizontalAlignment.Right,
                Size = new Size(60, 25),
                Location = new Point(174, 12)
            };
            var percent = new Forms.Label
            {
                Text = "%",
                AutoSize = true,
                Location = new Point(234, 16)
            };

            _slider.ValueChanged += (_, _) => ChangeValue(_slider.Value, fromSlider: true);
            _number.ValueChanged += (_, _) => ChangeValue((int)_number.Value, fromSlider: false);

            Controls.Add(_slider);
            Controls.Add(_number);
            Controls.Add(percent);
        }

        internal void SetScale(double scale)
        {
            var percent = Math.Clamp((int)Math.Round(scale * 100), 50, 200);
            _updating = true;
            _slider.Value = percent;
            _number.Value = percent;
            _updating = false;
        }

        private void ChangeValue(int percent, bool fromSlider)
        {
            if (_updating)
            {
                return;
            }

            _updating = true;
            if (fromSlider)
            {
                _number.Value = percent;
            }
            else
            {
                _slider.Value = percent;
            }
            _updating = false;
            _changed(percent / 100.0);
        }
    }
}
