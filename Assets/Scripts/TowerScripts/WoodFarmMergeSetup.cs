/*
    "Create a tower merge Archer > Ballista to create a white tower (any
    placeholder graphics will work) that make Wood for each wave, as well
    display how much they make on that tower."

    Registers a two-way merge recipe (Archer+Ballista -> Wood Farm, and
    Ballista+Archer -> Wood Farm, since Towers.TryGetMergeResult() is
    one-directional per recipe - see Tower.cs) onto the two existing tower
    prefab assets, and builds the Wood Farm Tower's template GameObject entirely
    at runtime (white square sprite as a stand-in - swap SpriteRenderer.sprite
    for real art later), since it has no hand-authored prefab of its own.

    NOT self-installing like most of this project's other runtime scripts - it
    needs two real prefab references (Archer Tower, Ballista Tower) that don't
    exist anywhere at runtime to auto-discover (they're not under a Resources/
    folder, so Resources.Load can't find them either). One-time Inspector setup
    required, same as ShopManager's woodManager/ectoManager rewiring:

      1. Drop this component on any persistent GameObject already in the scene
         (e.g. the same one PlacementManager lives on).
      2. Drag Assets/Prefabs/Archer Tower.prefab into archerTowerPrefab.
      3. Drag Assets/Prefabs/Ballista Tower.prefab into ballistaTowerPrefab.

    Without that wiring this silently no-ops (logs a warning) rather than
    throwing - matches how the rest of this project treats an unwired manager
    reference.
*/
using UnityEngine;

public class WoodFarmMergeSetup : MonoBehaviour {
    [Tooltip("Drag Assets/Prefabs/Archer Tower.prefab here.")]
    public GameObject archerTowerPrefab;

    [Tooltip("Drag Assets/Prefabs/Ballista Tower.prefab here.")]
    public GameObject ballistaTowerPrefab;

    [Tooltip("Wood the merged tower pays out at the end of every wave.")]
    public int woodPerWave = 10;

    private void Awake() {
        if (archerTowerPrefab == null || ballistaTowerPrefab == null) {
            Debug.LogWarning("WoodFarmMergeSetup: archerTowerPrefab/ballistaTowerPrefab not assigned in the " +
                              "Inspector - the Archer+Ballista -> Wood Farm merge is disabled.");
            return;
        }

        Towers archer = archerTowerPrefab.GetComponent<Towers>();
        Towers ballista = ballistaTowerPrefab.GetComponent<Towers>();
        if (archer == null || ballista == null) {
            Debug.LogWarning("WoodFarmMergeSetup: assigned prefabs are missing a Towers component - merge disabled.");
            return;
        }

        GameObject template = BuildTemplate(archer);

        archer.AddMergeRecipe(ballista.getName(), template);
        ballista.AddMergeRecipe(archer.getName(), template);
    }

    private GameObject BuildTemplate(Towers menuSource) {
        GameObject go = new GameObject("Wood Farm Tower");
        go.SetActive(false); // template only - PlacementManager Instantiate()s from this, it never runs on its own
        DontDestroyOnLoad(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BuildWhiteSquareSprite();
        sr.sortingOrder = 5;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        WoodFarmTower farm = go.AddComponent<WoodFarmTower>();
        farm.ConfigureAsMergeResult(woodPerWave);

        // Towers.OnMouseDown() needs `menu` (the Upgrade UI panel) set or clicking
        // this tower throws a NullReferenceException - reuse whatever the Archer
        // prefab is already wired to in the Inspector, since it's the same
        // shared Upgrade UI every tower opens.
        farm.menu = menuSource.menu;

        return go;
    }

    private static Sprite BuildWhiteSquareSprite() {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }
}
