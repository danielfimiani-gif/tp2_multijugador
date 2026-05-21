using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("0 = sigue exacto a la cámara. 1 = no se mueve (fondo más lejano).")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("Si está activo, también se mueve verticalmente con la cámara.")]
    [SerializeField] private bool verticalParallax = false;

    [Header("Infinite scroll horizontal")]
    [Tooltip("Si está activo, instancia copias del sprite a los lados y snapea la posición para dar ilusión de infinito.")]
    [SerializeField] private bool infiniteHorizontal = false;

    [Tooltip("Cantidad de copias por cada lado (izq/der). 1 alcanza para la mayoría de casos.")]
    [SerializeField] private int copiesPerSide = 1;

    private Transform _cam;
    private Vector3 _startPos;
    private float _spriteWidth;
    private readonly List<Transform> _copies = new();

    private void Start()
    {
        _cam = Camera.main != null ? Camera.main.transform : null;
        _startPos = transform.position;

        if (infiniteHorizontal && Application.isPlaying)
            CreateCopies();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _copies.Count; i++)
            if (_copies[i] != null)
                Destroy(_copies[i].gameObject);
        _copies.Clear();
    }

    private void CreateCopies()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        _spriteWidth = sr.bounds.size.x;
        if (_spriteWidth <= 0.01f) return;

        for (int n = 1; n <= copiesPerSide; n++)
        {
            _copies.Add(CreateCopy(sr, -n * _spriteWidth).transform);
            _copies.Add(CreateCopy(sr, +n * _spriteWidth).transform);
        }
    }

    private GameObject CreateCopy(SpriteRenderer template, float xOffset)
    {
        var copy = new GameObject($"{gameObject.name}_Copy_{xOffset:F0}");
        copy.transform.SetParent(transform.parent, true);
        copy.transform.position = transform.position + new Vector3(xOffset, 0f, 0f);
        copy.transform.rotation = transform.rotation;
        copy.transform.localScale = transform.lossyScale;

        var sr = copy.AddComponent<SpriteRenderer>();
        sr.sprite = template.sprite;
        sr.sortingLayerID = template.sortingLayerID;
        sr.sortingOrder = template.sortingOrder;
        sr.color = template.color;
        sr.flipX = template.flipX;
        sr.flipY = template.flipY;
        sr.drawMode = template.drawMode;
        if (template.drawMode != SpriteDrawMode.Simple) sr.size = template.size;

        return copy;
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            if (Camera.main != null) _cam = Camera.main.transform;
            else return;
        }

        var camPos = _cam.position;
        var xPos = _startPos.x + camPos.x * (1f - parallaxFactor);
        var yPos = _startPos.y + (verticalParallax ? camPos.y * (1f - parallaxFactor) : 0f);

        if (infiniteHorizontal && _spriteWidth > 0.01f)
        {
            var halfWidth = _spriteWidth * 0.5f;
            var rel = Mathf.Repeat(xPos - camPos.x + halfWidth, _spriteWidth) - halfWidth;
            xPos = camPos.x + rel;
        }

        transform.position = new Vector3(xPos, yPos, _startPos.z);

        if (infiniteHorizontal && _spriteWidth > 0.01f)
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                if (_copies[i] == null) continue;
                var side = (i % 2 == 0) ? -1 : 1;
                var n = (i / 2) + 1;
                _copies[i].position = new Vector3(xPos + side * n * _spriteWidth, yPos, _startPos.z);
            }
        }
    }
}
