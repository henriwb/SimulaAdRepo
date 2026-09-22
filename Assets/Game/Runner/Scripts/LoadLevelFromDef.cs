using HyperCasual.Core;
using UnityEngine;

namespace HyperCasual.Runner
{
    public class LoadLevelFromDef : AbstractState
    {
        public LoadLevelFromDef(SceneController sceneController, AbstractLevelData levelData, GameObject[] managerPrefabs)
        {
        }
        
        public override void Execute()
        {
            GameManager.Instance.LoadLevel();
        }
    }
}