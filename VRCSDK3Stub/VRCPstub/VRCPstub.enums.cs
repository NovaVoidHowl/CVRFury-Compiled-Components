namespace VRC.Dynamics.ManagedTypes
{
  public enum VRCConstraintPositionMode
  {
    None,
    MatchPosition,
    ChildPosition
  }

  public enum VRCConstraintRotationMode
  {
    None,
    MatchRotation,
    AimTowardsPosition,
    LookAtPosition
  }

  public enum VRCConstraintScaleMode
  {
    None,
    MatchScale
  }

  public enum WorldUpType
  {
    SceneUp,
    ObjectUp,
    ObjectRotationUp,
    Vector,
    None
  }
}
