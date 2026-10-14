using UnityEditor;
using UnityEditor.SceneManagement;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Pressing Play always starts the game scene, whatever scene the Editor has open.
    ///
    /// Batch-mode runs (scene rebuild, validator, research check, player build)
    /// rewrite Library/LastSceneManagerSetup.txt when they quit, often with an
    /// untitled scene. The next time the Editor opened it came up empty, and Play
    /// ran an empty scene. With this, opening the right scene by hand is optional.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string GameScene = "Assets/_Game/Scenes/Game.unity";

        static PlayModeStartScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScene);
        }
    }
}
