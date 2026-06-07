# PhysBone Collider Converter

Convert VRCPhysBoneCollider components to CVR-compatible colliders (DynamicBone,
MagicaCloth 1, MagicaCloth 2) directly from the Unity Inspector.

---

## Status

Implemented and live in the custom inspector.

This document reflects current behavior:

- Existing converted colliders are scoped to the current source collider only.
- Repeated conversions update existing owned colliders in place (no duplicate stacking).
- Convert button label changes to Re-Convert when owned conversions already exist.
- Removing the source VRC PhysBone Collider also removes its owned marker components.

---

## File Location

Primary implementation file:

- CVRFury-Compiled-Components/VRCStubConvertAndUI/VRCPBUIAndConverter/Editors/VRCPhysBoneColliderEditor.cs

---

## Runtime Package Detection

Because this editor is compiled into a precompiled DLL, package support is detected at runtime via type lookup:

- DynamicBoneCollider
- MagicaCloth.MagicaSphereCollider / MagicaCapsuleCollider / MagicaPlaneCollider
- MagicaCloth2.MagicaSphereCollider / MagicaCapsuleCollider / MagicaPlaneCollider

No compile-time per-project define guards are required for the conversion logic.

---

## Inspector UI Behavior

The inspector shows:

1. Shape and root transform info.
2. Existing converted colliders list (clickable entries).
3. Convert toggles:
   - Dynamic Bone Collider
   - MagicaCloth 1 Collider
   - MagicaCloth 2 Collider
4. Convert or Re-Convert button.
5. Remove VRC PhysBone Collider Component button.

### Toggle rules

- Dynamic Bone toggle is disabled when package is missing.
- Dynamic Bone toggle is also disabled for Plane shape.
- MC1 and MC2 toggles are disabled when required collider types are missing.

### Convert button label

- Shows Convert when no owned converted colliders exist for this source.
- Shows Re-Convert when at least one owned converted collider exists.
- After a successful conversion pass, label is set to Re-Convert.

---

## Ownership Marker System

A lightweight marker component is attached to converted collider GameObjects:

- Type: CVRFuryConvertedColliderMarker
- Field: sourceCollider (VRCPhysBoneCollider reference)

Purpose:

- Associate each converted collider with the exact source collider that created it.
- Prevent cross-contamination in the Existing converted colliders panel.
- Enable in-place reconversion updates and safe cleanup.

### Existing conversions panel filtering

The panel only shows converted colliders where:

- Component type matches DB/MC1/MC2 collider type, and
- Marker exists, and
- marker.sourceCollider equals the currently inspected VRCPhysBoneCollider.

So even if multiple converted sets exist under the same hierarchy, only the relevant set is displayed.

---

## Conversion Strategy (Current)

All conversion paths now use update-or-create behavior:

1. Search for an existing owned converted collider GameObject under effective root.
2. If found, reuse it and update fields.
3. If not found, create a new child GameObject and add the target collider component.
4. Attach ownership marker only on first creation.

This is implemented through helper methods that locate existing owned conversions by marker + target type.

---

## Effective Root and Placement

Effective root is:

- src.rootTransform when set, otherwise
- src.transform

Converted collider GameObjects live under the effective root.

Naming convention for newly created objects:

- {sourceName}_DBCollider
- {sourceName}_MC1SphereCollider
- {sourceName}_MC1CapsuleCollider
- {sourceName}_MC1PlaneCollider
- {sourceName}_MC2SphereCollider
- {sourceName}_MC2CapsuleCollider
- {sourceName}_MC2PlaneCollider

Note: during reconvert/update, existing names are preserved.

---

## Shape Mapping Details

### DynamicBone

- Plane is not supported.
- Radius maps to m_Radius.
- Capsule height maps to m_Height (sphere uses 0).
- Center maps to m_Center.
- Bound maps from insideBounds.
- Direction is fixed to Y axis (enum index 1).
- For capsule, child local rotation is set from source rotation.

### MagicaCloth 1

- Sphere: Radius and Center set via properties.
- Capsule:
  - AxisMode fixed to Y.
  - Uses half-length model: max(src.height / 2 - src.radius, 0), clamped to [0,1].
  - StartRadius and EndRadius set from source radius.
  - Center set from source position.
  - Warning emitted when half-length exceeds 1.0.
- Plane: Center set; local rotation copied from source for normal alignment.

### MagicaCloth 2

- Sphere:
  - center field set.
  - SetSize(float radius) used when available.
- Capsule:
  - center field set.
  - direction fixed to Y.
  - radiusSeparation set false.
  - alignedOnCenter set true.
  - SetSize(startRadius, endRadius, length) used when available.
  - local rotation copied from source.
- Plane:
  - center field set.
  - local rotation copied from source for normal alignment.

---

## Warnings and Dialogs

Convert/Re-Convert action shows summary dialog listing selected conversion targets.

Additional warnings are shown when applicable:

- insideBounds with MC1/MC2 (outside mode approximation)
- Plane orientation verification note for MC1/MC2
- MC1 capsule half-length clamped note when required

---

## Remove Button Behavior

Remove VRC PhysBone Collider Component now performs cleanup before source removal:

1. Finds all CVRFuryConvertedColliderMarker components under effective root.
2. Removes markers whose sourceCollider matches the source being removed.
3. Removes the source VRC PhysBone Collider component itself.

This prevents stale ownership markers from remaining after source deletion.

---

## Functional Summary

Current converter behavior is:

- Type-safe runtime package detection
- Scoped existing-conversions listing per source collider ownership
- Update-in-place reconversion (no repeated duplicate sets)
- Dynamic Convert/Re-Convert button labeling
- Marker cleanup on source removal

This is the intended baseline for future collider-converter enhancements.
