using System;

namespace VRC.Dynamics
{
  public class ContactReceiver : ContactBase
  {
    public bool allowSelf;
    public float paramValue;
    public float collisionValue;

    public float minVelocity;
    public IAnimParameterAccess paramAccess;

    public ReceiverType receiverType;

    public bool localOnly;

    public bool allowOthers;

    public string parameter;

    [Serializable]
    public enum ReceiverType
    {
      Constant = 0,
      OnEnter = 1,
      Proximity = 2
    }
    public class IAnimParameterAccess
    {
      bool boolVal { get; set; }
      int intVal { get; set; }
      float floatVal { get; set; }
    }
  }
  
}
