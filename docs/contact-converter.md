# Contact Converter

Convert VRC Contact Sender and VRC Contact Receiver components to CVR-compatible
components directly from the Unity Inspector.

---

## Status

Implemented and live in the custom inspectors.

This document reflects current behavior:

- Sender conversion creates one CVRPointer per collision tag.
- Receiver conversion creates one CVRAdvancedAvatarSettingsTrigger.
- Converted objects are parented under rootTransform when set.
- If rootTransform is not set, converted objects are parented under the source component GameObject.
- Source VRC contact component is removed after successful conversion.

---

## File Location

Primary implementation files:

- CVRFury-Compiled-Components/VRCStubConvertAndUI/VRCPCUIAndConverter/Editors/VRCContactSenderEditor.cs
- CVRFury-Compiled-Components/VRCStubConvertAndUI/VRCPCUIAndConverter/Editors/VRCContactReceiverEditor.cs

---

## Runtime Package Detection

Because these editors are compiled into precompiled DLLs, CVR support is detected at runtime via reflection type lookup.

Sender converter requires:

- CVRPointer

Receiver converter requires:

- CVRAdvancedAvatarSettingsTrigger
- CVRAdvancedAvatarSettingsTriggerTask
- CVRAdvancedAvatarSettingsTriggerTaskStay

If required types are missing, conversion logs an error and does not modify the source component.

---

## Inspector UI Behavior

Both inspectors show:

1. Stub version label.
2. UI version label.
3. Warning/info box explaining conversion requirement.
4. Single Convert button.

Sender button:

- Convert to Chillout VR Pointer

Receiver button:

- Convert to Chillout VR Advanced Avatar Settings Trigger

Current behavior is one-way conversion. There is no Re-Convert button because the
source component is removed once conversion succeeds. This is due to the fact that there
is only one possible item to convert to, rather than multiple options like colliders have.

---

## Effective Root and Placement

Effective parent transform is:

- src.rootTransform when set, otherwise
- src.transform

Converted child GameObjects are created under that effective parent.

This means:

- If rootTransform is assigned, source position is applied relative to a zeroed local transform under rootTransform.
- If rootTransform is not assigned, behavior matches legacy placement under the original component GameObject.

---

## Contact Sender Conversion

### Output

For each collision tag in sender.collisionTags:

- Create child GameObject named CVR_Pointer{index}_From_{sourceName}.
- Add trigger collider mapped from source shape.
- Add CVRPointer component.
- Set pointer type from that collision tag.

### Shape mapping

Sphere:

- Add SphereCollider.
- radius = sender.radius
- isTrigger = true
- localPosition = sender.position

Capsule:

- Add CapsuleCollider.
- radius = sender.radius
- height = sender.height
- direction = 1 (Y axis)
- isTrigger = true
- localPosition = sender.position
- localRotation = sender.rotation

Unknown shape types log an error.

---

## Contact Receiver Conversion

### Output

- Create one child GameObject named CVR_Trigger_From_{sourceName}.
- Add trigger collider mapped from source shape.
- Add CVRAdvancedAvatarSettingsTrigger component.
- Configure trigger settings and tasks based on receiver type.

### Base trigger property mapping

- areaSize = new Vector3(radius / 100, radius / 100, radius / 100)
- useAdvancedTrigger = true
- isLocalInteractable = allowSelf
- isNetworkInteractable = allowOthers
- allowParticleInteraction = false
- allowedTypes = collisionTags array

### Shape mapping

Sphere:

- Add SphereCollider.
- radius = receiver.radius
- isTrigger = true
- localPosition = receiver.position

Capsule:

- Add CapsuleCollider.
- radius = receiver.radius
- height = receiver.height
- direction = 1 (Y axis)
- isTrigger = true
- localPosition = receiver.position
- localRotation = receiver.rotation

Unknown shape types log an error.

### Receiver type task mapping

Constant:

- enterTasks: settingValue 1.0, delay 0, holdTime 0, updateMethod Override
- exitTasks: settingValue 0.0, delay 0, holdTime 0, updateMethod Override

OnEnter:

- enterTasks task 1: settingValue 1.0, delay 0, holdTime 0, updateMethod Override
- enterTasks task 2: settingValue 0.0, delay 0.5, holdTime 0, updateMethod Override

Proximity:

- stayTasks: minValue 1.0, maxValue 0.0, updateMethod SetFromDistance

All task entries use settingName = receiver.parameter.

---

## Reflection and Field/Property Assignment

Converters use helper logic that attempts field assignment first, then property assignment.

This supports minor API differences between CCK versions where members may be fields or properties.

---

## Dirty Marking and Source Removal

After successful conversion:

- Sender: marks source GameObject dirty, and rootTransform GameObject dirty when present.
- Receiver: marks new trigger GameObject dirty, source GameObject dirty, and rootTransform GameObject dirty when present.
- Original VRC contact component is removed with DestroyImmediate.

---

## Current Limitations

- No marker ownership system is used for contacts.
- No update-in-place reconversion path exists.
- Conversion is destructive to the source component (component removal).
- Converted naming may create duplicates if users manually recreate VRC components and convert repeatedly.

---

## Functional Summary

Current contact converter behavior is:

- Runtime type detection for CVR target types
- Sender to CVRPointer conversion per collision tag
- Receiver to CVRAdvancedAvatarSettingsTrigger conversion with receiver-type task mapping
- rootTransform-aware placement for both sender and receiver conversions
- Automatic source component removal after conversion

This is the current baseline for future contact-converter enhancements.
