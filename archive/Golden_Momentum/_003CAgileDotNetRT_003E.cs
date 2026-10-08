// Decompiled with JetBrains decompiler
// Type: <AgileDotNetRT>
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

#nullable disable
[SecuritySafeCritical]
internal class \u003CAgileDotNetRT\u003E
{
  private static bool inited;
  private static Assembly runtimeAssembly;

  [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
  [MethodImpl(MethodImplOptions.ForwardRef)]
  private static extern IntPtr LoadLibraryA([In] string obj0);

  [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
  [MethodImpl(MethodImplOptions.ForwardRef)]
  private static extern IntPtr GetProcAddress([In] IntPtr obj0, [In] string obj1);

  [DllImport("AgileDotNetRT.dll", CharSet = CharSet.Ansi)]
  [MethodImpl(MethodImplOptions.ForwardRef)]
  private static extern int _Initialize([In] IntPtr obj0);

  [DllImport("AgileDotNetRT64.dll", CharSet = CharSet.Ansi)]
  [MethodImpl(MethodImplOptions.ForwardRef)]
  private static extern int _Initialize64([In] IntPtr obj0);

  [DllImport("AgileDotNetRT.dll", CharSet = CharSet.Ansi)]
  [MethodImpl(MethodImplOptions.ForwardRef)]
  private static extern void _AtExit();

  [DllImport("AgileDotNetRT64.dll", EntryPoint = "_AtExit", CharSet = CharSet.Ansi)]
  [MethodImpl(MethodImplOptions.ForwardRef)]
  private static extern void _AtExit64();

  internal static IntPtr Load()
  {
    WindowsImpersonationContext impersonationContext;
    lock (typeof (\u003CAgileDotNetRT\u003E))
    {
      try
      {
        impersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero);
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        string name;
        string str;
        if (IntPtr.Size == 4)
        {
          name = "ea2b14ad-abc7-41d9-8ed6-3370288bfa05";
          str = "AgileDotNetRT";
        }
        else
        {
          name = "30438cbb-4bdb-4fa0-b7e3-dc1c3424d986";
          str = "AgileDotNetRT64";
        }
        BinaryReader binaryReader = new BinaryReader((Stream) new GZipStream(executingAssembly.GetManifestResourceStream(name), CompressionMode.Decompress));
        byte[] buffer = binaryReader.ReadBytes(binaryReader.ReadInt32());
        string path1 = $"{Path.GetTempPath()}{name}\\";
        Directory.CreateDirectory(path1);
        string path2 = $"{path1}{str}.dll";
        if (!File.Exists(path2))
        {
          FileStream fileStream = File.OpenWrite(path2);
          fileStream.Write(buffer, 0, buffer.Length);
          fileStream.Close();
          FileSystemAccessRule rule = new FileSystemAccessRule((IdentityReference) new SecurityIdentifier("S-1-1-0"), FileSystemRights.ReadAndExecute, AccessControlType.Allow);
          FileSecurity accessControl = File.GetAccessControl(path2);
          accessControl.AddAccessRule(rule);
          File.SetAccessControl(path2, accessControl);
        }
        return \u003CAgileDotNetRT\u003E.LoadLibraryA(path2);
      }
      finally
      {
        impersonationContext.Undo();
      }
    }
  }

  internal static int InitializeThroughDelegate([In] IntPtr obj0)
  {
    return ((InitializeDelegate) Marshal.GetDelegateForFunctionPointer(\u003CAgileDotNetRT\u003E.GetProcAddress(\u003CAgileDotNetRT\u003E.Load(), "_Initialize"), typeof (InitializeDelegate)))(obj0);
  }

  internal static int InitializeThroughDelegate64([In] IntPtr obj0)
  {
    return ((InitializeDelegate) Marshal.GetDelegateForFunctionPointer(\u003CAgileDotNetRT\u003E.GetProcAddress(\u003CAgileDotNetRT\u003E.Load(), "_Initialize64"), typeof (InitializeDelegate)))(obj0);
  }

  internal static void ExitThroughDelegate()
  {
    ((ExitDelegate) Marshal.GetDelegateForFunctionPointer(\u003CAgileDotNetRT\u003E.GetProcAddress(\u003CAgileDotNetRT\u003E.Load(), "_AtExit"), typeof (ExitDelegate)))();
  }

  internal static void ExitThroughDelegate64()
  {
    ((ExitDelegate) Marshal.GetDelegateForFunctionPointer(\u003CAgileDotNetRT\u003E.GetProcAddress(\u003CAgileDotNetRT\u003E.Load(), "_AtExit64"), typeof (ExitDelegate)))();
  }

  internal static void DomainUnload([In] object obj0, [In] EventArgs obj1)
  {
    if (IntPtr.Size == 4)
      \u003CAgileDotNetRT\u003E.ExitThroughDelegate();
    else
      \u003CAgileDotNetRT\u003E.ExitThroughDelegate64();
  }

  internal static void Initialize()
  {
    if (\u003CAgileDotNetRT\u003E.inited)
      return;
    RuntimeMethodHandle methodHandle = new StackTrace().GetFrame(0).GetMethod().MethodHandle;
    if ((IntPtr.Size != 4 ? \u003CAgileDotNetRT\u003E.InitializeThroughDelegate64(methodHandle.Value) : \u003CAgileDotNetRT\u003E.InitializeThroughDelegate(methodHandle.Value)) == 1)
      AppDomain.CurrentDomain.DomainUnload += new EventHandler(\u003CAgileDotNetRT\u003E.DomainUnload);
    \u003CAgileDotNetRT\u003E.inited = true;
  }

  internal static void PostInitialize()
  {
  }
}
