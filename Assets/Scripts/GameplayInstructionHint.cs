using TMPro;
using UnityEngine;

public class GameplayInstructionHint : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] GameObject hintRoot;
    [SerializeField] TextMeshProUGUI hintLabel;
    [SerializeField] float hideAboveY = 2f;
    [SerializeField] string instructionText = "Press Space once to set your direction, then hold Space to charge jump power; release to jump.";
    [SerializeField] bool keepOnTop = true;

    bool hidden;

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (hintRoot == null)
            hintRoot = gameObject;

        if (hintLabel != null)
            hintLabel.text = instructionText;

        hintRoot.SetActive(true);

        if (keepOnTop)
            hintRoot.transform.SetAsLastSibling();
    }

    void Update()
    {
        if (hidden || player == null || !GameOptions.GameStarted) return;

        if (player.position.y > hideAboveY)
        {
            hidden = true;
            hintRoot.SetActive(false);
        }
    }
}
