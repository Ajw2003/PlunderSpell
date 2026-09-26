using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Milestone 0 headless verification. Runs automatically on every domain reload (script
// recompile / editor start) via [InitializeOnLoad], and uses reflection to confirm the custom
// gravity module was fully removed: neither GravityReceiver nor GravitySource may exist in any
// loaded assembly. A clean recompile that reaches this code is itself proof the project compiles;
// this adds an explicit, greppable pass/fail marker to the Editor console/log.
//
// Success marker (grep for this in the Editor log): M0_GRAVITY_REMOVAL_OK
[InitializeOnLoad]
public static class M0CompilationValidator
{
    private static readonly string[] ForbiddenTypeNames =
    {
        "GravityReceiver",
        "GravitySource",
    };

    static M0CompilationValidator()
    {
        var offenders = ForbiddenTypeNames
            .Where(TypeExistsInLoadedAssemblies)
            .ToArray();

        if (offenders.Length == 0)
        {
            Debug.Log("M0_GRAVITY_REMOVAL_OK - gravity module removed; project uses standard -Y world gravity.");
        }
        else
        {
            Debug.LogError(
                "M0_GRAVITY_REMOVAL_FAILED - the following gravity types still exist in loaded " +
                "assemblies: " + string.Join(", ", offenders) +
                ". Milestone 0 requires GravityReceiver and GravitySource to be fully removed.");
        }
    }

    // Matches by simple type name across every loaded assembly, so it catches the type regardless
    // of namespace or which assembly it was compiled into.
    private static bool TypeExistsInLoadedAssemblies(string simpleTypeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // A partially-loadable assembly still exposes the types it could load.
                types = e.Types.Where(t => t != null).ToArray();
            }

            if (types.Any(t => t != null && t.Name == simpleTypeName))
            {
                return true;
            }
        }

        return false;
    }
}
