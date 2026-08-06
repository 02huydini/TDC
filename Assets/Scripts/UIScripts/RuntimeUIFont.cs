/*
    Unity renamed its built-in legacy UI font from "Arial.ttf" to "LegacyRuntime.ttf"
    in 2022.2+. This project targets 2021.3.45f2, where only "Arial.ttf" exists -
    asking for "LegacyRuntime.ttf" silently returns null, which made runtime-built
    Text/InputField components render with no visible font (looked like nothing was
    happening when typing). Tries both, oldest-version name first.
*/
using UnityEngine;

public static class RuntimeUIFont {
    private static Font cached;
    private static bool tried;

    public static Font Get() {
        if (tried) return cached;
        tried = true;

        cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (cached == null) cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cached == null) Debug.LogWarning("RuntimeUIFont: no built-in legacy font found under either name.");

        return cached;
    }
}
