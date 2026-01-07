using System;
using System.Reflection;
using UnityEngine;

namespace SurvivorMode
{
    // Applies persistent Survivor upgrades to spawned monsters (server-side intended).
    public class MonsterUpgradeApplier : MonoBehaviour
    {
        private bool _applied;

        // Apply upgrades immediately to a given monster GameObject
        public static void ApplyOn(GameObject monster)
        {
            if (monster == null) return;
            var applier = monster.GetComponent<MonsterUpgradeApplier>();
            if (applier == null) applier = monster.AddComponent<MonsterUpgradeApplier>();
            applier.ApplyNow();
        }

        public void ApplyNow()
        {
            if (_applied) return;
            _applied = true;

            // Compute factors
            float level = Mathf.Max(0, SurvivorModifiers.permanentMonsterUpgradeLevel);
            float healthFactor = 1f + 0.20f * level; // +25% PV par niveau
            float damageFactor = SurvivorModifiers.monsterDamageMultiplier * (1f + 0.07f * level); // +20% dégâts par niveau, fois multiplicateur d'événement

            try
            {
                // Apply to common field/property names via reflection
                ApplyHealthScaling(gameObject, healthFactor);
                ApplyDamageScaling(gameObject, damageFactor);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MonsterUpgradeApplier] Error while applying upgrades: " + ex.Message);
            }
        }

        private static void ApplyHealthScaling(GameObject root, float factor)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var b in behaviours)
            {
                if (b == null) continue;
                var t = b.GetType();
                // Fields
                foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    string n = f.Name;
                    if (!f.FieldType.IsPrimitive && f.FieldType != typeof(float) && f.FieldType != typeof(int)) continue;
                    if (n.Equals("maxHealth", StringComparison.OrdinalIgnoreCase) || n.Equals("maxHP", StringComparison.OrdinalIgnoreCase))
                    {
                        ScaleField(b, f, factor, clampMin: 1f);
                    }
                    if (n.Equals("health", StringComparison.OrdinalIgnoreCase) || n.Equals("hp", StringComparison.OrdinalIgnoreCase))
                    {
                        ScaleField(b, f, factor, clampMin: 1f);
                    }
                }
                // Properties
                foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    if (!p.CanRead || !p.CanWrite) continue;
                    if (p.PropertyType != typeof(float) && p.PropertyType != typeof(int)) continue;
                    string n = p.Name;
                    if (n.Equals("MaxHealth", StringComparison.OrdinalIgnoreCase) || n.Equals("MaxHP", StringComparison.OrdinalIgnoreCase))
                    {
                        ScaleProperty(b, p, factor, clampMin: 1f);
                    }
                    if (n.Equals("Health", StringComparison.OrdinalIgnoreCase) || n.Equals("HP", StringComparison.OrdinalIgnoreCase))
                    {
                        ScaleProperty(b, p, factor, clampMin: 1f);
                    }
                }
            }
        }

        private static void ApplyDamageScaling(GameObject root, float factor)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var b in behaviours)
            {
                if (b == null) continue;
                var t = b.GetType();
                // Fields
                foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    if (!f.FieldType.IsPrimitive && f.FieldType != typeof(float) && f.FieldType != typeof(int)) continue;
                    string n = f.Name;
                    if (n.Equals("damage", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("attackDamage", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("meleeDamage", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("rangedDamage", StringComparison.OrdinalIgnoreCase))
                    {
                        ScaleField(b, f, factor, clampMin: 1f);
                    }
                }
                // Properties
                foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    if (!p.CanRead || !p.CanWrite) continue;
                    if (p.PropertyType != typeof(float) && p.PropertyType != typeof(int)) continue;
                    string n = p.Name;
                    if (n.Equals("Damage", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("AttackDamage", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("MeleeDamage", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("RangedDamage", StringComparison.OrdinalIgnoreCase))
                    {
                        ScaleProperty(b, p, factor, clampMin: 1f);
                    }
                }
            }
        }

        private static void ScaleField(object obj, System.Reflection.FieldInfo f, float factor, float clampMin)
        {
            try
            {
                if (f.FieldType == typeof(float))
                {
                    float v = (float)f.GetValue(obj);
                    v = Mathf.Max(clampMin, v * factor);
                    f.SetValue(obj, v);
                }
                else if (f.FieldType == typeof(int))
                {
                    int v = (int)f.GetValue(obj);
                    v = Mathf.Max((int)clampMin, Mathf.RoundToInt(v * factor));
                    f.SetValue(obj, v);
                }
            }
            catch { }
        }

        private static void ScaleProperty(object obj, System.Reflection.PropertyInfo p, float factor, float clampMin)
        {
            try
            {
                if (p.PropertyType == typeof(float))
                {
                    float v = (float)p.GetValue(obj);
                    v = Mathf.Max(clampMin, v * factor);
                    p.SetValue(obj, v);
                }
                else if (p.PropertyType == typeof(int))
                {
                    int v = (int)p.GetValue(obj);
                    v = Mathf.Max((int)clampMin, Mathf.RoundToInt(v * factor));
                    p.SetValue(obj, v);
                }
            }
            catch { }
        }
    }
}

