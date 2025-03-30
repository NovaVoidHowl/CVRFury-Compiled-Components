using UnityEngine;

namespace VRC.Dynamics.ManagedTypes
{
  [AddComponentMenu("")]
  public class VRCPositionConstraintBase : VRCConstraintBase
  {
    public Vector3 PositionAtRest = Vector3.zero;
    public Vector3 PositionOffset = Vector3.zero;
    public bool AffectsPositionX = true;
    public bool AffectsPositionY = true;
    public bool AffectsPositionZ = true;

    protected override VRCConstraintPositionMode PositionMode => VRCConstraintPositionMode.MatchPosition;
    protected override VRCConstraintRotationMode RotationMode => VRCConstraintRotationMode.None;
    protected override VRCConstraintScaleMode ScaleMode => VRCConstraintScaleMode.None;

    public sealed override bool AffectsAnyAxis()
    {
      return AffectsPositionX || AffectsPositionY || AffectsPositionZ;
    }
  }
}
