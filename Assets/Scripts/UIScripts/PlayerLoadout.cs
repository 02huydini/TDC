    using System.Collections.Generic;
using UnityEngine;

public static class PlayerLoadout {
    public const int MaxDeckSize = 4;

    private static readonly GameObject[] slots = new GameObject[MaxDeckSize];

    public static IReadOnlyList<GameObject> SelectedTowers {
        get {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject tower in slots) {
                if (tower != null) result.Add(tower);
            }
            return result;
        }
    }

    public static bool IsFull => FirstEmptySlot() < 0;

    public static GameObject GetSlot(int index) {
        return (index >= 0 && index < MaxDeckSize) ? slots[index] : null;
    }

    public static bool IsSelected(GameObject towerPrefab) {
        return towerPrefab != null && IndexOf(towerPrefab) >= 0;
    }

    public static int IndexOf(GameObject towerPrefab) {
        if (towerPrefab == null) return -1;
        for (int i = 0; i < MaxDeckSize; i++) {
            if (slots[i] == towerPrefab) return i;
        }
        return -1;
    }

    public static int FirstEmptySlot() {
        for (int i = 0; i < MaxDeckSize; i++) {
            if (slots[i] == null) return i;
        }
        return -1;
    }
    public static bool AssignToSlot(int index, GameObject towerPrefab) {
        if (towerPrefab == null || index < 0 || index >= MaxDeckSize) return false;

        int existing = IndexOf(towerPrefab);
        if (existing >= 0) slots[existing] = null;

        slots[index] = towerPrefab;
        return true;
    }

    public static void RemoveFromSlot(int index) {
        if (index >= 0 && index < MaxDeckSize) slots[index] = null;
    }

    public static void Remove(GameObject towerPrefab) {
        int index = IndexOf(towerPrefab);
        if (index >= 0) slots[index] = null;
    }
    public static bool Toggle(GameObject towerPrefab) {
        if (towerPrefab == null) return false;

        int existing = IndexOf(towerPrefab);
        if (existing >= 0) {
            slots[existing] = null;
            return false;
        }

        int empty = FirstEmptySlot();
        if (empty < 0) return false;

        slots[empty] = towerPrefab;
        return true;
    }

    public static void Clear() {
        for (int i = 0; i < MaxDeckSize; i++) slots[i] = null;
    }
}