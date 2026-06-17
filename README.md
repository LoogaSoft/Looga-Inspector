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
the button opens a creation menu. New assets are created beside the inspected
asset when possible, or in `Assets` as a fallback.
