// This stub ensures Unity generates the Assembly-CSharp-Editor assembly.
// It helps work around resolver issues when packages expect the editor assembly to exist.
#if UNITY_EDITOR
using UnityEditor;

internal static class DummyEditor
{
    // Intentionally empty.
}
#endif

