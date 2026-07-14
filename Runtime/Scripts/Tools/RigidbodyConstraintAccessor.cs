using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyConstraintAccessor : MonoBehaviour
{
    public void Start()
    {
        m_rb = GetComponent<Rigidbody>();
    }

    
    public void FreezeTranslationX(bool freeze)
    {
        if (freeze) { m_rb.constraints |=  RigidbodyConstraints.FreezePositionX; }
        else        { m_rb.constraints &= ~RigidbodyConstraints.FreezePositionX; }
    }

    public void FreezeTranslationY(bool freeze)
    {
        if (freeze) { m_rb.constraints |=  RigidbodyConstraints.FreezePositionY; }
        else        { m_rb.constraints &= ~RigidbodyConstraints.FreezePositionY; }
    }

    public void FreezeTranslationZ(bool freeze)
    {
        if (freeze) { m_rb.constraints |=  RigidbodyConstraints.FreezePositionZ; }
        else        { m_rb.constraints &= ~RigidbodyConstraints.FreezePositionZ; }
    }

    public void FreezeRotationX(bool freeze)
    {
        if (freeze) { m_rb.constraints |=  RigidbodyConstraints.FreezeRotationX; }
        else        { m_rb.constraints &= ~RigidbodyConstraints.FreezeRotationX; }
    }

    public void FreezeRotationY(bool freeze)
    {
        if (freeze) { m_rb.constraints |=  RigidbodyConstraints.FreezeRotationY; }
        else        { m_rb.constraints &= ~RigidbodyConstraints.FreezeRotationY; }
    }

    public void FreezeRotationZ(bool freeze)
    {
        if (freeze) { m_rb.constraints |=  RigidbodyConstraints.FreezeRotationZ; }
        else        { m_rb.constraints &= ~RigidbodyConstraints.FreezeRotationZ; }
    }

    protected Rigidbody m_rb;
}
