# Looga Inspector

Looga Inspector is a small attribute-driven inspector framework for Unity. Add attributes from `LoogaSoft.Inspector.Runtime` to serialized fields or methods and the default Looga editor handles layout, visibility, validation, dropdowns, inline assets, buttons, and common Unity-specific selectors.

The package is designed to keep most inspector polish close to the data it affects, without creating one-off custom editor scripts for every component.

## Setup

Add the runtime namespace to scripts that use the attributes:

```csharp
using LoogaSoft.Inspector.Runtime;
```

For Unity-specific types in examples, also use:

```csharp
using UnityEngine;
```

## Layout

### Tabs

`TabAttribute` groups fields into tab bars. Use `level` for nested tab groups.

```csharp
[Tab("General")]
[SerializeField] private string _displayName;

[Tab("Effects")]
[Tab("SFX", level: 1)]
[SerializeField] private AudioClip _fireSound;

[Tab("VFX", level: 1)]
[SerializeField] private GameObject _muzzleFlashPrefab;
```

`TabEndAttribute` ends a tabbed section when later fields should return to normal drawing.

```csharp
[Tab("Advanced")]
[SerializeField] private float _debugValue;

[TabEnd]
[SerializeField] private bool _drawOutsideTabs;
```

### Foldouts

`LoogaFoldoutAttribute` wraps one field or nested serializable class in a styled foldout.

```csharp
[LoogaFoldout("Recoil", LoogaFoldoutStyle.Large, defaultExpanded: true)]
[SerializeField] private RecoilSettings _recoil;
```

`LoogaFoldoutGroupAttribute` starts a foldout around multiple fields. End it with `LoogaFoldoutGroupEndAttribute`.

```csharp
[LoogaFoldoutGroup("Audio", LoogaFoldoutStyle.Small)]
[SerializeField] private AudioClip _openSound;

[SerializeField] private AudioClip _closeSound;

[LoogaFoldoutGroupEnd]
[SerializeField] private float _volume = 1f;
```

`LoogaToggleFoldoutAttribute` draws a foldout with a toggle in its header. If the toggle is off, the foldout stays collapsed and hides the arrow. Attributes cannot receive live field references in C#, so nested-class toggles use a serialized child field name.

```csharp
[Serializable]
private sealed class DamagePopupSettings
{
    [SerializeField] private bool _enabled;
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private float _lifetime = 0.8f;
}

[LoogaToggleFoldout("Damage Popups", "_enabled")]
[SerializeField] private DamagePopupSettings _damagePopups;
```

For several sibling fields, use `LoogaToggleFoldoutGroupAttribute` on the bool field. That first bool becomes the header toggle and is not drawn again inside the foldout.

```csharp
[LoogaToggleFoldoutGroup("Advanced Recoil")]
[SerializeField] private bool _advancedRecoil;

[SerializeField] private float _springFrequency = 18f;

[LoogaToggleFoldoutGroupEnd]
[SerializeField] private float _springDamping = 0.6f;
```

### Boxes

`LoogaBoxAttribute` draws one field or nested serializable class inside a non-collapsible styled box.

```csharp
[LoogaBox("Projectile", LoogaFoldoutStyle.Large)]
[SerializeField] private ProjectileSettings _projectile;
```

`LoogaBoxGroupAttribute` and `LoogaBoxGroupEndAttribute` draw multiple fields inside a non-collapsible box.

```csharp
[LoogaBoxGroup("Identity", LoogaFoldoutStyle.Small)]
[SerializeField] private string _id;

[SerializeField] private Sprite _icon;

[LoogaBoxGroupEnd]
[SerializeField] private string _description;
```

### Headers And Separators

`CenterHeaderAttribute` draws a centered section header.

```csharp
[CenterHeader("Runtime")]
[SerializeField] private bool _enabled;
```

`DividerLineAttribute` draws a horizontal separator.

```csharp
[DividerLine]
[SerializeField] private float _nextSectionValue;
```

`TooltipBoxAttribute` draws an info, warning, or error box above a field.

```csharp
[TooltipBox("Used only while testing.", TooltipType.Warning)]
[SerializeField] private bool _debugMode;
```

`LoogaInspectorMessageAttribute` draws an info, warning, or error box at the top of a component inspector when a bool field, property, or method returns true. Use it for component-level setup warnings instead of writing a custom editor.

```csharp
[LoogaInspectorMessage(nameof(MissingSource), "No source component found.", MessageMode.Warning)]
public sealed class ExampleComponent : MonoBehaviour
{
    private bool MissingSource => GetComponent<SourceComponent>() == null;
}
```

## Visibility And Editability

### Conditional Visibility

`ShowIfAttribute` and `HideIfAttribute` show or hide fields based on a bool member, or by comparing a member to an expected value.

```csharp
[SerializeField] private bool _usesAmmo;

[ShowIf(nameof(_usesAmmo))]
[SerializeField] private int _magazineSize;
```

```csharp
[SerializeField] private FireMode _fireMode;

[ShowIf(nameof(_fireMode), "Automatic")]
[SerializeField] private float _fireRate;
```

Supported comparison values are `bool`, `int`, `float`, and `string`. For enums, pass the enum value name as a string.

### Conditional Enable/Disable

`EnableIfAttribute` and `DisableIfAttribute` enable or disable fields based on a bool member.

```csharp
[SerializeField] private bool _canEdit;

[EnableIf(nameof(_canEdit))]
[SerializeField] private string _editableValue;
```

### Play/Edit Mode Helpers

Use these to make setup-only or runtime-only data obvious:

```csharp
[DisableInPlayMode]
[SerializeField] private string _setupId;

[DisableInEditMode]
[SerializeField] private float _runtimeTuning;

[ShowInPlayMode]
[SerializeField] private int _runtimeCount;

[ShowInEditMode]
[SerializeField] private bool _editorOnlyPreview;
```

### Read Only

`ReadOnlyAttribute` draws a serialized field normally, but prevents editing.

```csharp
[ReadOnly]
[SerializeField] private int _currentHealth;
```

## Validation And Change Hooks

`ValidateInputAttribute` calls a bool method and shows a message when validation fails.

```csharp
[ValidateInput(nameof(HasValidId), "ID cannot be empty.", MessageMode.Warning)]
[SerializeField] private string _id;

private bool HasValidId() => !string.IsNullOrWhiteSpace(_id);
```

`OnFieldChangedAttribute` calls a method after a field changes in the inspector.

```csharp
[OnFieldChanged(nameof(RebuildCache))]
[SerializeField] private float _radius;

private void RebuildCache() { }
```

## Labels And Text

`CustomLabelAttribute` replaces the displayed label for a field.

```csharp
[CustomLabel("Display Name")]
[SerializeField] private string _displayName;
```

`LabelAttribute` provides label metadata used by Looga Inspector drawing paths.

```csharp
[Label("Movement Speed")]
[SerializeField] private float _speed;
```

`FittedTextAttribute` draws strings as a larger text area with a minimum line count.

```csharp
[FittedText(4)]
[SerializeField] private string _description;
```

`AssetDisplayNameAttribute` keeps a string synced to the asset name unless a paired custom-name bool is enabled.

```csharp
[SerializeField] private bool _useCustomDisplayName;

[AssetDisplayName(nameof(_useCustomDisplayName))]
[SerializeField] private string _displayName;
```

## Dropdowns And Selectors

### Custom Dropdowns

`DropdownAttribute` draws a field as a dropdown backed by a field, property, or zero-argument method.

```csharp
[Dropdown(nameof(GetWeaponIds))]
[SerializeField] private string _weaponId;

private string[] GetWeaponIds() => new[] { "Pistol", "Shotgun", "Rifle" };
```

Use `DropdownOption` when the label and stored value differ:

```csharp
[Dropdown(nameof(GetOptions))]
[SerializeField] private int _selectedIndex;

private DropdownOption[] GetOptions()
{
    return new[]
    {
        new DropdownOption("Easy", 0),
        new DropdownOption("Hard", 1)
    };
}
```

Or use `labelMember` and `valueMember` for object lists:

```csharp
[Dropdown(nameof(_entries), labelMember: "Name", valueMember: "Id")]
[SerializeField] private string _selectedId;
```

### Unity Selectors

These draw common Unity dropdowns:

```csharp
[Tag]
[SerializeField] private string _targetTag;

[Layer]
[SerializeField] private int _collisionLayer;

[SortingLayer]
[SerializeField] private string _sortingLayer;

[Scene]
[SerializeField] private string _sceneName;
```

`SingleEnumFlagAttribute` draws a flagged enum as a single-choice value instead of a multi-flag mask.

```csharp
[SingleEnumFlag]
[SerializeField] private DamageType _damageType;
```

`SecureStringAttribute` disables editing until the user explicitly enters edit mode. Pass `true` to obscure the value while locked.

```csharp
[SecureString]
[SerializeField] private string _titleId;

[SecureString(true)]
[SerializeField] private string _secret;
```

`BoolButtonAttribute` draws a bool with a button that calls a method.

```csharp
[BoolButton(nameof(TogglePreview), "Preview")]
[SerializeField] private bool _previewEnabled;

private void TogglePreview() { }
```

## Numeric And List Helpers

`MinMaxSliderAttribute` draws a `Vector2` as a min/max slider.

```csharp
[MinMaxSlider(0f, 100f)]
[SerializeField] private Vector2 _range;
```

`SliderlessRangeAttribute` clamps a numeric value without drawing Unity's slider UI.

```csharp
[SliderlessRange(0f, 1f)]
[SerializeField] private float _weight;
```

`NoDuplicateEntriesAttribute` warns when an array or list contains duplicates. Pass a child member name for object or struct lists.

```csharp
[NoDuplicateEntries]
[SerializeField] private List<string> _ids;

[NoDuplicateEntries("_key")]
[SerializeField] private List<RuleEntry> _rules;
```

`TableListAttribute` draws simple arrays/lists as compact rows. Pass the child field names to use as columns. The first constructor allows add/remove buttons; the second can disable them.

```csharp
[TableList("_clip", "_direction", "_playbackSpeed")]
[SerializeField] private List<JumpClip> _jumpClips;

[TableList(false, "_key", "_value")]
[SerializeField] private List<RuntimeValue> _runtimeValues;
```

Use table lists for compact data rows. Complex preview tools, filtering, or deeply nested data should still use a purpose-built editor.

## Scriptable Object Fields

`ExposeScriptableAttribute` draws an assigned ScriptableObject inline. When the field is empty, the drawer shows a create button beside the object field. Creating an asset opens Unity's save panel, assigns the new asset, and pings it in the Project window while keeping the current inspector selection.

```csharp
[ExposeScriptable(showScriptField: false, expandedByDefault: true, createButtonLabel: "New")]
[SerializeField] private WeaponVfxProfile _vfxProfile;
```

If the declared field type has multiple concrete ScriptableObject types, the create button opens a type menu first.

## Animator Helpers

Animator attributes draw string fields as dropdowns from an Animator Controller or Animator reference member.

```csharp
[SerializeField] private RuntimeAnimatorController _controller;

[AnimatorParameter(nameof(_controller))]
[SerializeField] private string _anyParameter;

[AnimatorParameter(nameof(_controller), AnimatorControllerParameterType.Bool)]
[SerializeField] private string _boolParameter;

[AnimatorLayer(nameof(_controller))]
[SerializeField] private string _layerName;

[AnimatorState(nameof(_controller))]
[SerializeField] private string _stateName;

[AnimatorClip(nameof(_controller))]
[SerializeField] private AnimationClip _clip;
```

The referenced member can be a serialized field, property, or method that resolves to an Animator, Animator Controller, or compatible source supported by the helper.

## Shader And Material Helpers

`ShaderPropertyAttribute` draws a string as a dropdown of shader property names from a referenced `Material` or `Shader`. Filter by `LoogaShaderPropertyType` when needed.

```csharp
[SerializeField] private Material _material;

[ShaderProperty(nameof(_material), LoogaShaderPropertyType.Color)]
[SerializeField] private string _tintProperty;

[ShaderProperty(nameof(_material), LoogaShaderPropertyType.Texture)]
[SerializeField] private string _maskProperty;
```

`ShaderKeywordAttribute` draws a string as a dropdown of local shader keywords from a referenced `Material` or `Shader`.

```csharp
[ShaderKeyword(nameof(_material))]
[SerializeField] private string _emissionKeyword;
```

`GlobalShaderPropertyAttribute` draws a string as a dropdown of project-known shader property names. This is meant for values used with `Shader.SetGlobal*`, global material property IDs, or shared rendering systems.

```csharp
[GlobalShaderProperty(LoogaShaderPropertyType.Texture)]
[SerializeField] private string _globalMaskProperty;
```

The global list is built by scanning shader assets in the project.

## Method Buttons

`ButtonAttribute` draws a method as an inspector button. It supports labels, top placement, edit/play mode gating, confirmation prompts, custom height, and an optional bool condition.

```csharp
[Button("Clear Data", confirmMessage: "Clear all saved data?")]
private void ClearData() { }

[Button("Spawn", mode: LoogaButtonMode.PlayModeOnly, enableIf: nameof(CanSpawn))]
private void Spawn() { }

[Button("Refresh", drawAtTop: true, height: 24f)]
private void Refresh() { }

private bool CanSpawn() => Application.isPlaying;
```

## When To Use A Custom Editor

Prefer Looga Inspector attributes for common inspector shaping: grouping, tabs, conditional fields, validation, inline ScriptableObjects, dropdowns, and simple tables.

Keep a custom editor or editor window when the workflow needs custom previews, graph editing, search/filter-heavy interfaces, drag-and-drop canvases, asset migration tools, or runtime debugging panels.

