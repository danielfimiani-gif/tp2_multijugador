using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Vector3 offset = new(0f, 2f, -15f);
    [SerializeField] private float positionSmoothTime = 0.25f;

    [Header("Zoom (orthographic)")]
    [SerializeField] private float minSize = 6f;
    [SerializeField] private float maxSize = 12f;
    [SerializeField] private float zoomPadding = 4f;
    [SerializeField] private float zoomSmoothTime = 0.3f;

    private static readonly List<Transform> _targets = new();

    private Camera _cam;
    private Vector3 _posVelocity;
    private float _sizeVelocity;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_targets.Count == 0) return;

        var center = Vector3.zero;
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var count = 0;

        for (int i = _targets.Count - 1; i >= 0; i--)
        {
            var t = _targets[i];
            if (t == null) { _targets.RemoveAt(i); continue; }
            var p = t.position;
            center += p;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
            count++;
        }
        if (count == 0) return;

        center /= count;
        var target = new Vector3(center.x, center.y, 0f) + offset;
        transform.position = Vector3.SmoothDamp(transform.position, target, ref _posVelocity, positionSmoothTime);

        if (_cam.orthographic)
        {
            var sizeForWidth = (maxX - minX + zoomPadding) * 0.5f / Mathf.Max(0.01f, _cam.aspect);
            var sizeForHeight = (maxY - minY + zoomPadding) * 0.5f;
            var desired = Mathf.Clamp(Mathf.Max(sizeForWidth, sizeForHeight), minSize, maxSize);
            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, desired, ref _sizeVelocity, zoomSmoothTime);
        }
    }

    public static void Register(Transform t)
    {
        if (t != null && !_targets.Contains(t)) _targets.Add(t);
    }

    public static void Unregister(Transform t)
    {
        _targets.Remove(t);
    }
}
