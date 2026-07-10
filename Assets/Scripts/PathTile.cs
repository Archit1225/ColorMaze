using UnityEngine;

public class PathTile : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;
    private bool isPainted = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        GameManager.Instance.AddPathTile();
    }

    public void Paint()
    {
        if (!isPainted)
        {
            isPainted = true;
            spriteRenderer.color = Color.green; // Set your painted color here
            _particleSystem.Play();
            GameManager.Instance.TilePainted();
        }
    }
}