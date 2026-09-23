using UnityEngine;

namespace StageFlow
{
    [RequireComponent(typeof(StageRunner))]
    public sealed class StageFlowPanel : MonoBehaviour
    {
        private StageRunner runner;
        private void Awake() => runner = GetComponent<StageRunner>();

        private void OnGUI()
        {
            if (runner == null) return;
            GUILayout.BeginArea(new Rect(16, 16, 300, 470), GUI.skin.box);
            GUILayout.Label("STAGEFLOW SANDBOX");
            GUILayout.Space(8);
            GUI.enabled = !runner.IsBusy;
            for (var i = 0; i < runner.StageCount; i++)
                if (GUILayout.Button((runner.SelectedIndex == i ? "> " : "") + runner.StageName(i), GUILayout.Height(32)))
                    runner.SelectStage(i);
            if (GUILayout.Button("Start", GUILayout.Height(34))) runner.StartRun();
            if (GUILayout.Button("Start Sequence (One > Two)", GUILayout.Height(34))) runner.StartSequence();
            GUI.enabled = runner.CanPause;
            if (GUILayout.Button("Pause", GUILayout.Height(34))) runner.Pause();
            GUI.enabled = runner.CanResume;
            if (GUILayout.Button("Resume", GUILayout.Height(34))) runner.Resume();
            GUI.enabled = true;
            if (GUILayout.Button("Reset", GUILayout.Height(34))) runner.ResetRun();
            GUILayout.Space(8);
            GUILayout.Label("Current / Selected: " + (runner.SelectedStage == null ? "None" : runner.SelectedStage.DisplayName));
            GUILayout.Label("State: " + runner.State);
            GUILayout.Label($"Planned: {runner.Planned}    Spawned: {runner.Spawned}");
            GUILayout.Label($"Active: {runner.ActiveCount}       Arrived: {runner.Arrived}");
            if (!string.IsNullOrEmpty(runner.LastError)) GUILayout.Label(runner.LastError);
            GUILayout.EndArea();
        }
    }
}
