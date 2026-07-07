# Looga Inspector

Looga Inspector is a lightweight, attribute-driven inspector framework for Unity. Add attributes from `LoogaSoft.Inspector.Runtime` to serialized fields, methods, or component classes, and the default Looga editor handles common inspector workflows: layout, foldouts, tabs, validation, dropdowns, inline ScriptableObjects, catalog management, buttons, and Unity-specific selectors.

The goal is to keep most inspector polish beside the data it affects, without creating one-off custom editor scripts for every component.

## Setup

Use the runtime namespace in scripts that use Looga Inspector attributes:

```csharp
using LoogaSoft.Inspector.Runtime;
```

For Unity examples, also include:

```csharp
using UnityEngine;
```

## Package Organization

Looga Inspector is organized by feature area so runtime attributes and editor drawers are easy to pair:

```text
Runtime/
  Core/                  Shared runtime types.
  Attributes/
    Assets/              ScriptableObject, asset link, catalog, and asset dropdown attributes.
    Animator/            Animator controller selector attributes.
    Class/               Class-level inspector attributes.
    Decorators/          Header and divider attributes.
    Display/             Labels, read-only fields, fitted text, status boxes, and tooltip boxes.
    Groups/              Foldouts, boxes, tabs, inline rows, and table lists.
    Input/               Dropdowns, enum filters, bool buttons, ranges, and secure strings.
    Rendering/           Shader, material, and volume profile attributes.
    Unity/               Scene, tag, layer, sorting layer, physics layer, and navmesh selectors.
    Validation/          Show/hide, enable/disable, change hooks, validation, and duplicate checks.

Editor/
  Core/                  Main inspector pipeline and cached metadata.
  Styles/                Shared colors, foldouts, tabs, buttons, and layout primitives.
  Utilities/             Reflection, property, shader, query, and member-value helpers.
  Drawers/               Feature-matched drawers mirroring Runtime/Attributes.
  OptionalSupport/       Toolbar and package-support toggles.
  Optional/              Optional integration assemblies, such as ZLinq support.
```

When adding a new attribute, place the runtime attribute in the closest `Runtime/Attributes` feature folder and its drawer in the matching `Editor/Drawers` folder. Prefer adding shared drawing behavior to `Editor/Styles` or `Editor/Core` instead of duplicating GUI math inside individual drawers.

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

`LoogaFoldoutGroupAttribute` starts a foldout around several sibling fields. End it with `LoogaFoldoutGroupEndAttribute`.

```csharp
[LoogaFoldoutGroup("Audio", LoogaFoldoutStyle.Small)]
[SerializeField] private AudioClip _openSound;

[SerializeField] private AudioClip _closeSound;

[LoogaFoldoutGroupEnd]
[SerializeField] private float _volume = 1f;
```

`LoogaToggleFoldoutAttribute` draws a foldout with a toggle in its header. If the toggle is off, the foldout stays collapsed and hides the arrow. For nested classes, pass the serialized child bool name.

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

`LoogaBoxGroupAttribute` and `LoogaBoxGroupEndAttribute` draw multiple sibling fields inside a non-collapsible box.

```csharp
[LoogaBoxGroup("Identity", LoogaFoldoutStyle.Small)]
[SerializeField] private string _id;

[SerializeField] private Sprite _icon;

[LoogaBoxGroupEnd]
[SerializeField] private string _description;
```

`StructBoxAttribute` is a compact drawer for a single serializable struct/class. Use it when a nested value should look grouped without creating a wider layout group.

```csharp
[StructBox("Damage")]
[SerializeField] private DamageSettings _damage;
```

You can also place it on the serializable type itself so every field or list element of that type uses the same boxed layout without a project-specific property drawer:

```csharp
[Serializable]
[StructBox("Ingredient")]
public struct IngredientDefinition
{
    public ItemQuantityPair item;
    public int weight;
}
```

### Inline Rows

`InlineRowAttribute` draws multiple values on one line.

When applied to one struct/class field, its visible child fields are drawn inline:

```csharp
[InlineRow]
[SerializeField] private DamageRange _damage;
```

You can also place it on the serializable type itself. Child fields may use `InlineRowAttribute` only to define relative widths:

```csharp
[Serializable]
[InlineRow]
public class ItemQuantityPair
{
    [InlineRow(width: 0.65f)]
    public ItemDefinition item;

    [InlineRow(width: 0.35f)]
    public int amount = 1;
}
```

When applied to adjacent sibling fields with the same row id, those normal fields are drawn in one row. The optional width controls the relative column weight.

```csharp
[InlineRow("damage", 2f)]
[SerializeField] private int _minimumDamage;

[InlineRow("damage")]
[SerializeField] private int _maximumDamage;
```

Use inline rows for small scalar values. Prefer boxes, foldouts, or tables for complex nested data.

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

`StatusBoxAttribute` draws an info, warning, or error box above a field or at the top of the whole inspector. It can be unconditional, conditional, or use a member as the message.

```csharp
[StatusBox("Assign a profile before entering play mode.", LoogaStatusBoxType.Warning, Condition = nameof(MissingProfile))]
[SerializeField] private ScriptableObject _profile;

private bool MissingProfile => _profile == null;
```

```csharp
[StatusBox(nameof(GetStatusMessage), LoogaStatusBoxType.Info, UseMember = true)]
public sealed class ExampleComponent : MonoBehaviour
{
    private string GetStatusMessage() => "Ready.";
}
```

`LoogaInspectorMessageAttribute` is a class-level setup warning driven by a bool field, property, or method.

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

Use `labelMember` and `valueMember` for object lists:

```csharp
[Dropdown(nameof(_entries), labelMember: "Name", valueMember: "Id")]
[SerializeField] private string _selectedId;
```

### Filtered Enums

`FilteredEnumAttribute` draws an enum field as a dropdown limited by a provider member. The provider can return enum values, enum names, ints, or a collection of those.

```csharp
[FilteredEnum(nameof(GetAllowedModes))]
[SerializeField] private FireMode _mode;

private FireMode[] GetAllowedModes()
{
    return new[] { FireMode.Single, FireMode.Burst };
}
```

### Hierarchical Asset Dropdowns

`HierarchicalAssetDropdownAttribute` draws object reference or string fields as a project asset dropdown. Entries are grouped by a path member on each asset, such as `Path`, `TagPath`, or `DisplayPath`.

```csharp
[HierarchicalAssetDropdown]
[SerializeField] private GameplayTagDefinition _tag;
```

For string fields, pass the asset type explicitly:

```csharp
[HierarchicalAssetDropdown(typeof(GameplayTagDefinition), pathMemberName: "Path")]
[SerializeField] private string _tagPath;
```

Use `searchFilter` to restrict the project search and `includeNone` to hide or show the `None` option.

### Unity Selectors

These draw common Unity dropdowns:

```csharp
[Tag]
[SerializeField] private string _targetTag;

[Layer]
[SerializeField] private int _collisionLayer;

[PhysicsLayer]
[SerializeField] private int _physicsLayer;

[PhysicsLayerMask]
[SerializeField] private LayerMask _hitMask;

[SortingLayer]
[SerializeField] private string _sortingLayer;

[NavMeshArea]
[SerializeField] private int _walkableArea;

[NavMeshAreaMask]
[SerializeField] private int _agentAreaMask;

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

## Assets And Catalogs

### Scriptable Object Fields

`ExposeScriptableAttribute` draws an assigned ScriptableObject inline. When the field is empty, the drawer shows a create button beside the object field. Creating an asset opens Unity's save panel, assigns the new asset, and pings it in the Project window while keeping the current inspector selection.

```csharp
[ExposeScriptable(showScriptField: false, expandedByDefault: true, createButtonLabel: "New")]
[SerializeField] private WeaponVfxProfile _vfxProfile;
```

If the declared field type has multiple concrete ScriptableObject types, the create button opens a type menu first.

`AssetLinkAttribute` keeps an asset reference compact and adds quick Open/Ping controls. Use it for fields where designers often jump to the assigned asset.

```csharp
[AssetLink(readOnly: true)]
[SerializeField] private WeaponProfile _profile;
```

### Catalog Fields

`LoogaCatalogAttribute` draws a catalog asset list with add, delete, rename, and sync support. It is meant for ScriptableObject catalog assets that own sub-assets, such as gameplay tags, events, stats, or quest definitions.

```csharp
[LoogaCatalog("Gameplay Tags", TreePath = "Path", CreateName = "New Tag")]
[SerializeField] private List<GameplayTagDefinition> _tags;
```

Useful options:

```csharp
[LoogaCatalog("Definitions", StoreAsSubAssets = true, AllowAdd = true, AllowDelete = true)]
[SerializeField] private List<MyDefinition> _definitions;
```

`TreePath` enables hierarchical list drawing when entries expose a path-like string. Renaming parent paths updates child paths when supported by the drawer.

## Unity Domain Helpers

### Animator Helpers

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

### Shader And Material Helpers

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

### Volume Override Values

`VolumeOverrideValueAttribute` works with `VolumeValue<T>` to select a value inside a referenced `VolumeProfile`. The drawer first selects the volume override, then the matching parameter of type `T`.

```csharp
[SerializeField] private VolumeProfile _hudVolumeProfile;

[VolumeOverrideValue(nameof(_hudVolumeProfile))]
[SerializeField] private VolumeValue<float> _lensIntensity;
```

At runtime, use the stored reference directly:

```csharp
_lensIntensity.SetValue(-0.1f);
float current = _lensIntensity.GetValue();
```

You can also read or write against another profile:

```csharp
_lensIntensity.SetValue(0f, overrideProfile);
```

## Actions And Buttons

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

`BoolButtonAttribute` draws a bool with a button that calls a method.

```csharp
[BoolButton(nameof(TogglePreview), "Preview")]
[SerializeField] private bool _previewEnabled;

private void TogglePreview() { }
```

`OpenEditorWindowAttribute` adds a button that executes a Unity menu item. It can be used on a field or at class level.

```csharp
[OpenEditorWindow("Open Item Database", "Kubera/Databases/Item Database")]
[SerializeField] private bool _openItemDatabase;
```

```csharp
[OpenEditorWindow("Open Project Hub", "Kubera/Hub")]
public sealed class ProjectSettingsComponent : MonoBehaviour
{
}
```

## Optional Integrations

Looga Inspector can enable optional editor acceleration for ZLinq from:

`LoogaSoft/Inspector/Enable ZLinq Support`

Only enable optional support when the dependency is installed in the project and the package is editable or updated at the source package level.

## When To Use A Custom Editor

Prefer Looga Inspector attributes for common inspector shaping: grouping, tabs, conditional fields, validation, inline ScriptableObjects, dropdowns, catalog lists, quick links, and simple tables.

Keep a custom editor or editor window when the workflow needs custom previews, graph editing, search/filter-heavy interfaces, drag-and-drop canvases, asset migration tools, or runtime debugging panels.
