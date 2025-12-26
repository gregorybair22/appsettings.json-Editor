# Assumptions & Questions

## Assumptions Made

### 1. Project Structure
- **Assumption**: The project is a .NET 6.0 Windows Forms library that can be embedded in an existing WinForms application.
- **Rationale**: Requirements specify WinForms UserControl (preferred) for embedding.

### 2. JSON Structure
- **Assumption**: The `appsettings.json` file follows a nested structure with sections: `ConnectionStrings`, `AppSettings`, `DispenseConfiguration`, `AntennaConfiguration`, `EmailConfiguration`.
- **Rationale**: Standard .NET configuration pattern.

### 3. Master Files
- **Assumption**: Master files are stored in a `masters` subdirectory relative to the application directory.
- **Assumption**: Master file naming convention: `master.{type}.json` (e.g., `master.default.json`, `master.machinetype.json`).
- **Assumption**: When merging masters, arrays are replaced entirely (not merged element-by-element).
- **Rationale**: Requirements specify this structure and merge order.

### 4. Template Files
- **Assumption**: Template files are stored in a `templates` subdirectory relative to the application directory.
- **Assumption**: Template file naming convention: `template.{name}.json`.
- **Rationale**: Requirements specify this structure.

### 5. Backup Files
- **Assumption**: Backup files are stored in the same directory as `appsettings.json`.
- **Assumption**: Backup file naming: `appsettings_yyyyMMdd_HHmmss.bak`.
- **Rationale**: Requirements specify this format.

### 6. Validation
- **Assumption**: String enums (like "castellano", "ingles") are validated against allowed values.
- **Assumption**: Integer ranges (like "0..3") are validated when specified in descriptions.
- **Assumption**: Required fields are not enforced unless explicitly stated (most fields appear optional based on nullable types).
- **Rationale**: Requirements specify "no invalid values" and strict validation.

### 7. UI Controls
- **Assumption**: Boolean properties use CheckBox controls.
- **Assumption**: Integer properties use NumericUpDown controls.
- **Assumption**: String properties with "Allowed:" values use ComboBox (dropdown).
- **Assumption**: Other string properties use TextBox.
- **Rationale**: Requirements specify correct UI controls per type.

### 8. Change Tracking
- **Assumption**: Status icons: ○ (gray) = unchanged, ✓ (green) = changed and valid, ✗ (red) = changed but invalid.
- **Rationale**: Requirements specify visual status indicators.

### 9. Dependencies
- **Assumption**: Field dependencies (e.g., disabling related fields when a feature is disabled) are not explicitly defined in the catalog, so not implemented in initial version.
- **Question**: Should we implement specific dependency rules? If so, which ones?

### 10. Missing Parameters
- **Assumption**: The PDF mentions "Additional DispenseConfiguration.* parameters exist in the master document" but not all are listed. The implementation uses a catalog-driven approach that can be extended.
- **Question**: Are there additional parameters not listed in the provided catalog that should be included?

### 11. Default Values
- **Assumption**: Default values are applied from the model's property initializers when loading a new/empty configuration.
- **Assumption**: Default values from masters override model defaults when applied.
- **Rationale**: Requirements specify defaults should come from masters.

### 12. Atomic Write Strategy
- **Assumption**: The atomic write uses a temporary file (`appsettings.json.tmp`) that is then moved to replace the original.
- **Rationale**: Requirements specify this strategy for safe saving.

## Questions

### 1. Parameter Dependencies
- **Question**: Are there specific field dependencies that should be enforced? For example:
  - When `AppSettings.ShowPairButtonsClothes` is `False`, should `AppSettings.SetCustom` be disabled?
  - When `AppSettings.AppTestMode` is `True`, should certain hardware-related fields be disabled?

### 2. Required Fields
- **Question**: Which fields are actually required vs. optional? The catalog doesn't explicitly mark required fields.
- **Question**: Should validation prevent saving if required fields are empty?

### 3. Integer Ranges
- **Question**: Are there minimum/maximum values for integer fields that aren't explicitly stated in the descriptions?
- **Question**: Should we add range validation for fields like `DispensingMachineId`, `MaxSecondsInactivity`, etc.?

### 4. String Validation
- **Question**: Are there regex patterns or other validation rules for string fields (like connection strings, paths, email addresses) that should be enforced?

### 5. Master Merge Strategy
- **Question**: The requirements mention "Arrays policy must be explicit (recommended: replace entire array)" - are there array properties in the configuration that need special handling?

### 6. Template Scope
- **Question**: Should templates allow saving only selected sections/subset of configuration, or always the entire configuration?
- **Question**: Should there be a UI to select which sections to include in a template?

### 7. Error Recovery
- **Question**: If `appsettings.json` is corrupted or invalid JSON, should the editor attempt to parse partial data, or show a complete error state?

### 8. Integration
- **Question**: How should the UserControl be integrated into the main application? Should it be added to a form, or embedded in a tab/page?
- **Question**: Are there specific size constraints or layout requirements for the embedded control?

### 9. Localization
- **Question**: Should the UI support multiple languages, or is English sufficient?
- **Question**: Should parameter descriptions be translatable?

### 10. Logging
- **Question**: Should the editor log its operations (saves, loads, errors) to a log file?

## Implementation Notes

### Extensibility
The implementation is designed to be extensible:
- New parameters can be added to the model classes without UI code changes (the UI is generated from reflection).
- New sections can be added by creating new model classes and adding them to `AppSettingsRootModel`.
- Validation rules can be extended in `ValidationService`.

### Testing Recommendations
- Test with missing `appsettings.json` file
- Test with invalid JSON
- Test with missing required fields
- Test master merge order and precedence
- Test template save/load
- Test backup restore
- Test validation for all field types
- Test change tracking accuracy

### Future Enhancements
- Add dependency enforcement between fields
- Add regex validation for string fields
- Add range validation for integer fields
- Add template section selection
- Add diff/preview when applying masters or templates
- Add search/filter functionality for large parameter lists
- Add export/import functionality

