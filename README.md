# AppSettings Editor - WinForms UserControl

A desktop configuration editor for `appsettings.json` files that can be embedded in WinForms applications.

## Features

- ✅ **Embedded WinForms UserControl** - Easy integration into existing applications
- ✅ **Strict Validation** - Prevents saving invalid values with type-specific controls
- ✅ **Parameter Help** - Displays purpose, usage, allowed values, and defaults for each parameter
- ✅ **Change Tracking** - Visual status indicators (○ unchanged, ✓ changed/valid, ✗ changed/invalid)
- ✅ **Safe Save** - Automatic backups before saving with atomic write strategy
- ✅ **Masters Support** - Load and apply default values from master files
- ✅ **Templates** - Save and load reusable configuration profiles
- ✅ **Backup Management** - Restore from automatic backups

## Project Structure

```
AppSettingsEditor/
├── Models/                          # Configuration models
│   ├── AppSettingsRootModel.cs      # Root configuration model
│   ├── ConnectionStringsModel.cs    # Connection strings section
│   ├── AppSettingsModel.cs          # App settings section
│   ├── DispenseConfigurationModel.cs # Dispense configuration section
│   ├── AntennaConfigurationModel.cs # Antenna configuration section
│   └── EmailConfigurationModel.cs   # Email configuration section
├── Services/                        # Business logic services
│   ├── JsonService.cs               # JSON load/save with backups
│   ├── MasterService.cs             # Master file management
│   ├── TemplateService.cs           # Template management
│   └── ValidationService.cs         # Field validation
├── Controls/                        # UI components
│   └── AppSettingsEditorControl.cs  # Main UserControl
├── AppSettingsEditor.csproj         # Project file
├── README.md                        # This file
└── ASSUMPTIONS_AND_QUESTIONS.md     # Assumptions and questions
```

## Usage

### Integration

1. Add the project to your solution or reference the compiled DLL.
2. Add the UserControl to your form:

```csharp
using AppSettingsEditor.Controls;

// In your form
var editor = new AppSettingsEditorControl();
editor.Dock = DockStyle.Fill;
this.Controls.Add(editor);
```

### Configuration File

The editor looks for `appsettings.json` in the current working directory by default. You can specify a custom path by modifying the `JsonService` constructor.

### Masters

Master files should be placed in a `masters` subdirectory:

```
masters/
├── master.default.json          # Base defaults
├── master.machinetype.json      # Machine type specific
├── master.customer.json         # Customer specific
└── master.machineid.json        # Machine ID specific
```

Masters are merged in order: default → type → customer → machine (later values override earlier ones).

### Templates

Templates are saved in a `templates` subdirectory:

```
templates/
├── template.production.json
├── template.testing.json
└── template.custom.json
```

## Requirements

- .NET 6.0 or later
- Windows Forms
- Newtonsoft.Json (included via NuGet)

## Building

```bash
dotnet build
```

### Testing

To test the UserControl standalone, change `OutputType` in `AppSettingsEditor.csproj` from `Library` to `WinExe`, then run:

```bash
dotnet run
```

This will launch a test form with the editor. For production use, keep it as a `Library` and embed it in your application.

## Features in Detail

### Validation

- **Boolean fields**: CheckBox controls (True/False only)
- **Integer fields**: NumericUpDown with appropriate ranges
- **String enums**: ComboBox with allowed values (e.g., "castellano", "ingles")
- **Other strings**: TextBox with optional regex validation

### Change Tracking

Each field shows a status icon:
- **○** (gray): Unchanged
- **✓** (green): Changed and valid
- **✗** (red): Changed but invalid

The status bar shows: "Pending changes: N | Errors: N"

### Safe Save

When saving:
1. Creates a backup: `appsettings_yyyyMMdd_HHmmss.bak`
2. Writes to temporary file: `appsettings.json.tmp`
3. Atomically replaces original file

### Masters

Masters provide default values that can be applied to the current configuration. The merge strategy replaces entire arrays and merges object properties.

### Templates

Templates allow saving the current configuration as a reusable profile. Templates can be loaded to quickly apply a known-good configuration.

## Parameter Catalog

The editor supports all parameters from the catalog:

- **ConnectionStrings**: Database connection strings
- **AppSettings**: Application settings (language, hardware flags, paths, etc.)
- **DispenseConfiguration**: Dispensing machine configuration (positions, speeds, sensors, etc.)
- **AntennaConfiguration**: RFID antenna settings
- **EmailConfiguration**: Email notification settings

Each parameter includes:
- Purpose description
- Usage notes
- Allowed values/constraints
- Default value (when defined)

## Notes

- The editor requires write access to the directory containing `appsettings.json`
- Backups are stored in the same directory as the configuration file
- Masters and templates are stored in subdirectories relative to the application directory
- Invalid configurations cannot be saved (validation must pass)

## See Also

- `ASSUMPTIONS_AND_QUESTIONS.md` for implementation assumptions and open questions

