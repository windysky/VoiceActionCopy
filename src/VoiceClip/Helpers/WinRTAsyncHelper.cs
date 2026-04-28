using System.Reflection;

namespace VoiceClip.Helpers;

/// <summary>
/// Reflection-based wrapper for WinRT AsTask() extension methods.
/// Required because Windows SDK and CsWinRt packages have overlapping AsTask() methods
/// that cannot be resolved in WPF. Based on Rick Strahl's approach.
/// </summary>
public static class WinRTAsyncHelper
{
    /// <summary>
    /// Converts a WinRT IAsyncAction or IAsyncOperation to a Task using reflection.
    /// </summary>
    /// <param name="action">The WinRT async action or operation object.</param>
    /// <returns>A Task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    public static Task AsTask(object action)
    {
        ArgumentNullException.ThrowIfNull(action);

        // First, try reflection to find the WinRT AsTask extension method
        if (TryReflectionAsTask(action, out var task))
        {
            return task!;
        }

        // Fallback: if the object is already a Task, return it directly
        if (action is Task t)
        {
            return t;
        }

        throw new InvalidOperationException(
            $"Cannot convert object of type {action.GetType().Name} to Task. " +
            "Ensure WinRT packages are properly installed.");
    }

    /// <summary>
    /// Tries to convert a WinRT async object to a Task.
    /// Returns false if conversion fails instead of throwing.
    /// </summary>
    /// <param name="action">The WinRT async object (may be null).</param>
    /// <param name="task">The resulting Task, or null if conversion failed.</param>
    /// <returns>True if conversion succeeded, false otherwise.</returns>
    public static bool TryAsTask(object? action, out Task? task)
    {
        task = null;

        if (action == null)
        {
            return false;
        }

        try
        {
            task = AsTask(action);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to invoke AsTask via reflection on the WinRT extension methods.
    /// </summary>
    private static bool TryReflectionAsTask(object action, out Task? task)
    {
        task = null;

        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName()?.Name == "Microsoft.Windows.SDK.NET");

            if (assembly == null)
            {
                return false;
            }

            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.FullName == "System.WindowsRuntimeSystemExtensions");

            if (type == null)
            {
                return false;
            }

            var method = type.GetMethod("AsTask", new[] { action.GetType() });

            if (method == null)
            {
                return false;
            }

            task = method.Invoke(null, new object[] { action }) as Task;
            return task != null;
        }
        catch
        {
            return false;
        }
    }
}
