using AppSettingsEditor.Models;
using AppSettingsEditor.Services;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace AppSettingsEditor.Controls;

public partial class AppSettingsEditorControl : UserControl
{
    private JsonService _jsonService = null!;
    private MasterService _masterService = null!;
    private TemplateService _templateService = null!;
    private ValidationService _validationService = null!;
    
    private AppSettingsRootModel _originalModel = null!;
    private AppSettingsRootModel _currentModel = null!;
    private Dictionary<string, Control> _fieldControls = null!;
    private Dictionary<string, Label> _statusLabels = null!;
    private Dictionary<string, bool> _fieldChanged = null!;
    private Dictionary<string, ValidationResult> _fieldValidation = null!;

    private TabControl _tabControl = null!;
    private ToolStrip _toolStrip = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public AppSettingsEditorControl()
    {
        InitializeComponent();
        InitializeServices();
        InitializeUI();
        LoadConfiguration();
    }

    private void InitializeServices()
    {
        _jsonService = new JsonService();
        _masterService = new MasterService();
        _templateService = new TemplateService();
        _validationService = new ValidationService();
        _fieldControls = new Dictionary<string, Control>();
        _statusLabels = new Dictionary<string, Label>();
        _fieldChanged = new Dictionary<string, bool>();
        _fieldValidation = new Dictionary<string, ValidationResult>();
    }

    private void InitializeUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        // Define blue theme colors
        var lightBlue = Color.FromArgb(173, 216, 230); // Light blue matching main app
        var mediumBlue = Color.FromArgb(100, 149, 237); // Medium blue for accents
        var darkBlue = Color.FromArgb(70, 130, 180); // Darker blue for text

        // ToolStrip
        _toolStrip = new ToolStrip
        {
            BackColor = lightBlue,
            ForeColor = Color.White
        };
        _toolStrip.Items.Add(new ToolStripButton("Save", null, (s, e) => SaveConfiguration()) { Enabled = false, ForeColor = Color.White });
        _toolStrip.Items.Add(new ToolStripButton("Discard", null, (s, e) => DiscardChanges()) { Enabled = false, ForeColor = Color.White });
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripButton("Reload", null, (s, e) => LoadConfiguration()) { ForeColor = Color.White });
        _toolStrip.Items.Add(new ToolStripButton("Restore Backup...", null, (s, e) => RestoreBackup()) { ForeColor = Color.White });
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripButton("Apply Master...", null, (s, e) => ApplyMaster()) { ForeColor = Color.White });
        _toolStrip.Items.Add(new ToolStripButton("Save Template...", null, (s, e) => SaveTemplate()) { ForeColor = Color.White });
        _toolStrip.Items.Add(new ToolStripButton("Load Template...", null, (s, e) => LoadTemplate()) { ForeColor = Color.White });

        // TabControl with blue theme
        _tabControl = new TabControl 
        { 
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ForeColor = darkBlue
        };
        _tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabControl.DrawItem += (s, e) =>
        {
            var tab = _tabControl.TabPages[e.Index];
            var rect = _tabControl.GetTabRect(e.Index);
            var font = e.Font ?? SystemFonts.DefaultFont;
            
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(lightBlue), rect);
                e.Graphics.DrawString(tab.Text, font, new SolidBrush(Color.White), rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.White), rect);
                e.Graphics.DrawString(tab.Text, font, new SolidBrush(darkBlue), rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        };

        // StatusStrip
        _statusStrip = new StatusStrip
        {
            BackColor = lightBlue,
            ForeColor = Color.White
        };
        _statusLabel = new ToolStripStatusLabel("Ready") { ForeColor = Color.White };
        _statusStrip.Items.Add(_statusLabel);

        // Layout
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        mainPanel.Controls.Add(_toolStrip, 0, 0);
        mainPanel.Controls.Add(_tabControl, 0, 1);
        mainPanel.Controls.Add(_statusStrip, 0, 2);

        this.Controls.Add(mainPanel);
    }

    private void LoadConfiguration()
    {
        var loadedModel = _jsonService.Load();
        
        if (loadedModel == null)
        {
            // File doesn't exist or is invalid
            bool fileExists = _jsonService.FileExists();
            
            if (fileExists)
            {
                // File exists but is corrupted/invalid
                MessageBox.Show("appsettings.json is invalid or corrupted. You can restore from backup or apply a master configuration.", 
                    "Configuration Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _currentModel = new AppSettingsRootModel();
            }
            else
            {
                // File doesn't exist - try to load from master.default.json
                var masterModel = _masterService.LoadMaster("master.default.json");
                if (masterModel != null)
                {
                    // Successfully loaded from master, save it as appsettings.json
                    _currentModel = masterModel;
                    try
                    {
                        _jsonService.Save(_currentModel);
                    }
                    catch
                    {
                        // If save fails, continue with the loaded model
                    }
                }
                else
                {
                    // No master file available, create empty model
                    _currentModel = new AppSettingsRootModel();
                }
            }
        }
        else
        {
            _currentModel = loadedModel;
        }

        _originalModel = CloneModel(_currentModel);
        CreateTabs();
        UpdateStatus();
    }

    private void CreateTabs()
    {
        _tabControl.TabPages.Clear();
        _fieldControls.Clear();
        _statusLabels.Clear();
        _fieldChanged.Clear();
        _fieldValidation.Clear();

        // ConnectionStrings Tab
        CreateTab("ConnectionStrings", _currentModel.ConnectionStrings ?? new ConnectionStringsModel());

        // AppSettings Tab
        CreateTab("AppSettings", _currentModel.AppSettings ?? new AppSettingsModel());

        // DispenseConfiguration Tab
        CreateTab("DispenseConfiguration", _currentModel.DispenseConfiguration ?? new DispenseConfigurationModel());

        // AntennaConfiguration Tab
        CreateTab("AntennaConfiguration", _currentModel.AntennaConfiguration ?? new AntennaConfigurationModel());

        // EmailConfiguration Tab
        CreateTab("EmailConfiguration", _currentModel.EmailConfiguration ?? new EmailConfigurationModel());
    }

    private void CreateTab(string sectionName, object sectionModel)
    {
        var tabPage = new TabPage(sectionName)
        {
            BackColor = Color.White
        };
        var scrollPanel = new Panel 
        { 
            Dock = DockStyle.Fill, 
            AutoScroll = true,
            BackColor = Color.White
        };
        var tableLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            Padding = new Padding(10),
            BackColor = Color.White,
            Location = new Point(0, 0)
        };

        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Property name
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Control
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30)); // Status icon
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); // Help text

        var properties = sectionModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        int row = 0;

        foreach (var prop in properties)
        {
            var propName = $"{sectionName}.{prop.Name}";
            var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
            var defaultValueAttr = prop.GetCustomAttribute<DefaultValueAttribute>();
            var description = descriptionAttr?.Description ?? "";
            var defaultValue = defaultValueAttr?.Value;

            // Property name label
            var nameLabel = new Label
            {
                Text = prop.Name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(70, 130, 180), // Dark blue
                Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold)
            };
            tableLayout.Controls.Add(nameLabel, 0, row);

            // Control based on type
            Control control = CreateControlForProperty(prop, sectionModel, propName);
            tableLayout.Controls.Add(control, 1, row);

            // Status icon
            var statusLabel = new Label
            {
                Text = "○",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(173, 216, 230) // Light blue
            };
            _statusLabels[propName] = statusLabel;
            tableLayout.Controls.Add(statusLabel, 2, row);

            // Help text
            var helpLabel = new Label
            {
                Text = description,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(100, 100, 100), // Dark gray for readability
                Font = new Font(Font.FontFamily, Font.Size * 0.95f),
                AutoSize = false
            };
            tableLayout.Controls.Add(helpLabel, 3, row);

            _fieldControls[propName] = control;
            _fieldChanged[propName] = false;
            _fieldValidation[propName] = new ValidationResult { IsValid = true };

            row++;
        }

        // Set table layout width to match scroll panel (will be adjusted on resize)
        tableLayout.Width = scrollPanel.ClientSize.Width;
        tableLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        
        // Handle scroll panel resize to adjust table layout width
        scrollPanel.Resize += (s, e) =>
        {
            if (tableLayout != null)
            {
                tableLayout.Width = Math.Max(scrollPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth, 0);
            }
        };
        
        scrollPanel.Controls.Add(tableLayout);
        tabPage.Controls.Add(scrollPanel);
        _tabControl.TabPages.Add(tabPage);
    }

    private Control CreateControlForProperty(PropertyInfo prop, object model, string propName)
    {
        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        var currentValue = prop.GetValue(model);
        Control control;

        if (propType == typeof(bool))
        {
            var checkbox = new CheckBox
            {
                Checked = currentValue as bool? ?? false,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(70, 130, 180) // Dark blue
            };
            checkbox.CheckedChanged += (s, e) => OnFieldChanged(propName, checkbox.Checked);
            control = checkbox;
        }
        else if (propType == typeof(int))
        {
            var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
            var description = descriptionAttr?.Description ?? "";
            
            // First, determine the min/max values
            decimal minValue = int.MinValue;
            decimal maxValue = int.MaxValue;
            
            // Check for range constraints (e.g., "Allowed: 0..3")
            if (description.Contains("Allowed:") && description.Contains(".."))
            {
                var rangeMatch = System.Text.RegularExpressions.Regex.Match(description, @"(\d+)\.\.(\d+)");
                if (rangeMatch.Success)
                {
                    minValue = int.Parse(rangeMatch.Groups[1].Value);
                    maxValue = int.Parse(rangeMatch.Groups[2].Value);
                }
            }
            
            // Set Minimum and Maximum BEFORE Value to avoid validation errors
            var numericUpDown = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = minValue,
                Maximum = maxValue,
                ForeColor = Color.FromArgb(70, 130, 180) // Dark blue
            };
            
            // Now set the Value after Minimum and Maximum are set
            var intValue = currentValue as int? ?? 0;
            // Clamp the value to the valid range
            if (intValue < minValue)
                intValue = (int)minValue;
            else if (intValue > maxValue)
                intValue = (int)maxValue;
            
            numericUpDown.Value = intValue;
            
            numericUpDown.ValueChanged += (s, e) => OnFieldChanged(propName, (int)numericUpDown.Value);
            control = numericUpDown;
        }
        else if (propType == typeof(string))
        {
            var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
            var description = descriptionAttr?.Description ?? "";

            // Check if it's an enum-like string
            if (description.Contains("Allowed:"))
            {
                var allowedMatch = System.Text.RegularExpressions.Regex.Match(description, @"Allowed:\s*([^\.]+)");
                if (allowedMatch.Success)
                {
                    var comboBox = new ComboBox
                    {
                        Dock = DockStyle.Fill,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        ForeColor = Color.FromArgb(70, 130, 180) // Dark blue
                    };
                    var allowedValues = allowedMatch.Groups[1].Value
                        .Split(',')
                        .Select(v => v.Trim())
                        .ToList();
                    comboBox.Items.AddRange(allowedValues.ToArray());
                    comboBox.SelectedItem = currentValue as string;
                    comboBox.SelectedIndexChanged += (s, e) => OnFieldChanged(propName, comboBox.SelectedItem?.ToString());
                    control = comboBox;
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Text = currentValue as string ?? "",
                        Dock = DockStyle.Fill,
                        ForeColor = Color.FromArgb(70, 130, 180) // Dark blue
                    };
                    textBox.TextChanged += (s, e) => OnFieldChanged(propName, textBox.Text);
                    control = textBox;
                }
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = currentValue as string ?? "",
                    Dock = DockStyle.Fill,
                    ForeColor = Color.FromArgb(70, 130, 180) // Dark blue
                };
                textBox.TextChanged += (s, e) => OnFieldChanged(propName, textBox.Text);
                control = textBox;
            }
        }
        else
        {
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? "",
                Dock = DockStyle.Fill,
                ReadOnly = true
            };
            control = textBox;
        }

        return control;
    }

    private void OnFieldChanged(string propName, object? newValue)
    {
        var sectionName = propName.Split('.')[0];
        var fieldName = propName.Split('.')[1];
        
        object? sectionModel = sectionName switch
        {
            "ConnectionStrings" => _currentModel.ConnectionStrings,
            "AppSettings" => _currentModel.AppSettings,
            "DispenseConfiguration" => _currentModel.DispenseConfiguration,
            "AntennaConfiguration" => _currentModel.AntennaConfiguration,
            "EmailConfiguration" => _currentModel.EmailConfiguration,
            _ => null
        };

        if (sectionModel == null) return;

        var prop = sectionModel.GetType().GetProperty(fieldName);
        if (prop == null) return;

        // Convert value to proper type
        object? convertedValue = null;
        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        
        if (propType == typeof(bool) && newValue is bool boolVal)
            convertedValue = boolVal;
        else if (propType == typeof(int) && newValue is int intVal)
            convertedValue = intVal;
        else if (propType == typeof(string))
            convertedValue = newValue?.ToString();

        // Validate
        var validation = _validationService.ValidateProperty(convertedValue, prop, propType);
        _fieldValidation[propName] = validation;

        // Set value
        prop.SetValue(sectionModel, convertedValue);

        // Check if changed
        var originalSection = GetOriginalSection(sectionName);
        var originalProp = originalSection?.GetType().GetProperty(fieldName);
        var originalValue = originalProp?.GetValue(originalSection);
        
        _fieldChanged[propName] = !Equals(originalValue, convertedValue);

        UpdateFieldStatus(propName);
        UpdateStatus();
    }

    private object? GetOriginalSection(string sectionName)
    {
        return sectionName switch
        {
            "ConnectionStrings" => _originalModel.ConnectionStrings,
            "AppSettings" => _originalModel.AppSettings,
            "DispenseConfiguration" => _originalModel.DispenseConfiguration,
            "AntennaConfiguration" => _originalModel.AntennaConfiguration,
            "EmailConfiguration" => _originalModel.EmailConfiguration,
            _ => null
        };
    }

    private void UpdateFieldStatus(string propName)
    {
        if (!_statusLabels.ContainsKey(propName)) return;

        var statusLabel = _statusLabels[propName];
        var changed = _fieldChanged[propName];
        var valid = _fieldValidation[propName].IsValid;

        if (!changed)
        {
            statusLabel.Text = "○";
            statusLabel.ForeColor = Color.FromArgb(173, 216, 230); // Light blue
        }
        else if (valid)
        {
            statusLabel.Text = "✓";
            statusLabel.ForeColor = Color.FromArgb(34, 139, 34); // Forest green
        }
        else
        {
            statusLabel.Text = "✗";
            statusLabel.ForeColor = Color.FromArgb(220, 20, 60); // Crimson red
        }
    }

    private void UpdateStatus()
    {
        var changedCount = _fieldChanged.Values.Count(c => c);
        var errorCount = _fieldValidation.Values.Count(v => !v.IsValid);

        _statusLabel.Text = $"Pending changes: {changedCount} | Errors: {errorCount}";
        
        var saveButton = _toolStrip.Items[0] as ToolStripButton;
        var discardButton = _toolStrip.Items[1] as ToolStripButton;
        
        if (saveButton != null)
            saveButton.Enabled = changedCount > 0 && errorCount == 0;
        
        if (discardButton != null)
            discardButton.Enabled = changedCount > 0;
    }

    private void SaveConfiguration()
    {
        if (_fieldValidation.Values.Any(v => !v.IsValid))
        {
            MessageBox.Show("Please fix all validation errors before saving.", "Validation Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _jsonService.Save(_currentModel);
            _originalModel = CloneModel(_currentModel);
            
            foreach (var key in _fieldChanged.Keys.ToList())
                _fieldChanged[key] = false;
            
            foreach (var key in _statusLabels.Keys)
                UpdateFieldStatus(key);
            
            UpdateStatus();
            MessageBox.Show("Configuration saved successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DiscardChanges()
    {
        if (MessageBox.Show("Discard all changes?", "Confirm", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _currentModel = CloneModel(_originalModel);
            CreateTabs();
            UpdateStatus();
        }
    }

    private void RestoreBackup()
    {
        var backups = _jsonService.GetBackupFiles();
        if (backups.Count == 0)
        {
            MessageBox.Show("No backup files found.", "No Backups", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var form = new Form
        {
            Text = "Restore Backup",
            Size = new Size(400, 300),
            StartPosition = FormStartPosition.CenterParent
        };

        var listBox = new ListBox { Dock = DockStyle.Fill };
        listBox.Items.AddRange(backups.ToArray());

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var restoreButton = new Button { Text = "Restore", DialogResult = DialogResult.OK };

        buttonPanel.Controls.AddRange(new Control[] { restoreButton, cancelButton });
        form.Controls.Add(listBox);
        form.Controls.Add(buttonPanel);
        form.AcceptButton = restoreButton;
        form.CancelButton = cancelButton;

        if (form.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
        {
            if (_jsonService.RestoreBackup(listBox.SelectedItem.ToString()!))
            {
                LoadConfiguration();
                MessageBox.Show("Backup restored successfully.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to restore backup.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ApplyMaster()
    {
        var masters = _masterService.GetAvailableMasters();
        if (masters.Count == 0)
        {
            MessageBox.Show("No master files found in masters directory.", "No Masters", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var form = new Form
        {
            Text = "Apply Master",
            Size = new Size(400, 300),
            StartPosition = FormStartPosition.CenterParent
        };

        var listBox = new ListBox { Dock = DockStyle.Fill };
        listBox.Items.AddRange(masters.ToArray());

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var applyButton = new Button { Text = "Apply", DialogResult = DialogResult.OK };

        buttonPanel.Controls.AddRange(new Control[] { applyButton, cancelButton });
        form.Controls.Add(listBox);
        form.Controls.Add(buttonPanel);
        form.AcceptButton = applyButton;
        form.CancelButton = cancelButton;

        if (form.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
        {
            var master = _masterService.LoadMaster(listBox.SelectedItem.ToString()!);
            if (master != null)
            {
                if (MessageBox.Show("Apply master values? This will overwrite current values.", "Confirm", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _currentModel = master;
                    CreateTabs();
                    UpdateStatus();
                }
            }
        }
    }

    private void SaveTemplate()
    {
        var form = new Form
        {
            Text = "Save Template",
            Size = new Size(300, 120),
            StartPosition = FormStartPosition.CenterParent
        };

        var textBox = new TextBox { Dock = DockStyle.Top, Margin = new Padding(10) };
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK };

        buttonPanel.Controls.AddRange(new Control[] { saveButton, cancelButton });
        form.Controls.Add(textBox);
        form.Controls.Add(buttonPanel);
        form.AcceptButton = saveButton;
        form.CancelButton = cancelButton;

        if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            if (_templateService.SaveTemplate(textBox.Text, _currentModel))
            {
                MessageBox.Show("Template saved successfully.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save template.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void LoadTemplate()
    {
        var templates = _templateService.GetAvailableTemplates();
        if (templates.Count == 0)
        {
            MessageBox.Show("No templates found in templates directory.", "No Templates", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var form = new Form
        {
            Text = "Load Template",
            Size = new Size(400, 300),
            StartPosition = FormStartPosition.CenterParent
        };

        var listBox = new ListBox { Dock = DockStyle.Fill };
        listBox.Items.AddRange(templates.ToArray());

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var loadButton = new Button { Text = "Load", DialogResult = DialogResult.OK };

        buttonPanel.Controls.AddRange(new Control[] { loadButton, cancelButton });
        form.Controls.Add(listBox);
        form.Controls.Add(buttonPanel);
        form.AcceptButton = loadButton;
        form.CancelButton = cancelButton;

        if (form.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
        {
            var template = _templateService.LoadTemplate(listBox.SelectedItem.ToString()!);
            if (template != null)
            {
                if (MessageBox.Show("Load template values? This will overwrite current values.", "Confirm", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _currentModel = template;
                    CreateTabs();
                    UpdateStatus();
                }
            }
        }
    }

    private AppSettingsRootModel CloneModel(AppSettingsRootModel source)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(source);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettingsRootModel>(json)!;
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.Name = "AppSettingsEditorControl";
        this.Size = new Size(800, 600);
        this.ResumeLayout(false);
    }
}

