using UnityEngine;

public class Move : MonoBehaviour
{
    private Vector3 m_TargetPos = Vector3.zero;
    private float m_Speed = 5;
    private int m_TerrainLayer;

    // Use this for initialization
    void Start()
    {
        m_TerrainLayer = LayerMask.NameToLayer("FogOfWar");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.layer.CompareTo(m_TerrainLayer) == 0)
                {
                    m_TargetPos = hit.point;
                }
            }
        }

        if (m_TargetPos != Vector3.zero)
        {
            if (Vector3.Distance(m_TargetPos, transform.position) > 0.1f)
            {
                m_TargetPos.y = transform.position.y;
                transform.LookAt(m_TargetPos);
                transform.Translate(Vector3.forward * Time.deltaTime * m_Speed, Space.Self);
            }
        }
    }
}
