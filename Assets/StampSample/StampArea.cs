using UnityEngine;

public abstract class StampArea : MonoBehaviour
{
    public Vector2 StampPosition => new Vector2(transform.position.x, transform.position.z);

    private bool _isStamped;

    public void TryStamp()
    {
        if (_isStamped) return;
        _isStamped = true;
        OnEnter();
    }

    public abstract void OnEnter();
    public abstract void OnStay();
    public abstract void OnExit();
}
