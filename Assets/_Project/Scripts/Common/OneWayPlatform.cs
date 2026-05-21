using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OneWayPlatform : MonoBehaviour
{
    [Tooltip("Buffer en Y. Si el jugador queda atascado al pasar, bajar; si cae por error desde arriba, subir.")]
    [SerializeField] private float topBuffer = 0.1f;

    [Tooltip("Frecuencia de refresco del cache de jugadores en segundos.")]
    [SerializeField] private float playerCacheRefreshSec = 0.5f;

    private Collider _platformCol;

    private static NetworkCharacterController[] _cachedPlayers;
    private static float _lastCacheTime;

    private void Awake()
    {
        _platformCol = GetComponent<Collider>();
    }

    private void Update()
    {
        if (_platformCol == null) return;

        if (Time.unscaledTime - _lastCacheTime > playerCacheRefreshSec || _cachedPlayers == null)
        {
            _cachedPlayers = FindObjectsByType<NetworkCharacterController>(FindObjectsSortMode.None);
            _lastCacheTime = Time.unscaledTime;
        }

        var topY = _platformCol.bounds.max.y;

        for (int i = 0; i < _cachedPlayers.Length; i++)
        {
            var ncc = _cachedPlayers[i];
            if (ncc == null) continue;

            var cc = ncc.GetComponent<CharacterController>();
            if (cc == null) continue;

            var playerBottom = ncc.transform.position.y - cc.height * 0.5f + cc.center.y;
            var shouldCollide = playerBottom >= topY - topBuffer;
            Physics.IgnoreCollision(cc, _platformCol, !shouldCollide);
        }
    }
}
