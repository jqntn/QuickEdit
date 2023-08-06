using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class NewPlayableAsset : PlayableAsset
{
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        return Playable.Create(graph);
    }
}