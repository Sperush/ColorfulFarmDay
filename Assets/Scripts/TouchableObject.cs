using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    public float floatAmplitude = 0.1f; // Biên độ dao động (lên xuống bao nhiêu)
    public float floatFrequency = 1f;   // Tốc độ dao động (bao nhiêu lần/giây)

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    public void OnClicked(ProDuceManager proDuce)
    {
        proDuce.OpenPanel();    
    }

    private void Update()
    {
        float offsetY = Mathf.Sin(Time.time * floatFrequency * 2f * Mathf.PI) * floatAmplitude;
        transform.position = startPos + new Vector3(0, offsetY, 0);
    }
}
