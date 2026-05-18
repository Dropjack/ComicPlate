using System.Runtime.InteropServices;

namespace ComicPlate.App.Services;

internal static class MacOSDockIconService
{
    private const string IconRelativePath = "platform/mac/ComicPlate_Logo.icns";
    private const string ObjectiveCRuntime = "/usr/lib/libobjc.A.dylib";

    public static void ApplyDevelopmentDockIcon()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, IconRelativePath);
        if (!File.Exists(iconPath))
        {
            return;
        }

        var pathHandle = IntPtr.Zero;
        try
        {
            pathHandle = Marshal.StringToCoTaskMemUTF8(iconPath);
            var nsPath = SendIntPtr(
                Send(ObjCClass("NSString"), "alloc"),
                "initWithUTF8String:",
                pathHandle);
            var image = SendIntPtr(
                Send(ObjCClass("NSImage"), "alloc"),
                "initWithContentsOfFile:",
                nsPath);

            if (image == IntPtr.Zero)
            {
                Release(nsPath);
                return;
            }

            var application = Send(ObjCClass("NSApplication"), "sharedApplication");
            SendVoidIntPtr(application, "setApplicationIconImage:", image);

            Release(image);
            Release(nsPath);
        }
        finally
        {
            if (pathHandle != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pathHandle);
            }
        }
    }

    private static IntPtr ObjCClass(string name)
    {
        return objc_getClass(name);
    }

    private static IntPtr Selector(string name)
    {
        return sel_registerName(name);
    }

    private static IntPtr Send(IntPtr receiver, string selector)
    {
        return objc_msgSend(receiver, Selector(selector));
    }

    private static IntPtr SendIntPtr(IntPtr receiver, string selector, IntPtr argument)
    {
        return objc_msgSend(receiver, Selector(selector), argument);
    }

    private static void SendVoidIntPtr(IntPtr receiver, string selector, IntPtr argument)
    {
        objc_msgSend_void(receiver, Selector(selector), argument);
    }

    private static void Release(IntPtr instance)
    {
        if (instance != IntPtr.Zero)
        {
            Send(instance, "release");
        }
    }

    [DllImport(ObjectiveCRuntime)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjectiveCRuntime)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjectiveCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjectiveCRuntime, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector, IntPtr argument);
}
