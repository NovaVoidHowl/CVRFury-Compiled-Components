using System;

namespace VRC.SDK3.Dynamics.PhysBone.Editors.Common
{
  public static class UIVersion
  {
    private static readonly Version _version = new Version(2, 3, 0);

    public static string CurrentVersion => _version.ToString();
    public static Version AsVersion => _version;

    public static bool IsVersionAtLeast(Version otherVersion)
    {
      return _version.CompareTo(otherVersion) >= 0;
    }

    public static bool IsVersionAtLeast(string otherVersion)
    {
      return IsVersionAtLeast(new Version(otherVersion));
    }
  }
}
