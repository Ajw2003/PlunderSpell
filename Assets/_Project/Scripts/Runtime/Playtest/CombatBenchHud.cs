using System.Collections.Generic;
using Plunderspell.Guards;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// The bench's control panel: pick an enemy, a count and a starting alert state, then spawn.
    /// See docs/systems/combat-bench.md, "The panel is IMGUI".
    /// </summary>
    [RequireComponent(typeof(CombatBench))]
    public class CombatBenchHud : MonoBehaviour
    {
        private const float k_PanelWidth = 260f;
        private const float k_PanelMargin = 12f;
        private const int k_MaxCount = 8;

        private static readonly GuardAlertState[] k_SelectableStates =
        {
            GuardAlertState.Patrolling,
            GuardAlertState.Investigating,
            GuardAlertState.Chasing,
        };

        [Tooltip("Hides the panel without disabling spawning, for a clean screenshot.")]
        [SerializeField] private bool m_isPanelVisible = true;

        [Tooltip("Key that shows or hides the panel.")]
        [SerializeField] private KeyCode m_toggleKey = KeyCode.F1;

        private CombatBench m_bench;
        private int m_selectedEnemy;
        private int m_count = 1;
        private int m_selectedState = 2;
        private Vector2 m_scroll;

        private void Awake()
        {
            m_bench = GetComponent<CombatBench>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(m_toggleKey))
            {
                m_isPanelVisible = !m_isPanelVisible;
            }
        }

        private void OnGUI()
        {
            if (!m_isPanelVisible)
            {
                return;
            }

            List<EnemyRoster.Entry> entries = SpawnableEntries();
            var panel = new Rect(k_PanelMargin, k_PanelMargin, k_PanelWidth,
                Mathf.Min(Screen.height - k_PanelMargin * 2f, 420f));

            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Label($"<b>Combat bench</b>  ({m_toggleKey} hides)", RichLabel());

            if (entries.Count == 0)
            {
                GUILayout.Label("No enemy roster assigned, or it has no prefabs.");
                GUILayout.EndArea();
                return;
            }

            DrawEnemyPicker(entries);
            DrawCountPicker();
            DrawStatePicker();
            DrawActions(entries);

            GUILayout.EndArea();
        }

        private void DrawEnemyPicker(List<EnemyRoster.Entry> entries)
        {
            GUILayout.Label("Enemy");
            m_scroll = GUILayout.BeginScrollView(m_scroll, GUILayout.Height(150f));

            for (int i = 0; i < entries.Count; i++)
            {
                bool isSelected = i == m_selectedEnemy;
                if (GUILayout.Toggle(isSelected, entries[i].EnemyId, GUI.skin.button) && !isSelected)
                {
                    m_selectedEnemy = i;
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawCountPicker()
        {
            GUILayout.Label($"Count: {m_count}");
            m_count = Mathf.RoundToInt(GUILayout.HorizontalSlider(m_count, 1f, k_MaxCount));
        }

        private void DrawStatePicker()
        {
            GUILayout.Label("Starting state");
            var names = new string[k_SelectableStates.Length];
            for (int i = 0; i < k_SelectableStates.Length; i++)
            {
                names[i] = k_SelectableStates[i].ToString();
            }

            m_selectedState = GUILayout.SelectionGrid(m_selectedState, names, 1);
        }

        private void DrawActions(List<EnemyRoster.Entry> entries)
        {
            GUILayout.Space(6f);

            if (GUILayout.Button("Spawn"))
            {
                m_bench.Spawn(entries[Mathf.Clamp(m_selectedEnemy, 0, entries.Count - 1)].Prefab,
                    m_count, k_SelectableStates[m_selectedState]);
            }

            if (GUILayout.Button($"Clear ({m_bench.Spawned.Count})"))
            {
                m_bench.ClearSpawned();
            }
        }

        private List<EnemyRoster.Entry> SpawnableEntries()
        {
            var result = new List<EnemyRoster.Entry>();
            EnemyRoster roster = m_bench.Roster;
            if (roster == null)
            {
                return result;
            }

            for (int i = 0; i < roster.Entries.Count; i++)
            {
                EnemyRoster.Entry entry = roster.Entries[i];
                if (entry != null && entry.Prefab != null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static GUIStyle RichLabel()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            return style;
        }
    }
}
