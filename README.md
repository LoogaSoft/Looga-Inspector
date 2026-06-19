# Looga Inspector

## Nested Tabs

`TabAttribute` supports hierarchical tab groups through its `level` parameter.
The default level is `0`, so existing usages continue to work:

```csharp
[Tab("General")]
public int value;
```

Use higher levels to create nested tab bars inside the active parent tab.
Stack parent and child tab attributes on the first field in a nested group:

```csharp
[Tab("Effects")]
[Tab("SFX", level: 1)]
public AudioClip equipSound;

[Tab("VFX", level: 1)]
public GameObject muzzleFlashPrefab;

[Tab("Impacts", level: 2)]
public GameObject metalImpactPrefab;
```

Tab levels are authored as a path. Changing a lower level keeps its parent tab,
while changing level `0` starts a new top-level tab.

## Exposed Scriptable Objects

`ExposeScriptableAttribute` draws assigned ScriptableObject assets inline.
When the field is empty, the drawer shows a `Create` button beside the object
field. If the declared field type has multiple concrete ScriptableObject types,
the button opens a creation menu first. Creating an asset opens Unity's save
panel so the asset name and location can be chosen. The new asset is assigned
back to the field and pinged in the Project window without changing the current
inspector selection.

Optional constructor settings:

```csharp
[ExposeScriptable(showScriptField: false, expandedByDefault: true, createButtonLabel: "New")]
public MyProfile profile;
```

## Conditional Visibility

`ShowIf` and `HideIf` can check a bool member as before, or compare against a
simple value. Enum comparisons should use the enum value name as a string:

```csharp
[ShowIf(nameof(mode), "Advanced")]
public float advancedValue;
```

## Buttons

`ButtonAttribute` supports confirmation prompts, edit/play mode gating, custom
height, and an optional bool condition:

```csharp
[Button("Clear Data", confirmMessage: "Clear all saved data?")]
private void ClearData() { }

[Button("Spawn", mode: LoogaButtonMode.PlayModeOnly, enableIf: nameof(CanSpawn))]
private void Spawn() { }
```

## Display Names

`AssetDisplayNameAttribute` keeps a string field synced to the asset name unless
a paired custom-name bool is enabled:

```csharp
[SerializeField] private bool _useCustomDisplayName;

[AssetDisplayName(nameof(_useCustomDisplayName))]
[SerializeField] private string _displayName;
```

## Duplicate Validation

`NoDuplicateEntriesAttribute` draws the normal list and adds a warning when
duplicate entries are found. Pass a child member name when list elements are
objects or structs:

```csharp
[NoDuplicateEntries("_key")]
public List<Rule> rules;
```

## Table Lists

`TableListAttribute` draws simple serialized arrays/lists as fixed one-line
tables. Pass the child field names to draw as columns:

```csharp
[TableList("_clip", "_direction", "_playbackSpeed")]
public List<JumpClip> jumpClips;
```

This is intended for compact data rows. Complex rows, nested foldouts, previews,
or search/filter tooling should still use purpose-built editor code.

## Mode-Aware Fields

Use `DisableInPlayMode`, `DisableInEditMode`, `ShowInPlayMode`, and
`ShowInEditMode` to keep runtime-only or setup-only data obvious in inspectors.

```csharp
[DisableInPlayMode]
public string setupId;
```

## Dropdowns

`DropdownAttribute` can still read options from a field, property, or method.
Options can now expose separate display/value members, or return
`DropdownOption` values:

```csharp
[Dropdown(nameof(GetOptions), labelMember: "Name", valueMember: "Id")]
public string selectedId;
```
